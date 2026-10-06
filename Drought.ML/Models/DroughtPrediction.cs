using Microsoft.ML.Data;

namespace Drought.ML.Models;

/// <summary>Saída do modelo ML.NET para uma observação individual.</summary>
public class DroughtPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = default!;

    public float[] Score { get; set; } = Array.Empty<float>();
}
