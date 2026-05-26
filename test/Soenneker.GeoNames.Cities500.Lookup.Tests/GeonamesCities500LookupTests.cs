using Soenneker.GeoNames.Cities500.Lookup.Abstract;
using Soenneker.Tests.HostedUnit;

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
    public void Default()
    {

    }
}
