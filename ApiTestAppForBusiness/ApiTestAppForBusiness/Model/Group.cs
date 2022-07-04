using System.Collections.Generic;

namespace ApiTestAppForBusiness.Model;

public class Group
{
    public string Name { get; set; } = null!;
    public string RelativeUri { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;

    public List<Parameter> Parameters { get; set; } = new();
}
