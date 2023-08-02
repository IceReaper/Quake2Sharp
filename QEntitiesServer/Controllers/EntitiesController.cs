using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QEntitiesServer.ECS;

namespace QEntitiesServer.Controllers;

[Authorize]
[Route("/")]
public class EntitiesController : Controller
{
    private readonly ECSConfigProvider _configurationProvider;

    public EntitiesController(ECSConfigProvider configurationClient)
    {   
        _configurationProvider = configurationClient ?? throw new ArgumentNullException(nameof(configurationClient));
    }

    [HttpGet]
    public async Task<ActionResult> GetEntities(string mapName)
    {
        string monstersPositionVersion = _configurationProvider.GetValue("CorrectMonsterPosition");

        string entitiesPath;

        switch (monstersPositionVersion)
        {
            case "v1":
                entitiesPath = "Entities\\Entities2.info";
                break;
            case "v2":
                entitiesPath = "Entities\\Entities1.info";
                break;
            default:
                entitiesPath = "Entities\\Entities_NoMonsters.info";
                break;
        }

        string entities = await System.IO.File.ReadAllTextAsync(entitiesPath);

        return Content(entities);
    }
}