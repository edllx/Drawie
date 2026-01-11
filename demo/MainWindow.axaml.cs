using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Drawie;
using Canvas = Avalonia.Controls.Canvas;

namespace demo;

public partial class MainWindow : Window
{
    List<INode> nodes = [];

    public MainWindow()
    {
        InitializeComponent();
        
        var canvas =CanvasView;
        if (canvas is null)
        {
            return;
        }


        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                nodes.Add(new TaskNode(new(32+i*(224 +8) ,32+j*(128+8)))
                {
                    Desctiption = "Simple description",
                    Title = "Task 1",
                    Id = $"ID_I{i}_J{j}"
                    ,Draggable = i%2==0,
                });
            }
        }
        
        canvas.AddNode(nodes,clear:true);
        
        canvas.AddNodeLink("ID_I1_J1","ID_I2_J3");
        
        Task.Run(async () =>
        {
            await Task.Delay(5000);
            canvas.RemoveNode("ID_I1_J1");
            
            await Task.Delay(1000);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                canvas.AddNodeLink("ID_I0_J1","ID_I2_J3");
            });
        });
    }

}

public class AsyncTimer
{
    private CancellationTokenSource? _cts;
    private int _delay = 5000;
    private Func<Task> _task;

    public AsyncTimer(int delay, Func<Task> task)
    {
        _delay = delay;
        _task = task;
    }
    
    private async Task Start(
        Func<Task> task, 
        int intervalMilliseconds, 
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _task(); 
            await Task.Delay(_delay, cancellationToken);
        }
    }
    
    public async Task Run()
    {
        Stop();
        _cts = new CancellationTokenSource();
        
        await Start(
            async () => 
            {
                await _task();
            },
            intervalMilliseconds: 5000,
            _cts.Token
        );
    }
    
    
    
    public void Stop()
    {
        _cts?.Cancel();
    }
}
