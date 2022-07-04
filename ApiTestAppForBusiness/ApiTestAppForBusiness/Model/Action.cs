using System.Collections.Generic;

namespace ApiTestAppForBusiness.Model;

public class Action
{
    public string Name { get; set; } = null!;
    public string RelativeUri { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;

    public IEnumerable<Parameter> Parameters { get; set; } = null!;
}
