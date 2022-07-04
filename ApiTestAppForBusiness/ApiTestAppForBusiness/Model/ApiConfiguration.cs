using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ApiTestAppForBusiness.Model;

public class ApiConfiguration
{
    private ApiConfiguration(List<Environment> environments)
    {
        Environments = environments;
    }

    public static ApiConfiguration CreateInstance(Configuration configuration)
    {
        var configEnvironments = configuration.GetSectionGroup("Environments");

        var environments = configEnvironments.Sections
            .Cast<ConfigurationSection>()
            .Select(environmentConfigSection => Environment.CreateInstance(environmentConfigSection.CurrentConfiguration))
            .ToList();

        return new ApiConfiguration(environments);
    }

    public List<Environment> Environments { get; set; }
}
