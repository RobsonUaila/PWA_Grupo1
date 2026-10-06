using Drought.Application.DTOs;
using Drought.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Drought.Api.Controllers;

[ApiController]
[Route("api/observations")]
[Authorize]
public class ObservationsController : ControllerBase
{
    private readonly DroughtDbContext _db;
    public ObservationsController(DroughtDbContext db) => _db = db;

    /// <summary>GET /api/observations?region=Chókwè — consulta dados climáticos importados.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ObservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] string? region, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var query = _db.SoilObservations.Include(o => o.Region).AsQueryable();

        if (!string.IsNullOrWhiteSpace(region))
        {
            var exists = await _db.Regions.AnyAsync(r => r.Name == region);
            if (!exists) return NotFound(new { erro = $"Região '{region}' não encontrada." });
            query = query.Where(o => o.Region.Name == region);
        }
        if (from is not null) query = query.Where(o => o.Date >= from);
        if (to is not null) query = query.Where(o => o.Date <= to);

        var data = await query
            .OrderByDescending(o => o.Date)
            .Take(1000)
            .Select(o => new ObservationDto(o.Date, o.Region.Name, o.S1_SoilMoisture,
                o.Ascat_SoilMoisture, o.DroughtClass == null ? null : o.DroughtClass.ToString()))
            .ToListAsync();

        return Ok(data);
    }
}
