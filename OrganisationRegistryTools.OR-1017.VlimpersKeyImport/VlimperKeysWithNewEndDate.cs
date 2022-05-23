using CsvHelper.Configuration.Attributes;

namespace OrganisationRegistryTools.OR_1017.VlimpersKeyImport;

public class VlimperKeysWithNewEndDate
{
    [Index(0)] public string OvoNumber { get; set; }
    [Index(1)] public string VlimpersKey { get; set; } = null!;
    [Format("d/MM/yyyy")] [Index(2)] public DateTime EndDate { get; set; }
}