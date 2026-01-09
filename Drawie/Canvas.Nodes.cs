using Avalonia;
using Avalonia.Media;

namespace Drawie;

public partial class Canvas
{
    private readonly Dictionary<string,INode> _nodes = [];
    // Setup Selection dragging
    // Setup Selection dragging

    public INode[] Nodes => _nodes.Values.ToArray();
    
    private bool AddN(INode node,bool replace = false)
    {
        var contain = _nodes.ContainsKey(node.Id);
        if (contain && !replace)
        {
            return false;
        }

        if (contain)
        {
            _nodes[node.Id] = node;
            return true;
        }
        
        _nodes.Add(node.Id, node);
        
        return true;
    }
    
    public void AddNode(INode node, bool replace = false)
    {
        if (node is Node nd)
        {
            nd.Canvas = this;
        }
        
        
        if (!AddN(node, replace: replace)){return;} 
        Refrech();
    }

    public void AddNode(INode[] nodes,bool replace =false)
    {
        Refrech();
    }

    public void UpdateNode(INode node,bool create=false)
    {
        Refrech();
    }

    public void RemoveNode(string id)
    {
        Refrech();
    }
    
    public INode? GetNodeAt(Point point)
    {
        var worldPoint = ScreenToWorld(point);

        foreach (var node in _nodes)
        {
            if (node.Value.Contains(worldPoint))
            {
                return node.Value;
            }
           
        }
        
        return null;
    }

    private void DrawNodes(DrawingContext context)
    {
        foreach (var node in _nodes)
        {
            if (IsNodeOffBound(node.Value))
            {
                continue;
            }
            node.Value.Render(context);
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
        var isOffRight = nd.Bounds.TopLeft.X > Bounds.Width / Zoom - PanOffset.X;
        var isOffBot = nd.Bounds.TopLeft.Y > Bounds.Height / Zoom - PanOffset.Y;

        var isOffTop = nd.Bounds.BottomRight.Y < -PanOffset.Y;
        var isOffLeft = nd.Bounds.BottomRight.X < -PanOffset.X;

        return isOffBot || isOffRight || isOffLeft || isOffTop;
    }
    
    public INode? GetNode(string? id)
    {
        if (id is null)
        {
            return null;
        }

        return _nodes[id];
    }
    public INode? GetHittingNode(Point p)
    {
        foreach (var node in _nodes)
        {
            if (node.Value.Contains(p))
            {
                return node.Value;
            }
           
        }

        return null;
    }
}
