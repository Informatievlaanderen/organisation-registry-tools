using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;
using OrganisationRegistryTools.Import;

namespace OrganisationRegistryTools.OR_1074.CloseAllVlimperskeys;

// I'll be back
public class VlimpersKeysTerminator
{
    public static Guid VlimpersKeyTypeId = new Guid("922a46bb-1378-45bd-a61f-b6bbf348a4d5");
    public static Guid VlimpersKortKeyTypeId = new Guid("E6CA1966-4149-4F49-A84B-62B21C363280");

    public static async Task ProcessFile(HttpClient client, Action<string> writeOutput)
    {
        var organisations = await Importer.GetOrganisations<Organisation>(
            client,
            "/v1/search/organisations?" +
            $"q=(keys.keyTypeId:{VlimpersKeyTypeId.ToString().ToLower()}%20OR%20keys.keyTypeId:{VlimpersKortKeyTypeId.ToString().ToLower()})" +
            "&scroll=true");

        await ProcessOrganisations(client, writeOutput, organisations);
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

                writeOutput($"/v1/organisations/{organisation.Id}/keys/{key.OrganisationKeyId}\n" +
                            $"\tsuccess: {updateResponse.IsSuccessStatusCode}\n" +
                            $"\tstatusCode: {updateResponse.StatusCode}\n" +
                            $"\terror: {await updateResponse.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                writeOutput($"{key.OrganisationKeyId}: {ex.Message}");
            }
        }
    }

    public class Organisation
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string OvoNumber { get; set; } = null!;
        public List<Key> Keys { get; set; } = null!;
    }

    public class Key
    {
        public Guid OrganisationKeyId { get; set; }

        public Guid KeyTypeId { get; set; }
        public string KeyTypeName { get; set; } = null!;
        public string Value { get; set; } = null!;
        public Validity Validity { get; set; } = null!;
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
