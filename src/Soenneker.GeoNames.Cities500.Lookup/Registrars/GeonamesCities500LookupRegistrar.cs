using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.File.Registrars;
using Soenneker.Utils.Paths.Resources.Registrars;
using Soenneker.GeoNames.Cities500.Lookup.Abstract;

namespace Soenneker.GeoNames.Cities500.Lookup.Registrars;

/// <summary>
/// A lookup util for GeoNames cities500 data, provided by GeoNames and updated daily.
/// </summary>
public static class GeonamesCities500LookupRegistrar
{
    /// <summary>
    /// Adds <see cref="IGeonamesCities500Lookup"/> as a singleton service.
    /// </summary>
    public static IServiceCollection AddGeonamesCities500LookupAsSingleton(this IServiceCollection services)
    {
        services.AddFileUtilAsSingleton()
                .AddResourcesPathUtilAsSingleton()
                .TryAddSingleton<IGeonamesCities500Lookup, GeonamesCities500Lookup>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IGeonamesCities500Lookup"/> as a scoped service.
    /// </summary>
    public static IServiceCollection AddGeonamesCities500LookupAsScoped(this IServiceCollection services)
    {
        services.AddFileUtilAsScoped()
                .AddResourcesPathUtilAsScoped()
                .TryAddScoped<IGeonamesCities500Lookup, GeonamesCities500Lookup>();

        return services;
    }
}
