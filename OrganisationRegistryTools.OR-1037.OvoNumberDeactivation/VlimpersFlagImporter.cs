using System.Globalization;
using System.Net.Mime;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;

namespace OrganisationRegistryTools.VlimpersFlagImport;

public class VlimpersFlagImporter
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
        await foreach (var updateInfo in reader.GetRecordsAsync<OrganisationToActivate>())
            try
            {
                await ProcessRecord(client, updateInfo);
                Console.WriteLine("    Success");
            }
            catch (Exception ex)
            {
                var message = $"[{updateInfo.OvoNumber}]{Environment.NewLine}{ex.Message}";
                await writer.WriteLineAsync(message);
                Console.WriteLine($"    {message}");
            }
    }

    private static async Task ProcessRecord(HttpClient client, OrganisationToActivate organisationToActivate)
    {
        var organisationsResponse =
            await client.GetAsync($"v1/search/organisations?q=ovoNumber:\"{organisationToActivate.OvoNumber}\"");

        if (!organisationsResponse.IsSuccessStatusCode)
            throw new Exception(
                $"Could not fetch organisation. Response of type {organisationsResponse.StatusCode}: {await organisationsResponse.Content.ReadAsStringAsync()}");

        var organisationString = await organisationsResponse.Content.ReadAsStringAsync();
        var organisation = JsonConvert.DeserializeObject<List<OrganisationInfoResponse>>(organisationString)!.Single();

        var targetUri = $"v1/organisations/{organisation.Id}";

        Console.WriteLine($"Patching host {client.BaseAddress}, resource {targetUri}, setting OperationalValidTo = {organisationToActivate.EndDate}");

        var updateResponse = await client.PutAsync(targetUri,
            new StringContent(
                JsonConvert.SerializeObject(new UpdateOrganisationInfoRequest()
                {
                    Article = organisation.Article,
                    Description = organisation.Description,
                    Name = organisation.Name,
                    PurposeIds = organisation.PurposeIds,
                    ShortName = organisation.ShortName,
                    ValidFrom = organisation.Validity.Start,
                    ValidTo = organisation.Validity.End,
                    OperationalValidFrom = organisation.OperationalValidity.Start,
                    ShowOnVlaamseOverheidSites = organisation.ShowOnVlaamseOverheidSites,
                    OperationalValidTo = organisationToActivate.EndDate
                }), Encoding.UTF8, MediaTypeNames.Application.Json));

        if (!updateResponse.IsSuccessStatusCode)
            throw new Exception($"Couldn't update info: \n{await updateResponse.Content.ReadAsStringAsync()}");
    }

    private class OrganisationInfoResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ShortName { get; set; }
        public List<Guid>? PurposeIds { get; set; }
        public bool ShowOnVlaamseOverheidSites { get; set; }
        public Validity Validity { get; set; }
        public string? Article { get; set; }
        public Validity OperationalValidity { get; set; }
    }

    private class Validity
    {
        public DateTime? Start { get; set; } = null;
        public DateTime? End { get; set; } = null;
    }
}