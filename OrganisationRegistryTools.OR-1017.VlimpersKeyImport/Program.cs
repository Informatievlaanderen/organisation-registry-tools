using System.Reflection;
using OrganisationRegistryTools.Import;
using OrganisationRegistryTools.OR_1017.VlimpersKeyImport;

Console.WriteLine("Please enter an authToken:");
var token = Console.ReadLine() ?? "";
Console.WriteLine("\nPlease enter the path where to find import files:");
var path = Console.ReadLine();
if (string.IsNullOrWhiteSpace(path))
    path = Path.Join(Directory
            .GetParent(typeof(Program).GetTypeInfo().Assembly.Location)?.Parent?.Parent?.Parent?.FullName,
        "Inputs");
Console.WriteLine("\nPlease enter the environment to which you want to import ([L]ocal, [s]taging, [p]roduction):");
var env = Console.ReadLine() ?? "l";
Console.WriteLine("\n");

await Importer.Run(VlimpersKeyEndDateImporter.ProcessFile, token, path, GetHost(env));

Hosts GetHost(string arg)
    => arg switch
    {
        "s" => Hosts.Staging,
        "p" => Hosts.Production,
        _ => Hosts.Local
    };