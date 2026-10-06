using Drought.Domain.Enums;

namespace Drought.Domain.Entities;

/// <summary>
/// Resultado de uma classificação de seca gerada pelo modelo activo,
/// guardado para histórico, auditoria e comparação (secção 2.5 / 5.3).
/// </summary>
public class Prediction
{
    public int Id { get; set; }

    public int RegionId { get; set; }
    public Region Region { get; set; } = default!;

    public DateOnly Date { get; set; }

    public int ModelVersionId { get; set; }
    public ModelVersion ModelVersion { get; set; } = default!;

    public DroughtClass PredictedClass { get; set; }
    public double Confidence { get; set; }                 // probabilidade da classe prevista (0-1)
    public string ProbabilitiesJson { get; set; } = "{}";   // probabilidades de todas as classes

    // Valores de entrada usados nesta previsão, guardados para explicabilidade.
    public double S1_SoilMoisture { get; set; }
    public double? Ascat_SoilMoisture { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string RequestedByUserId { get; set; } = default!;
}
