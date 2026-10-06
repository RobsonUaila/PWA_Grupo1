using Drought.Domain.Enums;

namespace Drought.Domain.Entities;

/// <summary>
/// Observação combinada de humidade do solo (Sentinel-1, ASCAT) e propriedades
/// do solo (SoilGrids) para uma região e data específicas.
/// Corresponde à tabela de treino descrita na secção 2.4 do enunciado.
/// </summary>
public class SoilObservation
{
    public int Id { get; set; }

    public int RegionId { get; set; }
    public Region Region { get; set; } = default!;

    public DateOnly Date { get; set; }

    // Sentinel-1 — fonte principal de condição superficial do solo.
    public double S1_SoilMoisture { get; set; }

    // ASCAT — comparação/validação.
    public double? Ascat_SoilMoisture { get; set; }

    // SoilGrids — propriedades relativamente estáveis do solo.
    public double? Clay { get; set; }
    public double? Sand { get; set; }
    public double? OrganicCarbon { get; set; }
    public double? BulkDensity { get; set; }

    // Rótulo (histórico/rotulado manualmente ou por regra) usado para treinar o modelo.
    // Null quando a observação ainda não tem classe conhecida (apenas para previsão).
    public DroughtClass? DroughtClass { get; set; }

    public int? DatasetSourceId { get; set; }
    public DatasetSource? DatasetSource { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
