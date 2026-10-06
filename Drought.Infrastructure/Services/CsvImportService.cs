using System.Globalization;
using Drought.Domain.Entities;
using Drought.Domain.Enums;
using Drought.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Drought.Infrastructure.Services;

/// <summary>
/// Serviço de importação e validação de ficheiros CSV com dados Sentinel-1,
/// ASCAT e SoilGrids (secção 1.2 "Módulo de validação de dados" do enunciado).
/// Formato esperado (cabeçalho):
/// Date,Region,S1_SoilMoisture,ASCAT_SM,Clay,Sand,OrganicCarbon,BulkDensity,DroughtClass
/// DroughtClass é opcional — linhas sem rótulo ficam disponíveis apenas para previsão.
/// </summary>
public class CsvImportService
{
    private readonly DroughtDbContext _db;

    public CsvImportService(DroughtDbContext db) => _db = db;

    public async Task<ImportResult> ImportAsync(Stream csvStream, string fileName, string userId)
    {
        var source = new DatasetSource
        {
            Name = "SoilMoisture_SoilGrids",
            FileName = fileName,
            ImportedAtUtc = DateTime.UtcNow,
            ImportedByUserId = userId
        };
        _db.DatasetSources.Add(source);

        var regions = await _db.Regions.ToDictionaryAsync(r => r.Name.ToLower(), r => r);

        using var reader = new StreamReader(csvStream);
        string? header = await reader.ReadLineAsync();
        if (header is null)
            return ImportResult.Failed("Ficheiro vazio.");

        var columns = header.Split(',').Select(c => c.Trim()).ToArray();
        int imported = 0, rejected = 0;
        var errors = new List<string>();
        int lineNumber = 1;

        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(',');
            if (values.Length < 3)
            {
                rejected++;
                errors.Add($"Linha {lineNumber}: número de colunas insuficiente.");
                continue;
            }

            try
            {
                var map = columns.Zip(values, (c, v) => (c, v)).ToDictionary(x => x.c, x => x.v.Trim());

                // --- validação de campos obrigatórios ---
                if (!DateOnly.TryParse(map.GetValueOrDefault("Date"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    rejected++; errors.Add($"Linha {lineNumber}: data inválida."); continue;
                }

                var regionName = map.GetValueOrDefault("Region", "").Trim();
                if (!regions.TryGetValue(regionName.ToLower(), out var region))
                {
                    rejected++; errors.Add($"Linha {lineNumber}: região '{regionName}' não reconhecida (esperado Chókwè ou Massinga)."); continue;
                }

                if (!double.TryParse(map.GetValueOrDefault("S1_SoilMoisture"), NumberStyles.Any, CultureInfo.InvariantCulture, out var s1))
                {
                    rejected++; errors.Add($"Linha {lineNumber}: S1_SoilMoisture inválido/nulo."); continue;
                }

                // valores fora do intervalo plausível [0,1] para humidade volumétrica.
                if (s1 < 0 || s1 > 1)
                {
                    rejected++; errors.Add($"Linha {lineNumber}: S1_SoilMoisture fora do intervalo esperado (0–1)."); continue;
                }

                double? TryParseNullable(string key) =>
                    double.TryParse(map.GetValueOrDefault(key), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

                DroughtClass? droughtClass = null;
                var classText = map.GetValueOrDefault("DroughtClass", "").Trim();
                if (!string.IsNullOrEmpty(classText))
                {
                    var normalizedClass = classText.ToLower().Replace(" ", "").Replace("_", "");
                    droughtClass = normalizedClass switch
                    {
                        "normal" => DroughtClass.Normal,
                        "moderada" or "secamoderada" => DroughtClass.SecaModerada,
                        "severa" or "secasevera" => DroughtClass.SecaSevera,
                        "extrema" or "secaextrema" => DroughtClass.SecaExtrema,
                        _ => null
                    };
                }

                // evita duplicados: mesma região + mesma data.
                bool duplicate = await _db.SoilObservations
                    .AnyAsync(o => o.RegionId == region.Id && o.Date == date);
                if (duplicate)
                {
                    rejected++; errors.Add($"Linha {lineNumber}: observação duplicada para {regionName} em {date}."); continue;
                }

                _db.SoilObservations.Add(new SoilObservation
                {
                    RegionId = region.Id,
                    Date = date,
                    S1_SoilMoisture = s1,
                    Ascat_SoilMoisture = TryParseNullable("ASCAT_SM"),
                    Clay = TryParseNullable("Clay"),
                    Sand = TryParseNullable("Sand"),
                    OrganicCarbon = TryParseNullable("OrganicCarbon"),
                    BulkDensity = TryParseNullable("BulkDensity"),
                    DroughtClass = droughtClass,
                    DatasetSource = source
                });
                imported++;
            }
            catch (Exception ex)
            {
                rejected++;
                errors.Add($"Linha {lineNumber}: erro inesperado ({ex.Message}).");
            }
        }

        source.RowsImported = imported;
        source.RowsRejected = rejected;
        source.Notes = errors.Count > 0 ? string.Join(" | ", errors.Take(20)) : null;

        await _db.SaveChangesAsync();

        return new ImportResult(true, imported, rejected, errors);
    }
}

public record ImportResult(bool Success, int Imported, int Rejected, List<string> Errors)
{
    public static ImportResult Failed(string message) => new(false, 0, 0, new List<string> { message });
}
