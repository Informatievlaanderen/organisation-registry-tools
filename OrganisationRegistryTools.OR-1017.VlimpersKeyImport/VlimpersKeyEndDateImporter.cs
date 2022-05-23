using System.Globalization;
using System.Net.Mime;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;

namespace OrganisationRegistryTools.OR_1017.VlimpersKeyImport;

public class VlimpersKeyEndDateImporter
{
    public static async Task ProcessFile(HttpClient client, string input, string output,
        JsonSerializerSettings jsonSerializerSettings)
    {
        await using var writer = File.CreateText(output);

        var csvFileConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true
        };

        var inputLines = await File.ReadAllTextAsync(input);
        using var reader = new CsvReader(new StringReader(inputLines), csvFileConfiguration);
        await foreach (var updateInfo in reader.GetRecordsAsync<VlimperKeysWithNewEndDate>())
            try
            {
                await ProcessRecord(client, updateInfo);
                Console.WriteLine("    Success");
            }
            catch (Exception ex)
            {
                var message = $"[{updateInfo.VlimpersKey}]{Environment.NewLine}{ex.Message}";
                await writer.WriteLineAsync(message);
                Console.WriteLine($"    {message}");
            }
    }

    private static async Task ProcessRecord(HttpClient client, VlimperKeysWithNewEndDate vlimperKeyToUpdate)
    {
        /*ovoNumber:\"{vlimperKeyToUpdate.OvoNumber}\" AND */
        var organisationsResponse =
            await client.GetAsync($"v1/search/organisations?q=ovoNumber:\"{vlimperKeyToUpdate.OvoNumber}\" AND keys.value.keyword:\"{vlimperKeyToUpdate.VlimpersKey}\"");

        if (!organisationsResponse.IsSuccessStatusCode)
            throw new Exception(
                $"Could not fetch organisation. Response of type {organisationsResponse.StatusCode}: {await organisationsResponse.Content.ReadAsStringAsync()}");

        var organisationString = await organisationsResponse.Content.ReadAsStringAsync();
        var maybeOrganisations = JsonConvert.DeserializeObject<List<Organisation>>(organisationString);

        if (maybeOrganisations is not { } organisations)
            throw new Exception("No organisations found.");

        var maybeVlimperKeyToEndByOrganisation = organisations
            //.Where(org=>org.OvoNumber == vlimperKeyToUpdate.OvoNumber)
            .Select(org => (org, Key: org.Keys
                .Where(k => k.Value == vlimperKeyToUpdate.VlimpersKey)
                .MaxBy(k => k.Validity.End ?? DateTime.MaxValue)))
            .MaxBy(tuple => tuple.Key?.Validity.End ?? DateTime.MaxValue);

        if (maybeVlimperKeyToEndByOrganisation.Key is not { } vlimperKeyToEndByOrganisation) 
            throw new Exception("No keys found in organisation.");

        var targetUri =
            $"v1/organisations/{maybeVlimperKeyToEndByOrganisation.org.Id}/keys/{vlimperKeyToEndByOrganisation.KeyTypeId}";

        Console.WriteLine(
            $"Patching host {client.BaseAddress}, resource {targetUri}, setting EndDate={vlimperKeyToUpdate.EndDate}");

        var updateResponse = await client.PutAsync(targetUri,
            new StringContent(
                JsonConvert.SerializeObject(new UpdateOrganisationKeyRequest
                {
                    OrganisationKeyId = vlimperKeyToEndByOrganisation.OrganisationKeyId,
                    KeyValue = vlimperKeyToEndByOrganisation.Value,
                    ValidFrom = vlimperKeyToEndByOrganisation.Validity.Start,
                    KeyTypeId = vlimperKeyToEndByOrganisation.KeyTypeId,
                    ValidTo = vlimperKeyToUpdate.EndDate
                }), Encoding.UTF8, MediaTypeNames.Application.Json));

        if (!updateResponse.IsSuccessStatusCode)
            throw new Exception($"Couldn't update info for {maybeVlimperKeyToEndByOrganisation.org.OvoNumber}: \n{await updateResponse.Content.ReadAsStringAsync()}");
    }

    private class Organisation
    {
        public Guid Id { get; set; }
        public string OvoNumber { get; set; } = null!;
        public List<Key> Keys { get; set; } = null!;
    }

    private class Key
    {
        public Guid KeyTypeId { get; set; }
        public Guid OrganisationKeyId { get; set; }
        public string Value { get; set; } = null!;
        public Validity Validity { get; set; } = null!;
    }

    private class Validity
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }
}