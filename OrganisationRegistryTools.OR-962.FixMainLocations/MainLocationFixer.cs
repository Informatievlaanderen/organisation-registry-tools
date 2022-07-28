using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;
using OrganisationRegistryTools.Import;

namespace OrganisationRegistryTools.OR_962.FixMainLocations;

public class MainLocationFixer
{
    private static readonly Guid KBOLocationTypeId = new("537C0B5B-8AB8-FC3D-0B37-F8249CBDD3BA");

    public static async Task ProcessFile(HttpClient client, Action<string> writeOutput)
    {
        var organisations =
            await Importer.GetOrganisations<Organisation>(client, "/v1/search/organisations?q=*&scroll=true");

        await ProcessOrganisations(client, writeOutput, organisations);
    }

    private static async Task ProcessOrganisations(HttpClient client, Action<string> writeOutput,
        List<Organisation> organisations)
    {
        foreach (var organisation in organisations)
        {
            await ProcessLocations(client, writeOutput, organisation);
        }
    }

    private static async Task ProcessLocations(HttpClient client, Action<string> writeOutput, Organisation organisation)
    {
        if (!(organisation.Locations?.Any() ?? false))
        {
            writeOutput($"no locations found for organisation {organisation.Id}");
            return;
        }
        
        var maybeKboLocation = organisation.Locations.FirstOrDefault(l => l.LocationTypeId == KBOLocationTypeId);
        var maybeMainLocation = organisation.Locations.FirstOrDefault(l => l.IsMainLocation);

        if (maybeMainLocation is { } mainLocation
            && maybeKboLocation is { } kboLocation
            && kboLocation.LocationId == mainLocation.LocationId
            && kboLocation.OrganisationLocationId != mainLocation.OrganisationLocationId)
        {
            await UpdateLocationMainLocation(client, organisation.Id, mainLocation, writeOutput, "", false);
            await UpdateLocationMainLocation(client, organisation.Id, kboLocation, writeOutput, "KBO", true);
        }
    }

    private static async Task UpdateLocationMainLocation(HttpClient client, Guid organisationId, Location location, Action<string> writeOutput, string source, bool isMainLocation)
    {
        var targetUri = $"v1/organisations/{organisationId}/locations/{location.OrganisationLocationId}";

        try
        {
            var updateResponse = await client.PutAsync(targetUri,
                new StringContent(
                    JsonConvert.SerializeObject(new UpdateOrganisationLocationRequest
                    {
                        OrganisationLocationId = location.OrganisationLocationId,
                        IsMainLocation = isMainLocation,
                        LocationId = location.LocationId,
                        LocationTypeId = location.LocationTypeId,
                        Source = source,
                        ValidFrom = location.Validity.Start,
                        ValidTo = location.Validity.End,
                    }), Encoding.UTF8, MediaTypeNames.Application.Json));

            writeOutput($"{targetUri}\n" +
                        $"\tsuccess: {updateResponse.IsSuccessStatusCode}\n" +
                        $"\tstatusCode: {updateResponse.StatusCode}\n");
            if (!updateResponse.IsSuccessStatusCode) 
                writeOutput($"\terror: {await updateResponse.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex)
        {
            writeOutput($"{targetUri}\n" +
                        $"\tError: {ex.Message}");
        }
    }
    
    public class UpdateOrganisationLocationRequest
    {
        public Guid OrganisationLocationId { get; set; }
        public Guid LocationId { get; set; }
        public bool IsMainLocation { get; set; }
        public Guid? LocationTypeId { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? Source { get; set; }
    }
    
    public class Organisation
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string OvoNumber { get; set; } = null!;
        public List<Location> Locations { get; set; } = null!;
    }

    public class Location
    {
        public Guid OrganisationLocationId { get; set; }
        public Guid LocationId { get; set; }
        public string FormattedAddress { get; set; }
        public bool IsMainLocation { get; set; }
        public Guid? LocationTypeId { get; set; }
        public string? LocationTypeName { get; set; }
        public Validity Validity { get; set; }
    }

    public class Validity
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public bool OverlapsWithToday() =>
            (!Start.HasValue || Start.Value <= DateTime.Today) &&
            (!End.HasValue || End.Value >= DateTime.Today);
    }
}
