namespace Drought.Domain.Entities;

/// <summary>
/// Regista a origem de cada importação de dados (Sentinel-1, ASCAT, SoilGrids),
/// permitindo rastreabilidade — requisito de qualidade dos dados do enunciado.
/// </summary>
public class DatasetSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;          // "Sentinel-1", "ASCAT", "SoilGrids"
    public string FileName { get; set; } = default!;
    public DateTime ImportedAtUtc { get; set; }
    public string ImportedByUserId { get; set; } = default!;
    public int RowsImported { get; set; }
    public int RowsRejected { get; set; }
    public string? Notes { get; set; }

    public ICollection<SoilObservation> Observations { get; set; } = new List<SoilObservation>();
}
