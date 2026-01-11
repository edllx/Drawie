using Avalonia.Threading;

namespace Drawie;

public partial class Canvas
{
    private RequestManager RenderManager = new(16);
    private RequestManager MouseManager = new(64);

    
    public void Refresh()
    {
        _ = RenderManager.Execute(
            new(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
            })
        );
    }
}
