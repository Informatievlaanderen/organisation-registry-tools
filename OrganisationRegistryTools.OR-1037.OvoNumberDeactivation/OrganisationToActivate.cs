using CsvHelper.Configuration.Attributes;

namespace OrganisationRegistryTools.VlimpersFlagImport;

public class OrganisationToActivate
{
    [Index(0)] public string OvoNumber { get; set; }
    [Format("d/MM/yyyy")] [Index(1)] public DateTime EndDate { get; set; }
}