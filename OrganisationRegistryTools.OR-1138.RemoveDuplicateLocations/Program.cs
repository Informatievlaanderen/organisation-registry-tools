using OrganisationRegistryTools.Import;
using OrganisationRegistryTools.OR_962.FixMainLocations;

var client = Importer.GetClient();

var file = File.Create("output.txt");
var fileWriter = new StreamWriter(file);

await DuplicateLocationRemover.ProcessFile(client, output =>
{
    Console.WriteLine(output);
    fileWriter.WriteLine(output);
});
