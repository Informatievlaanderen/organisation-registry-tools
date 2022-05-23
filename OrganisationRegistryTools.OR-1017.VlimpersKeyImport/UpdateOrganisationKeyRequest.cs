namespace OrganisationRegistryTools.OR_1017.VlimpersKeyImport;

public class UpdateOrganisationKeyRequest
{
    public Guid OrganisationKeyId { get; set; }
    public Guid KeyTypeId { get; set; }
    public string KeyValue { get; set; } = null!;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}