using Drought.Application.DTOs;
using Drought.Application.Interfaces;
using Drought.Domain.Entities;
using Drought.Infrastructure.Data;
using Drought.ML;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Drought.Application.Services;

/// <summary>
/// Orquestra o treino: exporta as observações rotuladas da base de dados para
/// CSV, treina dois algoritmos (SDCA e LightGBM), regista as métricas de ambos
/// e activa automaticamente o de maior MacroAccuracy (secção 2.5 / 5.3).
/// </summary>
public class TrainingService : ITrainingService
{
    private readonly DroughtDbContext _db;
    private readonly string _modelsFolder;

    public TrainingService(DroughtDbContext db, string modelsFolder)
    {
        _db = db;
        _modelsFolder = modelsFolder;
        Directory.CreateDirectory(_modelsFolder);
    }

    public async Task<IReadOnlyList<ModelMetricsDto>> TrainAndCompareAsync(string triggeredByUserId)
    {
        var labeled = await _db.SoilObservations
            .Include(o => o.Region)
            .Where(o => o.DroughtClass != null)
            .ToListAsync();

        if (labeled.Count < 20)
            throw new InvalidOperationException("Dados insuficientes para treinar (mínimo recomendado: 20 observações rotuladas).");

        var csvPath = Path.Combine(_modelsFolder, $"training_export_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        var ci = CultureInfo.InvariantCulture;
        await File.WriteAllLinesAsync(csvPath,
            new[] { "RegionName,S1_SoilMoisture,Ascat_SoilMoisture,Clay,Sand,OrganicCarbon,BulkDensity,Label" }
            .Concat(labeled.Select(o =>
                $"{o.Region.Name},{o.S1_SoilMoisture.ToString(ci)},{(o.Ascat_SoilMoisture ?? 0).ToString(ci)},{(o.Clay ?? 0).ToString(ci)},{(o.Sand ?? 0).ToString(ci)},{(o.OrganicCarbon ?? 0).ToString(ci)},{(o.BulkDensity ?? 0).ToString(ci)},{o.DroughtClass}")));

        var trainer = new DroughtModelTrainer();
        var results = new List<ModelMetricsDto>();

        foreach (var algorithm in new[] { "Sdca", "LightGbm" })
        {
            var modelPath = Path.Combine(_modelsFolder, $"drought_{algorithm}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
            var outcome = trainer.TrainAndEvaluate(csvPath, modelPath, algorithm);

            var version = new ModelVersion
            {
                Algorithm = algorithm,
                TrainedAtUtc = DateTime.UtcNow,
                ModelFilePath = modelPath,
                TrainingRows = outcome.TrainingRows,
                TestRows = outcome.TestRows,
                MacroAccuracy = outcome.MacroAccuracy,
                MicroAccuracy = outcome.MicroAccuracy,
                LogLoss = outcome.LogLoss,
                ConfusionMatrixJson = outcome.ConfusionMatrix is null ? null
                    : JsonSerializer.Serialize(outcome.ConfusionMatrix.Counts),
                IsActive = false
            };
            _db.ModelVersions.Add(version);
            await _db.SaveChangesAsync();

            results.Add(new ModelMetricsDto(version.Id, algorithm, version.TrainedAtUtc,
                version.TrainingRows, version.TestRows, version.MacroAccuracy, version.MicroAccuracy,
                version.LogLoss, version.IsActive));
        }

        // Activa apenas o melhor modelo (maior MacroAccuracy); desactiva os restantes.
        var bestId = results.OrderByDescending(r => r.MacroAccuracy).First().ModelVersionId;
        var allVersions = await _db.ModelVersions.ToListAsync();
        foreach (var v in allVersions) v.IsActive = v.Id == bestId;
        await _db.SaveChangesAsync();

        return results.Select(r => r with { IsActive = r.ModelVersionId == bestId }).ToList();
    }

    public async Task<ModelMetricsDto?> GetActiveModelMetricsAsync()
    {
        var active = await _db.ModelVersions.FirstOrDefaultAsync(m => m.IsActive);
        if (active is null) return null;

        return new ModelMetricsDto(active.Id, active.Algorithm, active.TrainedAtUtc,
            active.TrainingRows, active.TestRows, active.MacroAccuracy, active.MicroAccuracy,
            active.LogLoss, active.IsActive);
    }
}
