using Drought.Application.DTOs;
using Drought.Application.Interfaces;
using Drought.Domain.Entities;
using Drought.Domain.Enums;
using Drought.Infrastructure.Data;
using Drought.ML;
using Drought.ML.Models;
using Microsoft.EntityFrameworkCore;

namespace Drought.Application.Services;

public class PredictionService : IPredictionService
{
    private readonly DroughtDbContext _db;
    private readonly IWebHostPathProvider _paths;

    public PredictionService(DroughtDbContext db, IWebHostPathProvider paths)
    {
        _db = db;
        _paths = paths;
    }

    public async Task<PredictionResultDto> PredictAsync(PredictionRequestDto request, string userId)
    {
        var activeModel = await _db.ModelVersions.FirstOrDefaultAsync(m => m.IsActive)
            ?? throw new InvalidOperationException("Não existe nenhum modelo activo. Treine um modelo primeiro (POST /api/models/train).");

        var region = await _db.Regions.FindAsync(request.RegionId)
            ?? throw new KeyNotFoundException("Região não encontrada.");

        using var engine = new DroughtPredictionEngine(activeModel.ModelFilePath);

        var (predictedClassText, confidence, probabilities) = engine.Predict(new SoilObservationData
        {
            RegionName = region.Name,
            S1_SoilMoisture = (float)request.S1_SoilMoisture,
            Ascat_SoilMoisture = (float)(request.AscatSoilMoisture ?? 0),
            Clay = (float)(request.Clay ?? 0),
            Sand = (float)(request.Sand ?? 0),
            OrganicCarbon = (float)(request.OrganicCarbon ?? 0),
            BulkDensity = (float)(request.BulkDensity ?? 0),
        });

        var predictedClass = Enum.Parse<DroughtClass>(predictedClassText);

        _db.Predictions.Add(new Prediction
        {
            RegionId = region.Id,
            Date = request.Date,
            ModelVersionId = activeModel.Id,
            PredictedClass = predictedClass,
            Confidence = confidence,
            ProbabilitiesJson = System.Text.Json.JsonSerializer.Serialize(probabilities),
            S1_SoilMoisture = request.S1_SoilMoisture,
            Ascat_SoilMoisture = request.AscatSoilMoisture,
            RequestedByUserId = userId
        });
        await _db.SaveChangesAsync();

        return new PredictionResultDto(region.Name, request.Date, predictedClass.ToString(),
            confidence, probabilities, activeModel.Algorithm, activeModel.TrainedAtUtc);
    }

    public async Task<IReadOnlyList<PredictionResultDto>> GetHistoryAsync(int? regionId, DateOnly? from, DateOnly? to)
    {
        var query = _db.Predictions.Include(p => p.Region).Include(p => p.ModelVersion).AsQueryable();
        if (regionId is not null) query = query.Where(p => p.RegionId == regionId);
        if (from is not null) query = query.Where(p => p.Date >= from);
        if (to is not null) query = query.Where(p => p.Date <= to);

        var items = await query.OrderByDescending(p => p.Date).Take(500).ToListAsync();

        return items.Select(p => new PredictionResultDto(
            p.Region.Name, p.Date, p.PredictedClass.ToString(), p.Confidence,
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, float>>(p.ProbabilitiesJson) ?? new(),
            p.ModelVersion.Algorithm, p.ModelVersion.TrainedAtUtc
        )).ToList();
    }
}

/// <summary>Abstracção simples para resolver caminhos de ficheiros (facilita testes).</summary>
public interface IWebHostPathProvider
{
    string MapPath(string relativePath);
}
