using OrganisationRegistryTools.Import;
using OrganisationRegistryTools.OR_935.SyncWithKBO;

const string successFileName = "success.txt";
const string errorsFileName = "errors.txt";

var organisationIdsToSkip = GetOrganisationIdsToSkip();

var client = Importer.GetClient();

await KboSynchronisatie.Start(client, organisationIdsToSkip, logItem =>
{
    if (logItem.IsSuccess)
    {
        Console.WriteLine($"{logItem.Uri}; success");
        File.AppendAllText(successFileName, $"{logItem.OrganisationId.ToString()}\n");
    }
    else
    {
        var errorToLog = GetErrorToLog(logItem);
        Console.WriteLine(errorToLog);
        File.AppendAllText(errorsFileName,$"{errorToLog}\n");  
    }
});

static string GetErrorToLog(KboSynchronisatie.LogItem logItem) =>
    logItem.HttpCode is { } httpCode
        ? $"{logItem.Uri}\n\tsuccess: {logItem.IsSuccess}\n\tstatusCode: {httpCode}\n\terror: {logItem.Error}"
        : $"{logItem.Uri}\n\tsuccess: {logItem.IsSuccess}\n\terror: {logItem.Error}";

static HashSet<Guid> GetOrganisationIdsToSkip()
{
    try
    {
        var lines = File.ReadAllLines(successFileName);
    
        var result = new HashSet<Guid>();
        foreach (var line in lines)
        {
            result.Add(new Guid(line));
        }

        return result;
    }
    catch (Exception e)
    {
        return new HashSet<Guid>();
    }
}
