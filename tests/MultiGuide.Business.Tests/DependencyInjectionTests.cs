using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.Common;
using MultiGuide.Business.Services;

namespace MultiGuide.Business.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddBusinessServices_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBusinessServices();

        // Assert
        AssertContainsService(services, typeof(IClock), typeof(SystemClock), ServiceLifetime.Singleton);
        AssertContainsService(services, typeof(IAdminContentService), typeof(AdminContentService), ServiceLifetime.Scoped);
        AssertContainsService(services, typeof(IAreaService), typeof(AreaService), ServiceLifetime.Scoped);
        AssertContainsService(services, typeof(IPassService), typeof(PassService), ServiceLifetime.Scoped);
        AssertContainsService(services, typeof(IGeoService), typeof(GeoService), ServiceLifetime.Scoped);
        AssertContainsService(services, typeof(IPoiService), typeof(PoiService), ServiceLifetime.Scoped);
        AssertContainsService(services, typeof(IContentService), typeof(ContentService), ServiceLifetime.Scoped);
        AssertContainsService(services, typeof(IPlayLogService), typeof(PlayLogService), ServiceLifetime.Scoped);
    }

    private static void AssertContainsService(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceType);
        Assert.NotNull(descriptor);
        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(lifetime, descriptor.Lifetime);
    }
}
