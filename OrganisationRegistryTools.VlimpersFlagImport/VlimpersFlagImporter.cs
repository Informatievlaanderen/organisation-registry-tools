using System.Globalization;
using System.Net.Mime;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;

namespace OrganisationRegistryTools.VlimpersFlagImport;

public class VlimpersFlagImporter
{
    public static async Task ImportRecords(HttpClient client, string path, JsonSerializerSettings jsonSerializerSettings)
    {
        var filesToProcess = Directory.EnumerateFiles(path);

        foreach (var input in filesToProcess.Where(s => !s.Contains(".result.")))
        {
            var directoryName = Path.GetDirectoryName(input);

            if (directoryName == null)
                throw new NullReferenceException("directoryName");
            
            var output = Path.Combine(directoryName, $"{Path.GetFileNameWithoutExtension(input)}.result{Path.GetExtension(input)}");

            await ProcessFile(client, input, output, jsonSerializerSettings);
        }
    }

    private static async Task ProcessFile(HttpClient client, string input, string output, JsonSerializerSettings jsonSerializerSettings)
    {
        await using var writer = File.CreateText(output);

        var csvFileConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true
        };

        var inputLines = await File.ReadAllTextAsync(input);
        using var reader = new CsvReader(new StringReader(inputLines), csvFileConfiguration);
        await foreach (var updateInfo in reader.GetRecordsAsync<OrganisationToChangeVlimpersFor>())
            try
            {
                await DoWork(client, jsonSerializerSettings, updateInfo);
                Console.WriteLine("    Success");
            }
            catch (Exception ex)
            {
                var message = $"[{updateInfo.OrganisationId}]{Environment.NewLine}{ex.Message}";
                await writer.WriteLineAsync(message);
                Console.WriteLine($"    {message}");
            }
    }

    private static async Task DoWork(HttpClient client, JsonSerializerSettings jsonSerializerSettings, OrganisationToChangeVlimpersFor flagToUpdate)
    {
        var targetUri = $"v1/organisations/{flagToUpdate.OrganisationId}/vlimpers";

        Console.WriteLine($"Patching host {client.BaseAddress}, resource {targetUri}, setting VlimpersFlag=true");

        var updateResponse = await client.PatchAsync(targetUri,
            new StringContent(
                JsonConvert.SerializeObject(new UpdateOrganisationVlimpersFlag
                {
                    VlimpersManagement = true
                }), Encoding.UTF8, MediaTypeNames.Application.Json));

        if (!updateResponse.IsSuccessStatusCode)
            throw new Exception($"Couldn't update info: \n{await updateResponse.Content.ReadAsStringAsync()}");
    }
}