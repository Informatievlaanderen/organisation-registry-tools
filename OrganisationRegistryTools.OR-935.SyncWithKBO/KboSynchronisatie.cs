using System.Net;
using OrganisationRegistryTools.Import;

namespace OrganisationRegistryTools.OR_935.SyncWithKBO;

public static class KboSynchronisatie
{
    public record LogItem(string Uri, bool IsSuccess, Guid OrganisationId, string Error, HttpStatusCode? HttpCode = null);
    
    public static async Task Start(HttpClient client, HashSet<Guid> organisationIdsToSkip, Action<LogItem> output)
    {
        var allOrganisations = await Importer.GetRelevantOrganisations<Organisation>(
            client,
            "/v1/search/organisations?" +
            "q=kboNumber:/[0123456789][0123456789][0123456789][0123456789][0123456789][0123456789][0123456789][0123456789][0123456789][0123456789]/" +
            "&scroll=true");

        await SyncOrganisations(client, organisationIdsToSkip, output, allOrganisations);
    }

    private static async Task SyncOrganisations(
        HttpClient client,
        IReadOnlySet<Guid> organisationIdsToSkip,
        Action<LogItem> output,
        IEnumerable<Organisation> allOrganisations)
    {
        var organisationsToSync = allOrganisations.Where(organisation => !organisationIdsToSkip.Contains(organisation.Id)).ToList();

        var processedCount = 0;
        var totalCount = organisationsToSync.Count;
        
        Console.WriteLine($"Start syncing {totalCount} organisations");
        foreach (var organisation in organisationsToSync)
        {
            await TrySyncOrganisation(client, output, organisation);

            processedCount++;
            if (processedCount % 10 == 0)
                Console.WriteLine($"Synced {processedCount} of {totalCount} organisations");
        }
    }

    private static async Task TrySyncOrganisation(HttpClient client, Action<LogItem> writeOutput, Organisation organisation)
    {
        var targetUri = $"v1/organisations/{organisation.Id}/kbo/sync";

        try
        {
            var updateResponse = await client.PutAsync(targetUri, new StringContent(""));

            var updateResponseContent = await updateResponse.Content.ReadAsStringAsync();
            writeOutput(new LogItem(targetUri,
                updateResponse.IsSuccessStatusCode,
                organisation.Id,
                updateResponseContent,
                updateResponse.StatusCode));
        }
        catch (Exception ex)
        {
            writeOutput(new LogItem(targetUri, false, organisation.Id, ex.Message));
        }
    }

    public class Organisation
    {
        public Guid Id { get; set; }
    }
}
