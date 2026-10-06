namespace Drought.Domain.Entities;

/// <summary>
/// Guarda a versão/data de cada modelo treinado e as respectivas métricas,
/// conforme exigido em "5.3 Qualidade da Inteligência Artificial".
/// </summary>
public class ModelVersion
{
    public int Id { get; set; }
    public string Algorithm { get; set; } = default!;      // "SdcaMaximumEntropy", "LightGbm", ...
    public DateTime TrainedAtUtc { get; set; }
    public string ModelFilePath { get; set; } = default!;  // caminho do ficheiro .zip do ML.NET
    public int TrainingRows { get; set; }
    public int TestRows { get; set; }

    public double MacroAccuracy { get; set; }
    public double MicroAccuracy { get; set; }
    public double LogLoss { get; set; }
    public string? ConfusionMatrixJson { get; set; }       // matriz serializada em JSON para o dashboard
    public string? PerClassMetricsJson { get; set; }        // precision/recall/F1 por classe

    public bool IsActive { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
}
