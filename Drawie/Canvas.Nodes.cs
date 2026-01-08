using Avalonia;
using Avalonia.Media;

namespace Drawie;

public partial class Canvas
{
    public INode? GetNodeAt(Point point)
    {
        var worldPoint = ScreenToWorld(point);

        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            INode node = _nodes[i];
            if (node.Contains(worldPoint))
            {
                return node;
            }
        }
        return null;
    }

    private void DrawNodes(DrawingContext context)
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            var nd = _nodes[i];
            if (IsNodeOffBound(nd))
            {
                continue;
            }

            nd.Render(context);
        }
    }

    private void DrawNodeLinks(DrawingContext context)
    {
        for (int i = 0; i < _nodeLinks.Count; i++)
        {
            var nd = _nodeLinks[i];

            if (nd is not NodeLink nl || nl.Source is null || nl.Destination is null)
            {
                continue;
            }

            if (IsNodeOffBound(nl.Source) && IsNodeOffBound(nl.Destination))
            {
                continue;
            }

            nd.Render(context);
        }
    }

    private bool IsNodeOffBound(INode nd)
    {
        //Console.WriteLine(nd.Bounds);
        var isOffRight = nd.Bounds.TopLeft.X > Bounds.Width / Zoom - PanOffset.X;
        var isOffBot = nd.Bounds.TopLeft.Y > Bounds.Height / Zoom - PanOffset.Y;

        var isOffTop = nd.Bounds.BottomRight.Y < -PanOffset.Y;
        var isOffLeft = nd.Bounds.BottomRight.X < -PanOffset.X;

        return isOffBot || isOffRight || isOffLeft || isOffTop;
    }
}
