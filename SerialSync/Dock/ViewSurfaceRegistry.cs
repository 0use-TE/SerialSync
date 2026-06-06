using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace SerialSync.Dock;

public interface IViewSurfaceRegistry
{
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    Type GetViewType(Type viewModelType);

    Control CreateView(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type viewType,
        DockTabViewModel viewModel);
}

internal sealed class ViewSurfaceRegistry : IViewSurfaceRegistry
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<Type, Type> _map;

    public ViewSurfaceRegistry(IServiceProvider services, IEnumerable<IViewSurfacePairRegistration> registrations)
    {
        _services = services;
        _map = new Dictionary<Type, Type>();
        foreach (var registration in registrations)
            registration.Register(_map);
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type GetViewType(Type viewModelType)
    {
        if (_map.TryGetValue(viewModelType, out var viewType))
            return viewType;

        throw new InvalidOperationException($"未注册 View: {viewModelType.FullName}");
    }

    public Control CreateView(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type viewType,
        DockTabViewModel viewModel)
    {
        var view = (Control)ActivatorUtilities.CreateInstance(_services, viewType);
        view.DataContext = viewModel;
        return view;
    }
}

internal interface IViewSurfacePairRegistration
{
    void Register(Dictionary<Type, Type> map);
}

internal sealed class ViewSurfacePairRegistration<TViewModel, TView> : IViewSurfacePairRegistration
    where TViewModel : DockTabViewModel
    where TView : Control
{
    public void Register(Dictionary<Type, Type> map) => map[typeof(TViewModel)] = typeof(TView);
}
