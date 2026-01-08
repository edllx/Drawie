namespace Drawie;

public partial class Canvas
{
    private RequestManager RenderManager = new(16);
    private RequestManager MouseManager = new(64);

    public void Refrech()
    {
        _ = RenderManager.Execute(
            new(async () =>
            {
                InvalidateVisual();
            })
        );
    }
}
