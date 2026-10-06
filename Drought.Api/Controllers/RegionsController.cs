using Drought.Application.DTOs;
using Drought.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Drought.Api.Controllers;

[ApiController]
[Route("api/regions")]
[Authorize]
public class RegionsController : ControllerBase
{
    private readonly DroughtDbContext _db;
    public RegionsController(DroughtDbContext db) => _db = db;

    /// <summary>GET /api/regions — lista as regiões monitorizadas.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<RegionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var regions = await _db.Regions
            .Select(r => new RegionDto(r.Id, r.Name, r.Province))
            .ToListAsync();
        return Ok(regions);
    }
}
