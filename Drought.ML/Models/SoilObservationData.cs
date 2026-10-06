using Microsoft.ML.Data;

namespace Drought.ML.Models;

/// <summary>
/// Esquema de entrada do ML.NET. Cada propriedade corresponde a uma
/// coluna/feature usada no treino do classificador de seca (secção 2.4).
/// </summary>
public class SoilObservationData
{
    [LoadColumn(0)] public string RegionName { get; set; } = default!;
    [LoadColumn(1)] public float S1_SoilMoisture { get; set; }
    [LoadColumn(2)] public float Ascat_SoilMoisture { get; set; }
    [LoadColumn(3)] public float Clay { get; set; }
    [LoadColumn(4)] public float Sand { get; set; }
    [LoadColumn(5)] public float OrganicCarbon { get; set; }
    [LoadColumn(6)] public float BulkDensity { get; set; }

    // Rótulo em texto (ex.: "Normal", "SecaModerada", "SecaSevera", "SecaExtrema").
    [LoadColumn(7)] public string Label { get; set; } = default!;
}
