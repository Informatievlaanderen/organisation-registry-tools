using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace ApiTestAppForBusiness.Model;

public class Environment
{
    public static Environment CreateInstance(IConfiguration environmentConfiguration,
        IConfiguration globalConfiguration)
    {
        var environment = environmentConfiguration.Get<Environment>();
        var groups = environmentConfiguration.GetSection("groups").Get<string[]>();
        environment.Groups = groups.Select(g => globalConfiguration.GetSection(g).Get<Group>()).ToList();
        return environment;
    }

    public string Name { get; set; } = null!;
    public Uri BaseUri { get; set; } = null!;
    public List<Group> Groups { get; set; } = null!;
}
