using OrganisationRegistryTools.Import;
using OrganisationRegistryTools.OR_935.SyncWithKBO;

var client = Importer.GetClient();

var file = File.Create("output.txt");
var fileWriter = new StreamWriter(file);

await KboSynchronisatie.Start(client, output =>
{
    Console.WriteLine(output);
    fileWriter.WriteLine(output);
});
