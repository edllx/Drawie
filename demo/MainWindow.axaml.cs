using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeC();
    }

    public void InitializeC(bool loadXaml = true, bool attachDevTools = true)
    {
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }

        Canvas canvas = new Canvas();

#if DEBUG
        if (attachDevTools)
        {
            this.AttachDevTools();
        }
#endif
    }
}

