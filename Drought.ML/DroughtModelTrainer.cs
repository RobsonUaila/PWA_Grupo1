using Drought.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Drought.ML;

/// <summary>
/// Pipeline de treino e avaliação do modelo de classificação de seca.
/// Implementa o requisito "treinar pelo menos dois algoritmos de classificação
/// e comparar os resultados" (secção 2.5) usando SDCA e LightGBM multiclasse.
/// </summary>
public class DroughtModelTrainer
{
    private readonly MLContext _ml = new(seed: 42);

    public TrainingOutcome TrainAndEvaluate(string csvPath, string outputModelPath, string algorithm = "Sdca")
    {
        IDataView fullData = _ml.Data.LoadFromTextFile<SoilObservationData>(
            csvPath, hasHeader: true, separatorChar: ',');

        // Divisão treino/teste 80/20, estratificada de forma simples pelo ML.NET.
        var split = _ml.Data.TrainTestSplit(fullData, testFraction: 0.2, seed: 42);

        var featureColumns = new[]
        {
            nameof(SoilObservationData.S1_SoilMoisture),
            nameof(SoilObservationData.Ascat_SoilMoisture),
            nameof(SoilObservationData.Clay),
            nameof(SoilObservationData.Sand),
            nameof(SoilObservationData.OrganicCarbon),
            nameof(SoilObservationData.BulkDensity),
        };

        var dataPrepPipeline = _ml.Transforms.Conversion
            .MapValueToKey(inputColumnName: nameof(SoilObservationData.Label), outputColumnName: "LabelKey")
            .Append(_ml.Transforms.Categorical.OneHotEncoding(
                inputColumnName: nameof(SoilObservationData.RegionName), outputColumnName: "RegionEncoded"))
            .Append(_ml.Transforms.Concatenate("Features", featureColumns.Append("RegionEncoded").ToArray()))
            .Append(_ml.Transforms.NormalizeMinMax("Features"));

        IEstimator<ITransformer> trainer = algorithm switch
        {
            "LightGbm" => _ml.MulticlassClassification.Trainers.LightGbm(
                labelColumnName: "LabelKey", featureColumnName: "Features"),
            _ => _ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "LabelKey", featureColumnName: "Features"),
        };

        var fullPipeline = dataPrepPipeline
            .Append(trainer)
            .Append(_ml.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));

        var model = fullPipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = _ml.MulticlassClassification.Evaluate(
            predictions, labelColumnName: "LabelKey", scoreColumnName: "Score");

        _ml.Model.Save(model, split.TrainSet.Schema, outputModelPath);

        return new TrainingOutcome
        {
            Algorithm = algorithm,
            ModelPath = outputModelPath,
            TrainingRows = (int)_ml.Data.CreateEnumerable<SoilObservationData>(split.TrainSet, false).Count(),
            TestRows = (int)_ml.Data.CreateEnumerable<SoilObservationData>(split.TestSet, false).Count(),
            MacroAccuracy = metrics.MacroAccuracy,
            MicroAccuracy = metrics.MicroAccuracy,
            LogLoss = metrics.LogLoss,
            ConfusionMatrix = metrics.ConfusionMatrix,
            PerClassLogLoss = metrics.PerClassLogLoss.ToArray(),
        };
    }
}

public class TrainingOutcome
{
    public string Algorithm { get; set; } = default!;
    public string ModelPath { get; set; } = default!;
    public int TrainingRows { get; set; }
    public int TestRows { get; set; }
    public double MacroAccuracy { get; set; }
    public double MicroAccuracy { get; set; }
    public double LogLoss { get; set; }
    public ConfusionMatrix? ConfusionMatrix { get; set; }
    public double[] PerClassLogLoss { get; set; } = Array.Empty<double>();
}
