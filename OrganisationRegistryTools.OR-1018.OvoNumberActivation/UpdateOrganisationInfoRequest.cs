namespace OrganisationRegistryTools.VlimpersFlagImport;

public class UpdateOrganisationInfoRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ShortName { get; set; }
    public List<Guid>? PurposeIds { get; set; }
    public bool ShowOnVlaamseOverheidSites { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Article { get; set; }
    public DateTime? OperationalValidFrom { get; set; }
    public DateTime? OperationalValidTo { get; set; }
}
