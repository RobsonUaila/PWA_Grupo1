using Drought.Application.Interfaces;
using Drought.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Drought.Web.Controllers;

/// <summary>
/// Página inicial (Portal do Grupo 1) com apresentação executiva e indicadores vitais.
/// </summary>
public class HomeController : Controller
{
    private readonly DroughtDbContext _db;
    private readonly ITrainingService _trainingService;

    public HomeController(DroughtDbContext db, ITrainingService trainingService)
    {
        _db = db;
        _trainingService = trainingService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModel = await _trainingService.GetActiveModelMetricsAsync();
        ViewBag.TotalObservations = await _db.SoilObservations.CountAsync();
        ViewBag.TotalPredictions = await _db.Predictions.CountAsync();
        
        var regionsStatus = await _db.Regions
            .Select(r => new {
                r.Id,
                r.Name,
                r.Province,
                LatestObservation = r.Observations.OrderByDescending(o => o.Date).FirstOrDefault()
            })
            .ToListAsync();

        ViewBag.RegionsStatus = regionsStatus;
        return View();
    }
}
