using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ApiTestAppForBusiness.Model;

public class Environment
{
    private Environment()
    { }

    public static Environment CreateInstance(Configuration configuration)
    {
        var groups = configuration.GetSectionGroup("groups");

        foreach (var groupNames in groups.Sections.Cast<string>())
        {
            
        }
        
        return new();
    }

    public string Name { get; set; } = null!;
    public Uri BaseUri { get; set; } = null!;

    public List<Group> Groups { get; set; } = new();
}
