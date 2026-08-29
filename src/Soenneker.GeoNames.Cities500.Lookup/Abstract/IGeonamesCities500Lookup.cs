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
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the collection returned by get All.</returns>
    ValueTask<IReadOnlyCollection<GeoNamesRecord>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all US city records matching the provided city.
    /// </summary>
    /// <param name="city">City name to search for.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the collection returned by get By City.</returns>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCity(string city, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all US city records in the provided state. State may be a two-letter abbreviation or full state name.
    /// </summary>
    /// <param name="state">State value used by the variant.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the collection returned by get By State.</returns>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByState(string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all US city records matching the provided city and state. State may be a two-letter abbreviation or full state name.
    /// </summary>
    /// <param name="city">City name to search for.</param>
    /// <param name="state">State value used by the variant.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the collection returned by get By City And State.</returns>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCityAndState(string city, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first matching US city record for the provided city and state. State may be a two-letter abbreviation or full state name.
    /// </summary>
    /// <param name="city">City name to search for.</param>
    /// <param name="state">State value used by the variant.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested geo Names Record.</returns>
    ValueTask<GeoNamesRecord?> GetBestByCityAndState(string city, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets latitude and longitude for the first matching US city record for the provided city and state.
    /// </summary>
    /// <param name="city">City name to search for.</param>
    /// <param name="state">State value used by the variant.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested geo Names Coordinates.</returns>
    ValueTask<GeoNamesCoordinates?> GetCoordinatesByCityAndState(string city, string state, CancellationToken cancellationToken = default);
}
