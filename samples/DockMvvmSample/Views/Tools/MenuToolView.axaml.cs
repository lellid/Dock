using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DockMvvmSample.Views.Tools;

public partial class MenuToolView : UserControl
{
    public MenuToolView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
