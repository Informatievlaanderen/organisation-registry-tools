using CsvHelper.Configuration.Attributes;

namespace OrganisationRegistryTools.OR_1037.OvoNumberDeactivation;

public class OrganisationToActivate
{
    [Index(0)] public string OvoNumber { get; set; } = null!;
    [Format("d/MM/yyyy")] [Index(1)] public DateTime EndDate { get; set; }
}
