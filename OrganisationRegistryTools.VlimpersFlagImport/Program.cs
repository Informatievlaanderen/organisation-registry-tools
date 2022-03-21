using OrganisationRegistryTools.Import;
using OrganisationRegistryTools.VlimpersFlagImport;

Console.WriteLine("Please enter an authToken:");
var token = Console.ReadLine() ?? "";
Console.WriteLine("\nPlease enter the path where to find import files:");
var path = Console.ReadLine() ?? "";
Console.WriteLine("\nPlease enter the environment to which you want to import ([L]ocal, [s]taging, [p]roduction):");
var env = Console.ReadLine() ?? "l";
Console.WriteLine("\n");

await Importer.Run(VlimpersFlagImporter.ImportRecords, token, path, GetHost(env));

Hosts GetHost(string arg)
    => arg switch
    {
        "s" => Hosts.Staging,
        "p" => Hosts.Production,
        _ => Hosts.Local
    };