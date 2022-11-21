using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;
using OrganisationRegistryTools.Import;

namespace OrganisationRegistryTools.OR_962.FixMainLocations;

public class MainLocationFixer
{
    private static readonly Guid KBOLocationTypeId = new("537C0B5B-8AB8-FC3D-0B37-F8249CBDD3BA");
    private static readonly Guid MaatschappelijkeZetelTypeId = new("2a6c2912-6202-1693-e3b8-c9db9f33aec4");

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
        writeOutput($"Processing locations for organisation {organisation.Id} ({organisation.OvoNumber}):");
        
        const string noLocationsChangedBecause = "\tNo locations changed because";
        
        if (string.IsNullOrWhiteSpace(organisation.KboNumber))
        {
            writeOutput($"{noLocationsChangedBecause} no Kbo Number");
            return;
        }

        if (!(organisation.Locations?.Any() ?? false))
        {
            writeOutput($"{noLocationsChangedBecause} no locations found");
            return;
        }

        var maybeKboLocation = organisation.Locations
            .FirstOrDefault(l => l.LocationTypeId == KBOLocationTypeId && l.Validity.OverlapsWithToday());
        if (maybeKboLocation is not { } kboLocation)
        {
            writeOutput(
                $"{noLocationsChangedBecause} no valid location 'Maatschappelijke zetel volgens KBO' found");
            return;
        }

        var maybeMainLocation = organisation.Locations
            .FirstOrDefault(l => l.IsMainLocation && l.Validity.OverlapsWithToday());
        if (maybeMainLocation is not { } mainLocation)
        {
            writeOutput($"{noLocationsChangedBecause} no valid main location found");
            return;
        }

        if (!(mainLocation.LocationTypeId is null || mainLocation.LocationTypeId == MaatschappelijkeZetelTypeId))
        {
            writeOutput($"{noLocationsChangedBecause} no valid location 'Maatschappelijke zetel' (or empty locationtype) found");
            return;
        }

        if (kboLocation.LocationId == mainLocation.LocationId)
        {
            await UpdateLocationMainLocation(client, organisation, mainLocation, writeOutput, "", false);
            await UpdateLocationMainLocation(client, organisation, kboLocation, writeOutput, "KBO", true);
        }
    }

    private static async Task UpdateLocationMainLocation(HttpClient client, Organisation organisation, Location location,
        Action<string> writeOutput, string source, bool isMainLocation)
    {
        var targetUri = $"v1/organisations/{organisation.Id}/locations/{location.OrganisationLocationId}";

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

            writeOutput("\tAttempted change location:\n" +
                        $"\t{targetUri}\n" +
                        $"\t\tsuccess: {updateResponse.IsSuccessStatusCode}\n" +
                        $"\t\tstatusCode: {updateResponse.StatusCode}\n");
            if (!updateResponse.IsSuccessStatusCode)
                writeOutput($"\t\terror: {await updateResponse.Content.ReadAsStringAsync()}");
        }
        catch (Exception ex)
        {
            writeOutput($"\tError occured when attempting change location:\n" +
                        $"\t\t{targetUri}\n" +
                        $"\t\tovonummer: {organisation.OvoNumber}\n" +
                        $"\t\tError: {ex.Message}");
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