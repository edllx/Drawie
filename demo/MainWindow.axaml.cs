using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Drawie;
using Canvas = Avalonia.Controls.Canvas;

namespace demo;

public partial class MainWindow : Window
{
    List<INode> nodes = [];

    private Timer? _timer;
    
    public void StartTimer(int intervalMilliseconds)
    {
        // Create a timer that calls the method every X milliseconds
        _timer = new Timer(TimerCallback, null, 0, intervalMilliseconds);
    }
    
    private void TimerCallback(object? state)
    {
        // Your task logic here
        Console.WriteLine($"Task executed at {DateTime.Now}");
        
    }
    
    private void ProcessData()
    {
        // Your task logic
    }
    
    public void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }
    
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
                });
            }
        }
        
        canvas.AddNode(nodes,clear:true);
        
        canvas.AddNodeLink("ID_I1_J1","ID_I2_J3");
        

        /*
        AsyncTimer timer = new AsyncTimer(1000, async () =>
        {
           var random = Random.Shared.Next(0,nodes.Count); 
           var node = nodes[random];

           if (node is TaskNode taskNode)
           {
               taskNode.Desctiption = $"Random description : {Random.Shared.Next(0,1000)}";
           }
        });

        _ = timer.Run();

        Task.Run(async () =>
        {
            await Task.Delay(60000);
            timer.Stop();
        });
        */
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
