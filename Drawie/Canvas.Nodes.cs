using Avalonia;
using Avalonia.Media;

namespace Drawie;

public partial class Canvas
{
    private readonly Dictionary<string,INode> _nodes = [];
    private readonly Dictionary<string, NodeLink> _nodeLinks = [];

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

    public void AddNode(IEnumerable<INode> nodes,bool replace =false, bool clear=false)
    {
        if (clear)
        {
            _nodes.Clear();
            foreach (var node in nodes)
            {
                if (node is Node nd)
                {
                    nd.Canvas = this;
                }
                
                _nodes.Add(node.Id, node);
            }
            return;
        }
        
        foreach (var node in nodes)
        {
            if(_nodes.ContainsKey(node.Id) && ! replace){continue;}
            _nodes[node.Id] = node;
        }
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

    public void RemoveNode(Predicate<INode> predicate)
    {
        foreach (var node in _nodes.Values)
        {
            if (predicate(node))
            {
                _nodeLinks.Remove(node.Id);
            }
        }
        
        Refrech();
    }

    public void AddNodeLink(string fromId, string toId, string? id = null)
    {
        _nodes.TryGetValue(fromId, out INode? fromNode); 
        _nodes.TryGetValue(toId, out INode? toNode);

        if (fromNode is null || toNode is null)
        {
            Console.WriteLine(":>");
            return;
        }

        var link = new NodeLink(this,src:fromId,des:toId, id:id);
        _nodeLinks.Add(link.Id, link);
        
        Refrech();
    }

    public void RemoveNodeLink(string id)
    {
        _nodeLinks.Remove(id); 
        Refrech();
    }

    public void RemoveNodeLink(Predicate<NodeLink> predicate)
    {
        foreach (var link in _nodeLinks.Values)
        {
            if (predicate(link))
            {
                _nodeLinks.Remove(link.Id);
            }
        }
        
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
        foreach (var (key,nl) in _nodeLinks)
        {
            if(nl.Destination is null || nl.Source is null  ){continue;}
            
            nl.Render(context); 
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