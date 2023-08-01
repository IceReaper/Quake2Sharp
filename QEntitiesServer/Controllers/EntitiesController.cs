using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace QEntitiesServer.Controllers;

[Authorize]
[Route("/")]
public class EntitiesController : Controller
{
    public Features _features;

    public EntitiesController(IOptions<Features> features)
    {
        _ = features ?? throw new ArgumentNullException(nameof(features));

        _features = features.Value;
    }

    [HttpGet]
    public async Task<ActionResult> GetEntities(string mapName)
    {
        string entities = await System.IO.File.ReadAllTextAsync("Entities\\Entities1.info");

        return Content(entities);
    }
}