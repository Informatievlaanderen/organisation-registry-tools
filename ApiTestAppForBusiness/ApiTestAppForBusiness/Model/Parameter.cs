namespace ApiTestAppForBusiness.Model;

public class Parameter
{
    public string Type { get; set; } = null!; // Route, Query or Body
    public string DataType { get; set; } = null!; // string, guid, object
    public string? Name { get; set; }
    public string? Structure { get; set; }
}
