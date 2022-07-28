// 1) Alle organisations ophalen (Projectie ?)
// 2) Keys ophalen, bepalen welke keys Vlimperskeys zijn die nog niet werden afgesloten
// 3) command 'UpdateOrganisationKeyRequest' sturen, met einddatum '30/6/2022', voor de gevonden Vlimperskeys zonder einddatum

using OrganisationRegistryTools.Import;
using OrganisationRegistryTools.OR_1074.CloseAllVlimperskeys;

var client = Importer.GetClient();

var file = File.Create("output.txt");
var fileWriter = new StreamWriter(file);

await VlimpersKeysTerminator.ProcessFile(client, output =>
{
    Console.WriteLine(output);
    fileWriter.WriteLine(output);
});
