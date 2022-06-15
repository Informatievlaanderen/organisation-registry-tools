using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace OrganisationRegistryTools.Import;

public static class HostUrls
{
    public const string Local = "http://api.organisatie.dev-vlaanderen.local:9002";
    public const string Staging = "https://api.organisatie.dev-vlaanderen.be";
    public const string Production = "https://api.wegwijs.vlaanderen.be";
}

public enum Hosts
{
    Local,
    Staging,
    Production
}

public class Importer
{
    public static async Task Run(Func<HttpClient, string, string, JsonSerializerSettings, Task> processFile, string authToken, string path, Hosts hosts = Hosts.Local)
    {
        var client = GetClient(authToken, hosts);
        var jsonSerializerSettings = GetJsonSerializerSettings();

        await ImportRecords(processFile, client, path, jsonSerializerSettings);
    }

    public static HttpClient GetClient(string authToken, Hosts hosts)
    {
        var host = GetHostUrl(hosts);

        var client = new HttpClient
        {
            BaseAddress = new Uri(host),
            DefaultRequestHeaders =
            {
                { "Accept", "application/json" },
                { "Authorization", $"Bearer {authToken}" }
            }
        };

        return client;
    }

    public static JsonSerializerSettings GetJsonSerializerSettings()
    {
        var jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new OrganisationRegistryContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },

            MissingMemberHandling = MissingMemberHandling.Ignore,

            // Limit the object graph we'll consume to a fixed depth. This prevents stackoverflow exceptions
            // from deserialization errors that might occur from deeply nested objects.
            MaxDepth = 32,

            // Do not change this setting
            // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types
            TypeNameHandling = TypeNameHandling.None
        };

        jsonSerializerSettings.ContractResolver = new OrganisationRegistryContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

        // Limit the object graph we'll consume to a fixed depth. This prevents stackoverflow exceptions
        // from deserialization errors that might occur from deeply nested objects.
        jsonSerializerSettings.MaxDepth = 32;

        // Do not change this setting
        // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types
        jsonSerializerSettings.TypeNameHandling = TypeNameHandling.None;

        jsonSerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        jsonSerializerSettings.DateFormatString = "yyyy-MM-dd";
        jsonSerializerSettings.Converters.Add(new TrimStringConverter());
        jsonSerializerSettings.Converters.Add(new StringEnumConverter());
        jsonSerializerSettings.Converters.Add(new GuidConverter());

        JsonConvert.DefaultSettings =
            () => jsonSerializerSettings;
        return jsonSerializerSettings;
    }

    public static async Task ImportRecords(Func<HttpClient, string, string, JsonSerializerSettings, Task> processFile, HttpClient client, string path, JsonSerializerSettings jsonSerializerSettings)
    {
        var filesToProcess = Directory.EnumerateFiles(path);

        foreach (var input in filesToProcess.Where(s => !s.Contains(".result.")))
        {
            var directoryName = Path.GetDirectoryName(input);

            if (directoryName == null)
                throw new NullReferenceException("directoryName");

            var output = Path.Combine(directoryName, $"{Path.GetFileNameWithoutExtension(input)}.result{Path.GetExtension(input)}");

            await processFile(client, input, output, jsonSerializerSettings);
        }
        
        Console.WriteLine($"You can find the logfiles here: {path}");
    }

    private static string GetHostUrl(Hosts hosts)
        => hosts switch
        {
            Hosts.Local => HostUrls.Local,
            Hosts.Staging => HostUrls.Staging,
            Hosts.Production => HostUrls.Production,
            _ => throw new ArgumentOutOfRangeException(nameof(hosts), hosts, null)
        };
}
