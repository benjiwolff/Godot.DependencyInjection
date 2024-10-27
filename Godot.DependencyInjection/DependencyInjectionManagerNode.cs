using Godot.DependencyInjection.Injection;
using Microsoft.Extensions.DependencyInjection;

namespace Godot.DependencyInjection;

public abstract partial class DependencyInjectionManagerNode : Node
{
    public static DependencyInjectionManagerNode Instance => _instance ??
                                                             throw new InvalidOperationException(
                                                                 $"{nameof(DependencyInjectionManagerNode)} is not initialized.");

    private static DependencyInjectionManagerNode? _instance;

    public DependencyInjectionManagerNode()
    {
        if (_instance is not null)
        {
            throw new InvalidCastException(
                $"Failed to create {nameof(DependencyInjectionManagerNode)}. Only one instance is allowed.");
        }

        _instance = this;
        var configs = GetType().Assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(IStaticServicesConfigurator)));
        var serviceCollection = new ServiceCollection();
        foreach (var c in configs)
        {
            c.GetMethod(nameof(IStaticServicesConfigurator.ConfigureServices))!.Invoke(null, [serviceCollection]);
        }

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private InjectionService _injectionService = null!;

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// Called when the node is ready.
    /// </summary>
    public override void _EnterTree()
    {
        var tree = GetTree();
        (_injectionService, var nodesToInject) =
            InjectionServiceFactory.Create(new NodeWrapper(tree.Root));

        var unpackedNodes = nodesToInject.Select(x => ((NodeWrapper)x).Node);
        foreach (var node in unpackedNodes)
        {
            _injectionService.InjectDependencies(node);
        }

        tree.NodeAdded += _injectionService.InjectDependencies;
    }
}