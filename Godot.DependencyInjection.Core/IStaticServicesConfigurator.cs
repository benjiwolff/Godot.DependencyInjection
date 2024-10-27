using Microsoft.Extensions.DependencyInjection;

namespace Godot.DependencyInjection;

public interface IStaticServicesConfigurator
{
    /// <summary>
    /// Configures the services to be used in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    static abstract void ConfigureServices(IServiceCollection services);
}