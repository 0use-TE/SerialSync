using CommunityToolkit.Mvvm.ComponentModel;
using GOZA.Dock;
using SerialSync.Models;

namespace SerialSync.Dock;

public abstract class DockTabViewModel : ObservableObject, IDockTabItem
{
    public abstract string Id { get; }
    public abstract string Header { get; }
    public virtual bool ReuseSurface => false;
    public virtual DockSlot DefaultSlot => DockSlot.Left;
}
