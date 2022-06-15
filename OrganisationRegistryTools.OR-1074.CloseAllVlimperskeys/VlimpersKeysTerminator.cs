using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;

namespace OrganisationRegistryTools.OR_1074.CloseAllVlimperskeys;

// I'll be back
public class VlimpersKeysTerminator
{
    public static Guid VlimpersKeyTypeId = new Guid("922a46bb-1378-45bd-a61f-b6bbf348a4d5");
    public static Guid VlimpersKortKeyTypeId = new Guid("E6CA1966-4149-4F49-A84B-62B21C363280");

    public static async Task ProcessFile(HttpClient client, Action<string> writeOutput)
    {
        // await using var writer = File.CreateText(output);

        var response =
            await client.GetAsync("/v1/search/organisations?" +
                                  $"q=(keys.keyTypeId:{VlimpersKeyTypeId.ToString().ToLower()}%20OR%20keys.keyTypeId:{VlimpersKortKeyTypeId.ToString().ToLower()})" +
                                  "&scroll=true");
        response.EnsureSuccessStatusCode();

        var searchResponseHeader =
            JsonConvert.DeserializeObject<SearchResponseHeader>(response.Headers
                .GetValues(SearchConstants.SearchMetaDataHeaderName).First());
        var content = await response.Content.ReadAsStringAsync();
        var maybeOrganisations = JsonConvert.DeserializeObject<List<Organisation>>(content);

        var allOrganisations = new List<Organisation>();

        while (maybeOrganisations is { } organisations && organisations.Any())
        {
            var scrollResponse =
                await client.GetAsync($"/v1/search/organisations/scroll?id={searchResponseHeader.ScrollId}");

            scrollResponse.EnsureSuccessStatusCode();

            searchResponseHeader =
                JsonConvert.DeserializeObject<SearchResponseHeader>(response.Headers
                    .GetValues(SearchConstants.SearchMetaDataHeaderName).First());

            content = await scrollResponse.Content.ReadAsStringAsync();
            maybeOrganisations = JsonConvert.DeserializeObject<List<Organisation>>(content);

            allOrganisations.AddRange(maybeOrganisations);
        }

        await ProcessOrganisations(client, writeOutput, allOrganisations);
    }

    private static async Task ProcessOrganisations(HttpClient client, Action<string> writeOutput,
        List<Organisation> organisations)
    {
        foreach (var organisation in organisations)
        {
            await ProcessKeys(client, writeOutput, organisation);
        }
    }

    private static async Task ProcessKeys(HttpClient client, Action<string> writeOutput, Organisation organisation)
    {
        foreach (var key in organisation.Keys
                     .Where(x => (x.KeyTypeId == VlimpersKeyTypeId || x.KeyTypeId == VlimpersKortKeyTypeId) &&
                                 x.Validity.End == null))
        {
            try
            {
                var targetUri =
                    $"v1/organisations/{organisation.Id}/keys/{key.OrganisationKeyId}";

                var updateResponse = await client.PutAsync(targetUri,
                    new StringContent(
                        JsonConvert.SerializeObject(new UpdateOrganisationKeyRequest
                        {
                            OrganisationKeyId = key.OrganisationKeyId,
                            KeyValue = key.Value,
                            ValidFrom = key.Validity.Start,
                            KeyTypeId = key.KeyTypeId,
                            ValidTo = new DateTime(2022, 6, 30)
                        }), Encoding.UTF8, MediaTypeNames.Application.Json));

                writeOutput(
                    $"{key.OrganisationKeyId}: {updateResponse.IsSuccessStatusCode} {updateResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                writeOutput($"{key.OrganisationKeyId}: {ex.Message}");
            }
        }
    }

    internal class SearchResponseHeader
    {
        public string ScrollId { get; set; }
    }

    public class SearchConstants
    {
        public const string SearchMetaDataHeaderName = "x-search-metadata";
    }

    public class Organisation
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string OvoNumber { get; set; }
        public List<Key> Keys { get; set; }
    }

    public class Key
    {
        public Guid OrganisationKeyId { get; set; }

        public Guid KeyTypeId { get; set; }
        public string KeyTypeName { get; set; }
        public string Value { get; set; }
        public Validity Validity { get; set; }
    }

    public class Validity
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public bool OverlapsWithToday()
        {
            return (!Start.HasValue || Start.Value <= DateTime.Today) &&
                   (!End.HasValue || End.Value >= DateTime.Today);
        }
    }

    public class UpdateOrganisationKeyRequest
    {
        public Guid OrganisationKeyId { get; set; }
        public Guid KeyTypeId { get; set; }
        public string KeyValue { get; set; } = null!;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
}
