using System;
using System.Collections.Generic;

namespace Soenneker.GeoNames.Cities500.Lookup;

/// <summary>
/// A GeoNames tab-delimited data record.
/// </summary>
public sealed record GeoNamesRecord(
    int Id,
    string Name,
    string AsciiName,
    double Latitude,
    double Longitude,
    string FeatureClass,
    string FeatureCode,
    string CountryCode,
    string Admin1Code,
    long Population,
    string TimeZoneId,
    DateOnly ModificationDate,
    IReadOnlyList<string> Columns);
