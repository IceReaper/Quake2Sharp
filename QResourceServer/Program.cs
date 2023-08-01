using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using QEntitiesServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Logging;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureServices((hostContext, services) =>
{
    IdentityModelEventSource.ShowPII = true;

    services
        .Configure<ResourceAccessSettings>(hostContext.Configuration.GetSection(nameof(ResourceAccessSettings)))
        .AddOptions<ResourceAccessSettings>()
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var authConfigurationSection = builder.Configuration.GetSection("AzureAd");

    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(authConfigurationSection);

    // By default, the claims mapping will map claim names in the old format to accommodate older SAML applications.
    //'http://schemas.microsodt.com/ws/2008/06/identity/clains/role' instead of 'roles'
    // This flag ensures that the ClaimsIdentity claims collection will be build from the claims in the token
    JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

    MemoryCacheOptions memoryCacheOptions = new MemoryCacheOptions();
    IMemoryCache memoryCache = new MemoryCache(Options.Create(memoryCacheOptions));

    services.AddSingleton(memoryCache);

    services.AddControllers();
});

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    // Since IdentityModel version 5.2.1 (or since Microsoft.AspNetCore.Authentication.JwtBearer version 2.2.0),
    // PII hiding in log files is enabled by default for GDPR concerns.
    // For debugging/development purposes, one can enable additional detail in exceptions by setting IdentityModelEventSource.ShowPII to true.
    // Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
    app.UseDeveloperExceptionPage();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();