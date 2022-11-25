using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;
using OrganisationRegistryTools.Import;

namespace OrganisationRegistryTools.OR_962.FixMainLocations;

public class DuplicateLocationRemover
{
    private static readonly Guid KBOLocationTypeId = new("537C0B5B-8AB8-FC3D-0B37-F8249CBDD3BA");
    private static readonly Guid MaatschappelijkeZetelTypeId = new("2a6c2912-6202-1693-e3b8-c9db9f33aec4");

    public static async Task ProcessFile(HttpClient client, Action<string> writeOutput)
    {
        try
        {
            var organisations =
                await Importer.GetOrganisations<Organisation>(client, "/v1/search/organisations?q=*&scroll=true");

            await ProcessOrganisations(client, writeOutput, organisations);

        }
        catch (Exception e)
        {
            writeOutput($"Processing ended with error: {e.Message}");
        }
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
        writeOutput($"Processing locations for organisation {organisation.Id} ({organisation.OvoNumber}):");
        
        const string noLocationsRemovedBecause = "\tNo locations removed because";
        
        if (string.IsNullOrWhiteSpace(organisation.KboNumber))
        {
            writeOutput($"{noLocationsRemovedBecause} no Kbo Number");
            return;
        }

        if (!(organisation.Locations?.Any() ?? false))
        {
            writeOutput($"{noLocationsRemovedBecause} no locations found");
            return;
        }

        var maybeKboLocation = organisation.Locations
            .FirstOrDefault(l => l.LocationTypeId == KBOLocationTypeId && l.Validity.OverlapsWithToday());
        if (maybeKboLocation is not { } kboLocation)
        {
            writeOutput($"{noLocationsRemovedBecause} no valid location 'Maatschappelijke zetel volgens KBO' found");
            return;
        }
        
        var possibleLocationsToRemove = organisation.Locations
            .Where(l => l.Validity.OverlapsWithToday())
            .Where(l => l.LocationId == kboLocation.LocationId)
            .Where(l => l.LocationTypeId is null || l.LocationTypeId == MaatschappelijkeZetelTypeId);

        foreach (var location in possibleLocationsToRemove)
        {
            if (location.IsMainLocation)
            {
                writeOutput($"{noLocationsRemovedBecause} this location is 'hoofdlocatie'");
                continue;
            }

            await RemoveLocation(client, organisation, location, writeOutput);
        }
    }

    private static async Task RemoveLocation(HttpClient client, Organisation organisation, Location location,
        Action<string> writeOutput)
    {
        var targetUri = $"v1/organisations/{organisation.Id}/locations/{location.OrganisationLocationId}";

        try
        {
            var updateResponse = await client.DeleteAsync(targetUri);

            writeOutput("\tAttempted remove location:\n" +
                        $"\t{targetUri}\n" +
                        $"\t\tovonummer: {organisation.OvoNumber}\n" +
                        $"\t\tsuccess: {updateResponse.IsSuccessStatusCode}\n" +
                        $"\t\tstatusCode: {updateResponse.StatusCode}\n");
            if (!updateResponse.IsSuccessStatusCode)
                writeOutput($"\t\terror: {await updateResponse.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex)
        {
            writeOutput($"\tError occured when attempting remove location:\n" +
                        $"\t\t{targetUri}\n" +
                        $"\t\tovonummer: {organisation.OvoNumber}\n" +
                        $"\t\tError: {ex.Message}");
        }
    }

    public class Organisation
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string OvoNumber { get; set; } = null!;
        public List<Location> Locations { get; set; } = null!;
        public string? KboNumber { get; set; }
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