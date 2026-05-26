using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Asyncs.Initializers;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.Paths.Resources.Abstract;
using Soenneker.GeoNames.Cities500.Lookup.Abstract;

namespace Soenneker.GeoNames.Cities500.Lookup;

/// <inheritdoc cref="IGeonamesCities500Lookup"/>
public sealed class GeonamesCities500Lookup : IGeonamesCities500Lookup
{
    private const string _fileName = "Cities500.txt";
    private static readonly IReadOnlyList<GeoNamesRecord> _emptyList = [];

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

    public async ValueTask<GeoNamesRecord?> Get(int id, CancellationToken cancellationToken = default)
    {
        GeoNamesIndex index = await GetIndex(cancellationToken).NoSync();
        index.ById.TryGetValue(id, out GeoNamesRecord? record);
        return record;
    }

    public async ValueTask<IReadOnlyList<GeoNamesRecord>> GetByName(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return _emptyList;

        GeoNamesIndex index = await GetIndex(cancellationToken).NoSync();
        return index.ByName.GetValueOrDefault(name.Trim(), _emptyList);
    }

    public async ValueTask<IReadOnlyList<GeoNamesRecord>> GetByCountryCode(string countryCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return _emptyList;

        GeoNamesIndex index = await GetIndex(cancellationToken).NoSync();
        return index.ByCountryCode.GetValueOrDefault(countryCode.Trim(), _emptyList);
    }

    public async ValueTask<GeoNamesCoordinates?> GetCoordinates(int id, CancellationToken cancellationToken = default)
    {
        GeoNamesIndex index = await GetIndex(cancellationToken).NoSync();
        return index.ByCoordinates.TryGetValue(id, out GeoNamesCoordinates coordinates) ? coordinates : null;
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
        var byId = new Dictionary<int, GeoNamesRecord>();
        var byCoordinates = new Dictionary<int, GeoNamesCoordinates>();
        var byName = new Dictionary<string, List<GeoNamesRecord>>(StringComparer.OrdinalIgnoreCase);
        var byCountryCode = new Dictionary<string, List<GeoNamesRecord>>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 0;

        await using FileStream fileStream = _fileUtil.OpenRead(filePath, log: false);
        using var reader = new StreamReader(fileStream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] columns = line.Split('\t');

            if (columns.Length < 19)
                throw new InvalidDataException($"Unexpected GeoNames format at line {lineNumber}. Expected at least 19 tab-delimited columns.");

            var record = new GeoNamesRecord(
                int.Parse(columns[0], CultureInfo.InvariantCulture),
                columns[1],
                columns[2],
                double.Parse(columns[4], CultureInfo.InvariantCulture),
                double.Parse(columns[5], CultureInfo.InvariantCulture),
                columns[6],
                columns[7],
                columns[8],
                columns[10],
                ParseLong(columns[14]),
                columns[17],
                DateOnly.Parse(columns[18], CultureInfo.InvariantCulture),
                columns);

            all.Add(record);
            byId[record.Id] = record;
            byCoordinates[record.Id] = new GeoNamesCoordinates(record.Latitude, record.Longitude);
            Add(byName, record.Name, record);
            Add(byCountryCode, record.CountryCode, record);
        }

        return new GeoNamesIndex(all.ToArray(), byId.ToFrozenDictionary(), byCoordinates.ToFrozenDictionary(), Freeze(byName), Freeze(byCountryCode));
    }

    private async ValueTask<string> GetDataFilePath(CancellationToken cancellationToken)
    {
        string filePath = await _resourcesPathUtil.GetResourceFilePath(_fileName, cancellationToken).NoSync();

        if (await _fileUtil.Exists(filePath, cancellationToken).NoSync())
            return filePath;

        throw new FileNotFoundException($"Could not locate {filePath}. Ensure the {ConstantsDataPackage} content file is copied to the output directory.", filePath);
    }

    private const string ConstantsDataPackage = "Soenneker.GeoNames.Cities500.Data";

    private static long ParseLong(string value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result) ? result : 0;
    }

    private static void Add(Dictionary<string, List<GeoNamesRecord>> dictionary, string key, GeoNamesRecord record)
    {
        if (!dictionary.TryGetValue(key, out List<GeoNamesRecord>? entries))
        {
            entries = [];
            dictionary[key] = entries;
        }

        entries.Add(record);
    }

    private static FrozenDictionary<string, IReadOnlyList<GeoNamesRecord>> Freeze(Dictionary<string, List<GeoNamesRecord>> source)
    {
        var result = new Dictionary<string, IReadOnlyList<GeoNamesRecord>>(source.Count, source.Comparer);

        foreach (KeyValuePair<string, List<GeoNamesRecord>> pair in source)
        {
            result[pair.Key] = pair.Value.ToArray();
        }

        return result.ToFrozenDictionary(source.Comparer);
    }

    private sealed record GeoNamesIndex(
        IReadOnlyCollection<GeoNamesRecord> All,
        FrozenDictionary<int, GeoNamesRecord> ById,
        FrozenDictionary<int, GeoNamesCoordinates> ByCoordinates,
        FrozenDictionary<string, IReadOnlyList<GeoNamesRecord>> ByName,
        FrozenDictionary<string, IReadOnlyList<GeoNamesRecord>> ByCountryCode);
}
