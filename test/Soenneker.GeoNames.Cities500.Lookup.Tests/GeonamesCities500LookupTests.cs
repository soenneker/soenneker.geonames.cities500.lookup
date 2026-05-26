using Soenneker.GeoNames.Cities500.Lookup.Abstract;
using Soenneker.Tests.HostedUnit;
using System.Linq;
using System.Threading.Tasks;

namespace Soenneker.GeoNames.Cities500.Lookup.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class GeonamesCities500LookupTests : HostedUnitTest
{
    private readonly IGeonamesCities500Lookup _datasuiteutil;

    public GeonamesCities500LookupTests(Host host) : base(host)
    {
        _datasuiteutil = Resolve<IGeonamesCities500Lookup>(true);
    }

    [Test]
    public async Task Gets_records_from_packaged_data()
    {
        GeoNamesRecord? newYork = (await _datasuiteutil.GetByCityAndState("New York City", "NY")).FirstOrDefault();

        await Assert.That(newYork).IsNotNull();
        await Assert.That(newYork!.City).IsEqualTo("New York City");
        await Assert.That(newYork.State).IsEqualTo("NY");
        await Assert.That(newYork.Latitude).IsEqualTo(40.71427);
        await Assert.That(newYork.Longitude).IsEqualTo(-74.00597);
    }

    [Test]
    public async Task Gets_indexed_records()
    {
        GeoNamesRecord? byCity = (await _datasuiteutil.GetByCity("New York City")).FirstOrDefault();
        GeoNamesRecord? byState = (await _datasuiteutil.GetByState("NY")).FirstOrDefault(x => x.City == "New York City");
        GeoNamesRecord? byStateName = (await _datasuiteutil.GetByState("New York")).FirstOrDefault(x => x.City == "New York City");

        await Assert.That(byCity).IsNotNull();
        await Assert.That(byState).IsNotNull();
        await Assert.That(byStateName).IsNotNull();
    }

    [Test]
    public async Task Gets_coordinates_by_city_and_state()
    {
        GeoNamesCoordinates? coordinates = await _datasuiteutil.GetCoordinatesByCityAndState("Fort Lauderdale", "Florida");

        await Assert.That(coordinates).IsNotNull();
        await Assert.That(coordinates!.Value.Latitude).IsBetween(26.0, 27.0);
        await Assert.That(coordinates.Value.Longitude).IsBetween(-81.0, -80.0);
    }

    [Test]
    public async Task Normalizes_city_and_state_for_coordinate_lookup()
    {
        GeoNamesRecord? record = await _datasuiteutil.GetBestByCityAndState("Ft. Lauderdale", "FL");

        await Assert.That(record).IsNotNull();
        await Assert.That(record!.City).IsEqualTo("Fort Lauderdale");
        await Assert.That(record.State).IsEqualTo("FL");
    }
}
