using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GeoNames.Cities500.Lookup.Abstract;

/// <summary>
/// A lookup util for US cities with populations under 500, provided by GeoNames, updated daily.
/// </summary>
public interface IGeonamesCities500Lookup
{
    /// <summary>
    /// Gets all GeoNames records.
    /// </summary>
    ValueTask<IReadOnlyCollection<GeoNamesRecord>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a GeoNames record by geonameid.
    /// </summary>
    ValueTask<GeoNamesRecord?> Get(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all GeoNames records with the provided name.
    /// </summary>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByName(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all GeoNames records in the provided country code.
    /// </summary>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCountryCode(string countryCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get latitude and longitude for a GeoNames record.
    /// </summary>
    ValueTask<GeoNamesCoordinates?> GetCoordinates(int id, CancellationToken cancellationToken = default);
}
