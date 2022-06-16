using OrganisationRegistryTools.Import;

namespace OrganisationRegistryTools.OR_935.SyncWithKBO;

public static class KboSynchronisatie
{
    public static async Task Start(HttpClient client, Action<string> output)
    {
        var allOrganisations = await Importer.GetRelevantOrganisations<Organisation>(
            client,
            "/v1/search/organisations?" +
            "q=kboNumber:/[0123456789][0123456789][0123456789][0123456789][0123456789][0123456789][0123456789][0123456789][0123456789][0123456789]/" +
            "&scroll=true");

        await SyncOrganisations(client, output, allOrganisations);
    }

    private static async Task SyncOrganisations(HttpClient client, Action<string> output,
        List<Organisation> allOrganisations)
    {
        foreach (var organisation in allOrganisations)
        {
            await TrySyncOrganisation(client, output, organisation);
        }
    }

    private static async Task TrySyncOrganisation(HttpClient client, Action<string> writeOutput, Organisation organisation)
    {
        try
        {
            var targetUri =
                $"v1/organisations/{organisation.Id}/kbo/sync";

            var updateResponse = await client.PutAsync(targetUri, new StringContent(""));

            writeOutput($"{targetUri}\n" +
                        $"\tsuccess: {updateResponse.IsSuccessStatusCode}\n" +
                        $"\tstatusCode: {updateResponse.StatusCode}\n" +
                        $"\terror: {await updateResponse.Content.ReadAsStringAsync()}");        }
        catch (Exception ex)
        {
            writeOutput($"{organisation.Id}: {ex.Message}");
        }
    }

    public class Organisation
    {
        public Guid Id { get; set; }
    }
}
