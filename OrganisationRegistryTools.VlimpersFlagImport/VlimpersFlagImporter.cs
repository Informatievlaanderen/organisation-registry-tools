﻿using System.Globalization;
using System.Net.Mime;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;

namespace OrganisationRegistryTools.VlimpersFlagImport;

public class VlimpersFlagImporter
{
    public static async Task ProcessFile(HttpClient client, string input, string output, JsonSerializerSettings jsonSerializerSettings)
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
                await ProcessRecord(client, updateInfo);
                Console.WriteLine("    Success");
            }
            catch (Exception ex)
            {
                var message = $"[{updateInfo.OrganisationId}]{Environment.NewLine}{ex.Message}";
                await writer.WriteLineAsync(message);
                Console.WriteLine($"    {message}");
            }
    }

    private static async Task ProcessRecord(HttpClient client, OrganisationToChangeVlimpersFor flagToUpdate)
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