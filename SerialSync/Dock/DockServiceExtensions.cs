using Avalonia.Controls;
using Crystal.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace SerialSync.Dock;

public static class DockServiceExtensions
{
    public static IServiceCollection AddDockInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<ViewSurfaceRegistry>();
        services.TryAddSingleton<IViewSurfaceRegistry>(sp => sp.GetRequiredService<ViewSurfaceRegistry>());
        services.AddSingleton<DockTabDataTemplate>();
        return services;
    }

    public static void AddDockPanel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(
        this IServiceCollection services)
        where TView : Control
        where TViewModel : DockTabViewModel
    {
        services.AddMvvmSingleton<TView, TViewModel>();
        services.AddSingleton<IViewSurfacePairRegistration, ViewSurfacePairRegistration<TViewModel, TView>>();
    }
}
