namespace Soenneker.GeoNames.Cities500.Lookup;

/// <summary>
/// A US city coordinate record.
/// </summary>
public sealed record GeoNamesRecord(string City, string State, double Latitude, double Longitude);
