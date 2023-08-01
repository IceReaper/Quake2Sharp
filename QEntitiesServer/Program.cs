using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using QEntitiesServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureServices((hostContext, services) =>
{
    services
        .Configure<Features>(hostContext.Configuration.GetSection(nameof(Features)))
        .AddOptions<Features>()
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var authConfigurationSection = builder.Configuration.GetSection("AzureAd");

    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(authConfigurationSection);

    MemoryCacheOptions memoryCacheOptions = new MemoryCacheOptions();
    IMemoryCache memoryCache = new MemoryCache(Options.Create(memoryCacheOptions));

    services.AddSingleton(memoryCache);

    services.AddControllers();
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();