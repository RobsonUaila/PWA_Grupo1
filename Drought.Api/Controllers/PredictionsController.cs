using Drought.Application.DTOs;
using Drought.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drought.Api.Controllers;

[ApiController]
[Route("api/predictions")]
[Authorize]
public class PredictionsController : ControllerBase
{
    private readonly IPredictionService _predictionService;
    public PredictionsController(IPredictionService predictionService) => _predictionService = predictionService;

    /// <summary>POST /api/predictions — gera uma classificação de seca para uma observação.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PredictionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Predict([FromBody] PredictionRequestDto request)
    {
        try
        {
            var userId = User.Identity!.Name ?? "anonimo";
            var result = await _predictionService.PredictAsync(request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { erro = ex.Message }); }
    }

    /// <summary>GET /api/predictions/history — histórico de previsões por região/período.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int? regionId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var history = await _predictionService.GetHistoryAsync(regionId, from, to);
        return Ok(history);
    }
}
