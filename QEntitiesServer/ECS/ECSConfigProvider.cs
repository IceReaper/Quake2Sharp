using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace QEntitiesServer.ECS;

public sealed class ECSConfigProvider
{
    private const int PollingPeriodInSec = 30;

    private readonly ConcurrentDictionary<string, string> _ecsConfigurationValues;
    private readonly ECSAuthInfo _authInfo;

    public ECSConfigProvider(IOptions<ECSAuthInfo> authInfo)
    {
        _ecsConfigurationValues = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _authInfo = authInfo.Value;

        UpdateConfigurationOnce().GetAwaiter().GetResult();

        Task.Run(UpdateConfiguration);
    }

    public string GetValue(string key)
    {
        if (_ecsConfigurationValues.TryGetValue(key, value: out var value))
        {
            return value;
        }

        return string.Empty;
    }

    private async Task UpdateConfiguration()
    {
        while (true)
        {
            await UpdateConfigurationOnce();

            await Task.Delay(TimeSpan.FromSeconds(PollingPeriodInSec));
        }
    }

    private async Task UpdateConfigurationOnce()
    {
        try
        {
            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(
                _authInfo.TenantId,
                _authInfo.ClientId,
                _authInfo.ClientSecret);

            TokenRequestContext tokenRequestContext =
                new TokenRequestContext(
                    new string[] { "https://ecs.skype.com/.default", });

            string token = clientSecretCredential.GetToken(tokenRequestContext).Token;

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var builder = new UriBuilder("https://ecs.skype.net/api/v1/configurations");
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["client"] = "q2ffhackaton";
            query["team"] = "q2ffhackaton";
            query["details"] = "true";
            builder.Query = query.ToString();
            string url = builder.ToString();

            var httpResult = await httpClient.GetAsync(url);

            string result = await httpResult.Content.ReadAsStringAsync();

            PolulateDictionary(result);

            // TODO emit metric via open telemetry
            Console.WriteLine("Config updated.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void PolulateDictionary(string ecsConfigurationsJson)
    {
        JsonDocument doc = JsonDocument.Parse(ecsConfigurationsJson);
        JsonElement configurations = doc.RootElement.GetProperty("configurations");
        JsonElement.ArrayEnumerator configurationsEnumerator = configurations.EnumerateArray();

        while(configurationsEnumerator.MoveNext())
        {
            JsonElement current = configurationsEnumerator.Current;
            JsonElement configs = current.GetProperty("configs");

            JsonElement.ArrayEnumerator configsEnumerator = configs.EnumerateArray();

            while (configsEnumerator.MoveNext())
            {
                JsonElement config = configsEnumerator.Current.GetProperty("config");

                JsonElement.ObjectEnumerator configEnumerator = config.EnumerateObject();

                while (configEnumerator.MoveNext())
                {
                    JsonProperty element = configEnumerator.Current;

                    string key = element.Name;
                    string newValue = element.Value.GetString()!;

                    _ecsConfigurationValues.AddOrUpdate(
                        key,
                        newValue,
                        (_, __) => newValue);
                }
            }
        }    
    }
}