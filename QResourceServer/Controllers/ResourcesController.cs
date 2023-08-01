using Azure.Identity;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QEntitiesServer;
using System.Net.Mime;

[Authorize]
[Route("/")]
public class ResourcesController : Controller
{
    public const string ResourcesCacheItemKey = "ResourcesCacheItemKey";

    private readonly ResourceAccessSettings _resourceAccessSettings;
    private readonly IMemoryCache _cache;

    public ResourcesController(IOptions<ResourceAccessSettings> options, IMemoryCache cache)
    {
        _ = options.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = cache == null ? throw new ArgumentNullException(nameof(cache)) : cache;

        _resourceAccessSettings = options.Value;
    }

    [HttpGet]
    public async Task<FileContentResult> Get()
    {
        byte[] resources;
        if (_cache.TryGetValue(ResourcesCacheItemKey, out resources))
        {
            return File(resources, contentType: MediaTypeNames.Application.Zip);
        }

        var resourceUri = new Uri(_resourceAccessSettings.ResourceUri);
        var blockBlocClient = new BlockBlobClient(resourceUri, new ClientSecretCredential(_resourceAccessSettings.TenantId, _resourceAccessSettings.ClientId, _resourceAccessSettings.ClientSecret));

        var result = await blockBlocClient.DownloadContentAsync();
        resources = result.Value.Content.ToArray();

        _cache.Set(ResourcesCacheItemKey, resources);

        return File(resources, contentType: MediaTypeNames.Application.Zip);
    }
}