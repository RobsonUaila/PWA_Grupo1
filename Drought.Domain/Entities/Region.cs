namespace Drought.Domain.Entities;

/// <summary>
/// Região monitorizada pelo sistema. Neste projecto: Chókwè e Massinga.
/// </summary>
public class Region
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Province { get; set; } = default!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public ICollection<SoilObservation> Observations { get; set; } = new List<SoilObservation>();
    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
}
