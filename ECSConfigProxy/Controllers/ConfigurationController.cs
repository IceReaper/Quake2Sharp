using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECSConfigProxy.ECS;
using ECSConfigProxy.FlagdModel;

namespace ECSConfigProxy.Controllers
{
    [Route("/")]
    public class ConfigurationController : Controller
    {
        private readonly ECSConfigCache _configurationCache;

        public ConfigurationController(ECSConfigCache configCache)
        {
            _configurationCache = configCache ?? throw new ArgumentNullException(nameof(configCache));
        }

        [HttpGet]
        public async Task<ActionResult> RetrieveConfigs()
        {
            var allCachedConfigs = _configurationCache.GetAll();

            var obj = FlagdFlagFactory.CreateFlagsSet(allCachedConfigs);

            return Json(obj);
        }
    }
}
