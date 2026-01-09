using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Media;

namespace Drawie;


public class Container : Node
{
    public ObservableCollection<INode> Children { get; init; } = [];

    public Container()
    {
        Id = IdGenerator.GenerateRandomString(16, "CT-");
        Border = new(0);
        Dirty = true;
        Children.CollectionChanged += HandleCollectionChanged;
    }

    private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Canvas?.InvalidateVisual();
    }

    public Container(string id)
        : base(id)
    {
        Border = new(0);
    }

    public override void Render(DrawingContext ctx)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var p1 = Origin;
        var p2 = new Point(p1.X + w, p1.Y);
        var p3 = new Point(p1.X + w, p1.Y + h);
        var p4 = new Point(p1.X, p1.Y + h);

        // Border
        ctx.FillRectangle(
            Selected ? DefaultSelectionBrush : Brushes.Gray,
            new(p1.X, p1.Y, w, h),
            cornerRadius: (float)BoderRadius
        );

        var pb1 = p1 + new Point(Border.Left, Border.Top);
        var pb3 = p3 + new Point(-Border.Right, -Border.Bottom);

        // Body
        ctx.FillRectangle(Background, new(pb1, pb3), cornerRadius: (float)BoderRadius);

        //Render Children
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].Render(ctx);
        }
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        switch (propertyName)
        {
            case nameof(Origin):

                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i].Origin = Origin;
                }
                break;
        }
    }

    public override void Dispose()
    {
        Children.CollectionChanged -= HandleCollectionChanged;
    }
}