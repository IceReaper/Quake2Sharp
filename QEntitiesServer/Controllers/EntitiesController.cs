using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenFeature.Contrib.Providers.Flagd;

namespace QEntitiesServer.Controllers;

[Authorize]
[Route("/")]
public class EntitiesController : Controller
{
    private readonly OpenFeature.FeatureClient _featureClient;

    public EntitiesController()
    {
        var flagdProvider = new FlagdProvider();
        OpenFeature.Api.Instance.SetProvider(flagdProvider);
        _featureClient = OpenFeature.Api.Instance.GetClient(nameof(EntitiesController));
    }

    [HttpGet]
    public async Task<ActionResult> GetEntities(string mapName)
    {
        string monstersPositionVersion = await _featureClient.GetStringValue("CorrectMonsterPosition", "none", null).ConfigureAwait(false);
        Console.WriteLine($"Read CorrectMonsterPosition as {monstersPositionVersion}");

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