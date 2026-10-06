using Drought.Application.Interfaces;
using Drought.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Drought.Domain.Entities;

namespace Drought.Web.Controllers;

/// <summary>
/// Área de administração: gerir utilizadores, importar dados e re-treinar
/// o modelo (secção 1.2 "Área de administração").
/// </summary>
[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly CsvImportService _importService;
    private readonly ITrainingService _trainingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(CsvImportService importService, ITrainingService trainingService,
        UserManager<ApplicationUser> userManager)
    {
        _importService = importService;
        _trainingService = trainingService;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken] // protecção CSRF (secção 5.1)
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Erro"] = "Selecciona um ficheiro CSV.";
            return RedirectToAction(nameof(Import));
        }

        var userId = _userManager.GetUserId(User)!;
        using var stream = file.OpenReadStream();
        var result = await _importService.ImportAsync(stream, file.FileName, userId);

        TempData["Resultado"] = $"Importadas: {result.Imported} | Rejeitadas: {result.Rejected}";
        return RedirectToAction(nameof(Import));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrainModel()
    {
        try
        {
            var results = await _trainingService.TrainAndCompareAsync(User.Identity!.Name!);
            TempData["Resultado"] = string.Join(" | ", results.Select(r =>
                $"{r.Algorithm}: MacroAcc={r.MacroAccuracy:P1}{(r.IsActive ? " (ACTIVO)" : "")}"));
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }
        return RedirectToAction(nameof(Import));
    }
}
