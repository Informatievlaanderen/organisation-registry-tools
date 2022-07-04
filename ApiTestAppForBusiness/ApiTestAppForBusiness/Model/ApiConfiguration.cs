using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace ApiTestAppForBusiness.Model;

public class ApiConfiguration
{
    private static ApiConfiguration? _instance;

    private ApiConfiguration(List<Environment> environments)
    {
        Environments = environments;
    }

    public static ApiConfiguration GetInstance(IConfiguration configuration)
    {
        if (_instance is not { })
            _instance = GetInstanceFromConfig(configuration);
        return _instance;
    }

    public static ApiConfiguration GetInstance()
    {
        if (_instance is not { })
            throw new NullReferenceException("ApiConfiguration not initialised.");
        return _instance;
    }

    private static ApiConfiguration GetInstanceFromConfig(IConfiguration configuration)
    {
        var configEnvironments = configuration.GetSection("Environments");

        var environments = configEnvironments.GetChildren()
            .Select(environmentConfiguration => Environment.CreateInstance(environmentConfiguration, configuration))
            .ToList();

        return new ApiConfiguration(environments);
    }

    public List<Environment> Environments { get; }
}
