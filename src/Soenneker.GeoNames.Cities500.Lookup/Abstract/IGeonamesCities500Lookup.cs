using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GeoNames.Cities500.Lookup.Abstract;

/// <summary>
/// A lookup util for US GeoNames cities500 data.
/// </summary>
public interface IGeonamesCities500Lookup
{
    /// <summary>
    /// Gets all US city records.
    /// </summary>
    ValueTask<IReadOnlyCollection<GeoNamesRecord>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all US city records matching the provided city.
    /// </summary>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCity(string city, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all US city records in the provided state. State may be a two-letter abbreviation or full state name.
    /// </summary>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByState(string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all US city records matching the provided city and state. State may be a two-letter abbreviation or full state name.
    /// </summary>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCityAndState(string city, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first matching US city record for the provided city and state. State may be a two-letter abbreviation or full state name.
    /// </summary>
    ValueTask<GeoNamesRecord?> GetBestByCityAndState(string city, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets latitude and longitude for the first matching US city record for the provided city and state.
    /// </summary>
    ValueTask<GeoNamesCoordinates?> GetCoordinatesByCityAndState(string city, string state, CancellationToken cancellationToken = default);
}
