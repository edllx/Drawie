using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Drawie;
using Canvas = Avalonia.Controls.Canvas;

namespace demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        //InitializeC();
        InitializeComponent();
        
        var canvas =CanvasView;
        if (canvas is null)
        {
            return;
        }
        
        canvas.AddNode(new TaskNode(new(160,160))
        {
            Desctiption = "Simple description",
            Title = "Task 1",
        });
       ;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        //CanvasView.AddNode(new TaskNode(new (124,124))); 
        base.OnApplyTemplate(e);
        
    }

    public void InitializeC(bool loadXaml = true, bool attachDevTools = true)
    {
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }


#if DEBUG
        if (attachDevTools)
        {
            this.AttachDevTools();
        }
#endif
    }
}

