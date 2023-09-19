using Microsoft.AspNetCore.Mvc;
using TileMapService.Models;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Custom API endpoint for managing tile sources.
    /// </summary>
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly ITileSourceFabric tileSourceFabric;

        public ApiController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

        [HttpGet("sources")]
        public IActionResult GetSources()
        {
            var result = this.tileSourceFabric.Sources.Select(p => new TileSourceModel(p.Type, p.Id, p.Format, p.Title, p.Abstract, p.Tms, p.MinZoom, p.MaxZoom, p.Srs));
            return Ok(result);
        }

        // TODO: full set of CRUD actions for sources
        // Simple authorization - allow only for local requests
        ////if (Request.IsLocal()) { } else { return Forbid(); }
    }
}
