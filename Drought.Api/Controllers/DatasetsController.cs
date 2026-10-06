using Drought.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Drought.Domain.Entities;

namespace Drought.Api.Controllers;

[ApiController]
[Route("api/datasets")]
[Authorize(Roles = "Administrador")]
public class DatasetsController : ControllerBase
{
    private readonly CsvImportService _importService;
    private readonly UserManager<ApplicationUser> _userManager;

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // limite de 20 MB (5.1 Segurança)
    private static readonly string[] AllowedExtensions = { ".csv" };

    public DatasetsController(CsvImportService importService, UserManager<ApplicationUser> userManager)
    {
        _importService = importService;
        _userManager = userManager;
    }

    /// <summary>POST /api/datasets/import — importa um ficheiro CSV validado (Admin).</summary>
    [HttpPost("import")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { erro = "Nenhum ficheiro enviado." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(new { erro = "Apenas ficheiros .csv são permitidos." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { erro = "Ficheiro excede o limite de 20 MB." });

        var userId = _userManager.GetUserId(User) ?? "sistema";

        using var stream = file.OpenReadStream();
        var result = await _importService.ImportAsync(stream, file.FileName, userId);

        if (!result.Success) return BadRequest(new { erro = result.Errors.FirstOrDefault() });

        return Ok(new
        {
            importadas = result.Imported,
            rejeitadas = result.Rejected,
            primeirosErros = result.Errors.Take(10)
        });
    }
}
