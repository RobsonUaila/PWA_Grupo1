using Drought.Application.DTOs;
using Drought.Application.Interfaces;
using Drought.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Drought.Web.Controllers;

/// <summary>
/// Dashboard com gráficos, mapa interativo e simulador de IA para Chókwè e Massinga.
/// Acesso público para consulta rápida e monitoria académica.
/// </summary>
public class DashboardController : Controller
{
    private readonly DroughtDbContext _db;
    private readonly ITrainingService _trainingService;
    private readonly IPredictionService _predictionService;

    public DashboardController(DroughtDbContext db, ITrainingService trainingService, IPredictionService predictionService)
    {
        _db = db;
        _trainingService = trainingService;
        _predictionService = predictionService;
    }

    public async Task<IActionResult> Index(int? regionId)
    {
        var regions = await _db.Regions.ToListAsync();
        ViewBag.Regions = regions;
        ViewBag.SelectedRegionId = regionId;
        ViewBag.ActiveModel = await _trainingService.GetActiveModelMetricsAsync();

        var obsQuery = _db.SoilObservations.Include(o => o.Region).AsQueryable();
        if (regionId is not null) obsQuery = obsQuery.Where(o => o.RegionId == regionId);

        var series = await obsQuery
            .OrderBy(o => o.Date)
            .Select(o => new { 
                Date = o.Date.ToString("yyyy-MM-dd"), 
                Region = o.Region.Name, 
                o.S1_SoilMoisture, 
                o.Ascat_SoilMoisture 
            })
            .Take(300)
            .ToListAsync();

        var mapData = await _db.Regions
            .Select(r => new {
                r.Id,
                r.Name,
                r.Province,
                r.Latitude,
                r.Longitude,
                LatestObservation = r.Observations.OrderByDescending(o => o.Date).FirstOrDefault(),
                TotalObservations = r.Observations.Count()
            })
            .ToListAsync();

        var mapPoints = mapData.Where(m => m.Latitude.HasValue && m.Longitude.HasValue)
            .Select(m => new {
                m.Id,
                m.Name,
                m.Province,
                m.Latitude,
                m.Longitude,
                DroughtClass = m.LatestObservation?.DroughtClass.ToString() ?? "Desconhecido",
                Moisture = m.LatestObservation?.S1_SoilMoisture ?? 0,
                Date = m.LatestObservation?.Date.ToString("dd/MM/yyyy") ?? "Sem dados",
                m.TotalObservations
            });

        ViewBag.TotalObservationsCount = await _db.SoilObservations.CountAsync();
        ViewBag.TotalPredictionsCount = await _db.Predictions.CountAsync();
        ViewBag.MapData = mapPoints.ToList();
        ViewBag.SeriesJson = System.Text.Json.JsonSerializer.Serialize(series);
        ViewBag.MapDataJson = System.Text.Json.JsonSerializer.Serialize(mapPoints);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Predict(int regionId, double s1SoilMoisture, double? ascat, double? clay, double? sand, double? organicCarbon, double? bulkDensity)
    {
        try
        {
            var req = new PredictionRequestDto(
                regionId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                s1SoilMoisture,
                ascat,
                clay,
                sand,
                organicCarbon,
                bulkDensity
            );

            var result = await _predictionService.PredictAsync(req, User.Identity?.Name ?? "Visitante_Web");
            TempData["PredictionSuccess"] = System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            TempData["PredictionError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { regionId });
    }
}
