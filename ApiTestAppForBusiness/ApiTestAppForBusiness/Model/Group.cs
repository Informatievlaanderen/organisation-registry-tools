using System.Collections.Generic;

namespace ApiTestAppForBusiness.Model;

public class Group
{
    public string Name { get; set; }= null!;
    public IEnumerable<Action> Actions { get; set; } = null!;
}
