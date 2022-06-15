

// 1) Alle organisations ophalen (Projectie ?)

// 2) Keys ophalen, bepalen welke keys Vlimperskeys zijn die nog niet werden afgesloten

// 3) command 'UpdateOrganisationKeyRequest' sturen, met einddatum '30/6/2022', voor de gevonden Vlimperskeys zonder einddatum

using OrganisationRegistryTools.Import;
using OrganisationRegistryTools.OR_1074.CloseAllVlimperskeys;
Console.WriteLine("Please enter an authToken:");
var token = Console.ReadLine() ?? "";

Console.WriteLine("\nPlease enter the environment to which you want to import ([L]ocal, [s]taging, [p]roduction):");
var env = Console.ReadLine() ?? "l";
Console.WriteLine("\n");

var client = Importer.GetClient(token, GetHost(env));

var file = File.Create("output.txt");
var fileWriter = new StreamWriter(file);

await VlimpersKeysTerminator.ProcessFile(client, output =>
{
    Console.WriteLine(output);
    fileWriter.WriteLine(output);
});

Hosts GetHost(string arg)
    => arg switch
    {
        "s" => Hosts.Staging,
        "p" => Hosts.Production,
        _ => Hosts.Local
    };
