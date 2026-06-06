using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace SerialSync.Dock;

public sealed class DockTabDataTemplate : IDataTemplate
{
    private readonly IViewSurfaceRegistry _registry;

    public DockTabDataTemplate(IViewSurfaceRegistry registry) => _registry = registry;

    public bool Match(object? data) => data is DockTabViewModel;

    public Control? Build(object? param)
    {
        if (param is not DockTabViewModel viewModel)
            return null;

        var viewType = _registry.GetViewType(viewModel.GetType());
        return _registry.CreateView(viewType, viewModel);
    }
}
