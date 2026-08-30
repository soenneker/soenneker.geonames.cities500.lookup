using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GeoNames.Cities500.Lookup.Abstract;

/// <summary>
/// Provides indexed access to the packaged US city, state, and coordinate extract from GeoNames cities500 data.
/// </summary>
public interface IGeonamesCities500Lookup
{
    /// <summary>
    /// Gets every packaged US city record in source-file order.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An immutable collection of all records.</returns>
    ValueTask<IReadOnlyCollection<GeoNamesRecord>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets records whose normalized city name matches <paramref name="city"/>.
    /// </summary>
    /// <param name="city">City name to search for.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An immutable list of matching records, or an empty list when no match exists.</returns>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCity(string city, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all records in a state. A two-letter postal abbreviation or full state name is accepted.
    /// </summary>
    /// <param name="state">The state postal abbreviation or full name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An immutable list of matching records, or an empty list when the state is unknown.</returns>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByState(string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets records matching a normalized city name within a state.
    /// </summary>
    /// <param name="city">City name to search for.</param>
    /// <param name="state">The state postal abbreviation or full name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An immutable list of matching records, or an empty list when no match exists.</returns>
    ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCityAndState(string city, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first source-file record matching a normalized city name within a state.
    /// </summary>
    /// <param name="city">City name to search for.</param>
    /// <param name="state">The state postal abbreviation or full name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The first matching record, or <see langword="null"/> when no match exists.</returns>
    ValueTask<GeoNamesRecord?> GetBestByCityAndState(string city, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets coordinates from the first source-file record matching a normalized city name within a state.
    /// </summary>
    /// <param name="city">City name to search for.</param>
    /// <param name="state">The state postal abbreviation or full name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching coordinates, or <see langword="null"/> when no match exists.</returns>
    ValueTask<GeoNamesCoordinates?> GetCoordinatesByCityAndState(string city, string state, CancellationToken cancellationToken = default);
}
