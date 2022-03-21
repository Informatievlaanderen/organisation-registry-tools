using CsvHelper.Configuration.Attributes;

namespace OrganisationRegistryTools.VlimpersFlagImport;

public class OrganisationToChangeVlimpersFor
{
    [Index(0)] public Guid OrganisationId { get; set; }
}