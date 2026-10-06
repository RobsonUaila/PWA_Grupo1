using Drought.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Drought.ML;

/// <summary>
/// Carrega um modelo ML.NET já treinado (.zip) e permite gerar previsões
/// individuais a partir dos valores de humidade/solo de uma observação.
/// </summary>
public class DroughtPredictionEngine : IDisposable
{
    private readonly MLContext _ml = new();
    private readonly PredictionEngine<SoilObservationData, DroughtPrediction> _engine;
    private readonly ITransformer _model;
    public string[] ClassNames { get; }

    public DroughtPredictionEngine(string modelPath)
    {
        _model = _ml.Model.Load(modelPath, out var schema);
        _engine = _ml.Model.CreatePredictionEngine<SoilObservationData, DroughtPrediction>(_model);

        // Recupera os nomes das classes na mesma ordem dos scores, para reportar probabilidades.
        VBuffer<ReadOnlyMemory<char>> slotNames = default;
        schema["Score"].Annotations.GetValue("SlotNames", ref slotNames);
        ClassNames = slotNames.DenseValues().Select(v => v.ToString()).ToArray();
    }

    public (string PredictedClass, float Confidence, Dictionary<string, float> Probabilities) Predict(SoilObservationData input)
    {
        var result = _engine.Predict(input);
        var probabilities = new Dictionary<string, float>();
        for (int i = 0; i < ClassNames.Length && i < result.Score.Length; i++)
            probabilities[ClassNames[i]] = result.Score[i];

        var confidence = probabilities.TryGetValue(result.PredictedLabel, out var c) ? c : result.Score.Max();
        return (result.PredictedLabel, confidence, probabilities);
    }

    public void Dispose() => _engine.Dispose();
}
