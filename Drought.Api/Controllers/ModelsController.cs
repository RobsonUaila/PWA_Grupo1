using Drought.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drought.Api.Controllers;

[ApiController]
[Route("api/models")]
[Authorize]
public class ModelsController : ControllerBase
{
    private readonly ITrainingService _trainingService;
    public ModelsController(ITrainingService trainingService) => _trainingService = trainingService;

    /// <summary>POST /api/models/train — treina/re-treina o modelo (apenas Administrador).</summary>
    [HttpPost("train")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Train()
    {
        try
        {
            var results = await _trainingService.TrainAndCompareAsync(User.Identity!.Name!);
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>GET /api/models/metrics — métricas da versão activa do modelo.</summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics()
    {
        var metrics = await _trainingService.GetActiveModelMetricsAsync();
        if (metrics is null) return NotFound(new { erro = "Nenhum modelo activo. Treine um modelo primeiro." });
        return Ok(metrics);
    }
}
