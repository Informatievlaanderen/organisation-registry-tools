using CsvHelper.Configuration.Attributes;

namespace OrganisationRegistryTools.OR_1018.OvoNumberActivation;

public class OrganisationToActivate
{
    [Index(0)] public string OvoNumber { get; set; } = null!;
    [Format("d/MM/yyyy")] [Index(1)] public DateTime StartDate { get; set; }
}
