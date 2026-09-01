using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
using Soenneker.Utils.PooledStringBuilders;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Asyncs.Initializers;
using Soenneker.Extensions.ValueTask;
using Soenneker.GeoNames.Cities500.Lookup.Abstract;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.Paths.Resources.Abstract;

namespace Soenneker.GeoNames.Cities500.Lookup;

public sealed class GeonamesCities500Lookup : IGeonamesCities500Lookup
{
    private const string _fileName = "cities500.txt";
    private static readonly IReadOnlyList<GeoNamesRecord> _emptyList = [];
    private static readonly FrozenDictionary<string, string> _stateNames = BuildStateNames();
    private static readonly FrozenDictionary<string, string> _cityTokenAliases = BuildCityTokenAliases();

    private readonly IFileUtil _fileUtil;
    private readonly IResourcesPathUtil _resourcesPathUtil;
    private readonly AsyncInitializer _initializer;
    private GeoNamesIndex? _index;

    public GeonamesCities500Lookup(IFileUtil fileUtil, IResourcesPathUtil resourcesPathUtil)
    {
        _fileUtil = fileUtil;
        _resourcesPathUtil = resourcesPathUtil;
        _initializer = new AsyncInitializer(Initialize);
    }

    public async ValueTask<IReadOnlyCollection<GeoNamesRecord>> GetAll(CancellationToken cancellationToken = default)
    {
        GeoNamesIndex index = await GetIndex(cancellationToken).NoSync();
        return index.All;
    }

    public async ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCity(string city,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
            return _emptyList;

        string normalizedCity = NormalizeCity(city);

        if (normalizedCity.Length == 0)
            return _emptyList;

        GeoNamesIndex index = await GetIndex(cancellationToken).NoSync();
        return index.ByNormalizedCity.GetValueOrDefault(normalizedCity, _emptyList);
    }

    public async ValueTask<IReadOnlyList<GeoNamesRecord>> GetByState(string state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(state))
            return _emptyList;

        if (!TryNormalizeState(state, out string stateCode))
            return _emptyList;

        GeoNamesIndex index = await GetIndex(cancellationToken).NoSync();
        return index.ByState.GetValueOrDefault(stateCode, _emptyList);
    }

    public async ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCityAndState(string city, string state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
            return _emptyList;

        if (!TryNormalizeState(state, out string stateCode))
            return _emptyList;

        string normalizedCity = NormalizeCity(city);

        if (normalizedCity.Length == 0)
            return _emptyList;

        GeoNamesIndex index = await GetIndex(cancellationToken).NoSync();
        return index.ByStateNormalizedCity.GetValueOrDefault(BuildStateCityKey(stateCode, normalizedCity), _emptyList);
    }

    public async ValueTask<GeoNamesRecord?> GetBestByCityAndState(string city, string state,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GeoNamesRecord> records = await GetByCityAndState(city, state, cancellationToken).NoSync();
        return records.Count == 0 ? null : records[0];
    }

    public async ValueTask<GeoNamesCoordinates?> GetCoordinatesByCityAndState(string city, string state,
        CancellationToken cancellationToken = default)
    {
        GeoNamesRecord? record = await GetBestByCityAndState(city, state, cancellationToken).NoSync();

        if (record == null)
            return null;

        return new GeoNamesCoordinates(record.Latitude, record.Longitude);
    }

    private ValueTask<GeoNamesIndex> GetIndex(CancellationToken cancellationToken)
    {
        if (_index != null)
            return new ValueTask<GeoNamesIndex>(_index);

        return GetIndexSlow(cancellationToken);
    }

    private async ValueTask<GeoNamesIndex> GetIndexSlow(CancellationToken cancellationToken)
    {
        await _initializer.Init(cancellationToken).NoSync();
        return _index!;
    }

    private async ValueTask Initialize(CancellationToken cancellationToken)
    {
        _index = await LoadIndex(cancellationToken).NoSync();
    }

    private async ValueTask<GeoNamesIndex> LoadIndex(CancellationToken cancellationToken)
    {
        string filePath = await GetDataFilePath(cancellationToken).NoSync();

        var all = new List<GeoNamesRecord>();
        var byNormalizedCity = new Dictionary<string, List<GeoNamesRecord>>(StringComparer.OrdinalIgnoreCase);
        var byState = new Dictionary<string, List<GeoNamesRecord>>(StringComparer.OrdinalIgnoreCase);
        var byStateNormalizedCity = new Dictionary<string, List<GeoNamesRecord>>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 0;

        await using FileStream fileStream = _fileUtil.OpenRead(filePath, log: false);
        using var reader = new StreamReader(fileStream);
        Span<Range> columns = stackalloc Range[4];

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            ReadOnlySpan<char> lineSpan = line;
            int columnCount = lineSpan.Split(columns, '\t');

            if (columnCount != 4)
                throw new InvalidDataException(
                    $"Unexpected GeoNames format at line {lineNumber}. Expected 4 tab-delimited columns: city, state, latitude, longitude.");

            string city = lineSpan[columns[0]].Trim().ToString();
            string state = lineSpan[columns[1]].Trim().ToString();

            if (city.Length == 0 || !TryNormalizeState(state, out string stateCode))
                continue;

            if (!double.TryParse(lineSpan[columns[2]], NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude) ||
                !double.IsFinite(latitude) || latitude is < -90 or > 90 ||
                !double.TryParse(lineSpan[columns[3]], NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude) ||
                !double.IsFinite(longitude) || longitude is < -180 or > 180)
            {
                throw new InvalidDataException($"Invalid coordinates in GeoNames data at line {lineNumber}.");
            }

            var record = new GeoNamesRecord(city, stateCode, latitude, longitude);

            all.Add(record);

            string normalizedCity = NormalizeCity(record.City);
            Add(byNormalizedCity, normalizedCity, record);
            Add(byState, record.State, record);
            Add(byStateNormalizedCity, BuildStateCityKey(record.State, normalizedCity), record);
        }

        return new GeoNamesIndex(all.ToImmutableArray(), Freeze(byNormalizedCity), Freeze(byState),
            Freeze(byStateNormalizedCity));
    }

    private async ValueTask<string> GetDataFilePath(CancellationToken cancellationToken)
    {
        string filePath = await _resourcesPathUtil.GetResourceFilePath(_fileName, cancellationToken).NoSync();

        if (await _fileUtil.Exists(filePath, cancellationToken).NoSync())
            return filePath;

        throw new FileNotFoundException(
            $"Could not locate {filePath}. Ensure the {ConstantsDataPackage} content file is copied to the output directory.",
            filePath);
    }

    private const string ConstantsDataPackage = "Soenneker.GeoNames.Cities500.Data";

    private static void Add(Dictionary<string, List<GeoNamesRecord>> dictionary, string key, GeoNamesRecord record)
    {
        if (!dictionary.TryGetValue(key, out List<GeoNamesRecord>? entries))
        {
            entries = [];
            dictionary[key] = entries;
        }

        entries.Add(record);
    }

    private static FrozenDictionary<string, IReadOnlyList<GeoNamesRecord>> Freeze(
        Dictionary<string, List<GeoNamesRecord>> source)
    {
        var result = new Dictionary<string, IReadOnlyList<GeoNamesRecord>>(source.Count, source.Comparer);

        foreach (KeyValuePair<string, List<GeoNamesRecord>> pair in source)
        {
            result[pair.Key] = pair.Value.ToImmutableArray();
        }

        return result.ToFrozenDictionary(source.Comparer);
    }

    private static string BuildStateCityKey(string stateCode, string normalizedCity)
    {
        return $"{stateCode}\u001F{normalizedCity}";
    }

    private static bool TryNormalizeState(string state, out string stateCode)
    {
        stateCode = "";
        string normalized = NormalizeText(state);

        if (normalized.Length == 0)
            return false;

        if (_stateNames.TryGetValue(normalized, out string? normalizedStateCode))
        {
            stateCode = normalizedStateCode;
            return true;
        }

        return false;
    }

    private static string NormalizeCity(string city)
    {
        string normalized = NormalizeText(city);

        if (normalized.Length == 0)
            return normalized;

        FrozenDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> aliases =
            _cityTokenAliases.GetAlternateLookup<ReadOnlySpan<char>>();
        StringBuilder? builder = null;
        ReadOnlySpan<char> span = normalized;
        var start = 0;

        for (var i = 0; i <= span.Length; i++)
        {
            if (i != span.Length && span[i] != ' ')
                continue;

            ReadOnlySpan<char> token = span[start..i];
            if (aliases.TryGetValue(token, out string? replacement))
            {
                builder ??= new StringBuilder(normalized.Length + 8).Append(span[..start]);
                builder.Append(replacement);
            }
            else if (builder is not null)
            {
                builder.Append(token);
            }

            if (builder is not null && i < span.Length)
                builder.Append(' ');

            start = i + 1;
        }

        return builder?.ToString() ?? normalized;
    }

    private static string NormalizeText(string value)
    {
        value = value.Trim().Normalize(NormalizationForm.FormD);
        using var builder = new PooledStringBuilder(value.Length);
        var previousWasSpace = true;

        foreach (char character in value)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSpace = false;
                continue;
            }

            if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        if (builder.Length > 0 && builder.AsSpan()[^1] == ' ')
            builder.Shrink(1);

        return builder.ToString();
    }

    private static FrozenDictionary<string, string> BuildCityTokenAliases()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ft"] = "fort",
            ["ftd"] = "fort",
            ["st"] = "saint",
            ["ste"] = "sainte",
            ["mt"] = "mount",
            ["n"] = "north",
            ["s"] = "south",
            ["e"] = "east",
            ["w"] = "west"
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static FrozenDictionary<string, string> BuildStateNames()
    {
        var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["al"] = "AL", ["alabama"] = "AL",
            ["ak"] = "AK", ["alaska"] = "AK",
            ["az"] = "AZ", ["arizona"] = "AZ",
            ["ar"] = "AR", ["arkansas"] = "AR",
            ["ca"] = "CA", ["california"] = "CA",
            ["co"] = "CO", ["colorado"] = "CO",
            ["ct"] = "CT", ["connecticut"] = "CT",
            ["de"] = "DE", ["delaware"] = "DE",
            ["dc"] = "DC", ["district of columbia"] = "DC",
            ["fl"] = "FL", ["florida"] = "FL",
            ["ga"] = "GA", ["georgia"] = "GA",
            ["hi"] = "HI", ["hawaii"] = "HI",
            ["id"] = "ID", ["idaho"] = "ID",
            ["il"] = "IL", ["illinois"] = "IL",
            ["in"] = "IN", ["indiana"] = "IN",
            ["ia"] = "IA", ["iowa"] = "IA",
            ["ks"] = "KS", ["kansas"] = "KS",
            ["ky"] = "KY", ["kentucky"] = "KY",
            ["la"] = "LA", ["louisiana"] = "LA",
            ["me"] = "ME", ["maine"] = "ME",
            ["md"] = "MD", ["maryland"] = "MD",
            ["ma"] = "MA", ["massachusetts"] = "MA",
            ["mi"] = "MI", ["michigan"] = "MI",
            ["mn"] = "MN", ["minnesota"] = "MN",
            ["ms"] = "MS", ["mississippi"] = "MS",
            ["mo"] = "MO", ["missouri"] = "MO",
            ["mt"] = "MT", ["montana"] = "MT",
            ["ne"] = "NE", ["nebraska"] = "NE",
            ["nv"] = "NV", ["nevada"] = "NV",
            ["nh"] = "NH", ["new hampshire"] = "NH",
            ["nj"] = "NJ", ["new jersey"] = "NJ",
            ["nm"] = "NM", ["new mexico"] = "NM",
            ["ny"] = "NY", ["new york"] = "NY",
            ["nc"] = "NC", ["north carolina"] = "NC",
            ["nd"] = "ND", ["north dakota"] = "ND",
            ["oh"] = "OH", ["ohio"] = "OH",
            ["ok"] = "OK", ["oklahoma"] = "OK",
            ["or"] = "OR", ["oregon"] = "OR",
            ["pa"] = "PA", ["pennsylvania"] = "PA",
            ["ri"] = "RI", ["rhode island"] = "RI",
            ["sc"] = "SC", ["south carolina"] = "SC",
            ["sd"] = "SD", ["south dakota"] = "SD",
            ["tn"] = "TN", ["tennessee"] = "TN",
            ["tx"] = "TX", ["texas"] = "TX",
            ["ut"] = "UT", ["utah"] = "UT",
            ["vt"] = "VT", ["vermont"] = "VT",
            ["va"] = "VA", ["virginia"] = "VA",
            ["wa"] = "WA", ["washington"] = "WA",
            ["wv"] = "WV", ["west virginia"] = "WV",
            ["wi"] = "WI", ["wisconsin"] = "WI",
            ["wy"] = "WY", ["wyoming"] = "WY"
        };

        return states.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record GeoNamesIndex(
        IReadOnlyCollection<GeoNamesRecord> All,
        FrozenDictionary<string, IReadOnlyList<GeoNamesRecord>> ByNormalizedCity,
        FrozenDictionary<string, IReadOnlyList<GeoNamesRecord>> ByState,
        FrozenDictionary<string, IReadOnlyList<GeoNamesRecord>> ByStateNormalizedCity);
}
