using Avalonia;
using Avalonia.Input;

namespace Drawie;

public partial class CanvasPointerEventHandler
{
    private Canvas _canvas;

    public CanvasPointerEventHandler(Canvas c)
    {
        _canvas = c;
    }

    public CanvasPointerEventHandler HandleLeftPressed(PointerPressedEventArgs e)
    {
        if (e.Handled || !e.Properties.IsLeftButtonPressed)
        {
            return this;
        }

        var worldPoint = _canvas.ScreenToWorld(_canvas.LastMousePressedPosition);
        // Clicking selection area
        // Setup Selection dragging
        if (_canvas.Selection.Contains(worldPoint))
        {
            _canvas.Selection.Dragging = true;
            var tl = _canvas.Selection.TopLeft;
            _canvas.Selection.DragOrigin = _canvas.Selection.TopLeft;
            _canvas.Selection.ClickOffset = worldPoint - tl;
            _canvas.Selection.NotityBoundChanged();
            return this;
        }

        _canvas.ClearSelection();
        e.Handled = true;
        return this;
    }

    public CanvasPointerEventHandler HandleShiftLeftPressedMove(PointerEventArgs e)
    {
        if (
            e.Handled
            || !e.Properties.IsLeftButtonPressed
            || !e.KeyModifiers.HasFlag(KeyModifiers.Shift)
        )
        {
            return this;
        }

        Point position = e.GetPosition(_canvas);
        e.Handled = true;
        return this;
    }

    public CanvasPointerEventHandler HandleMiddlePressedMove(PointerEventArgs e)
    {
        if (e.Handled || !e.Properties.IsMiddleButtonPressed)
        {
            return this;
        }

        e.Handled = true;

        Point position = e.GetPosition(_canvas);

        var delta = (position - _canvas.LastMousePressedPosition) / _canvas.Zoom;
        _canvas.SetPanOffset(_canvas.PanOffset + delta);
        _canvas.SetLastPressed(position);

        return this;
    }

    public CanvasPointerEventHandler HandleLeftPressedMove(PointerEventArgs e)
    {
        if (e.Handled || !e.Properties.IsLeftButtonPressed)
        {
            return this;
        }
        e.Handled = true;

        Point position = e.GetPosition(_canvas);
        var delta = (position - _canvas.LastMousePressedPosition) / _canvas.Zoom;

        // Drag the selection
        if (_canvas.Selection.Dragging)
        {
            _canvas.Selection.Drag(delta);
            return this;
        }

        _canvas.ClearSelection();
        double distance = Point.Distance(_canvas.LastMousePressedPosition, position);

        if (distance < 16)
        {
            return this;
        }

        // Set the seleciton end on cell further
        var br = Canvas.GetOrigin(_canvas.ScreenToWorld(position)) + new Point(1, 1);

        _canvas.Selection.BotRight = br;

        foreach (Node nd in _canvas.Nodes)
        {
            if (
                _canvas.Selection.Contains(nd.Bounds.BottomRight)
                && _canvas.Selection.Contains(nd.Bounds.TopLeft)
            )
            {
                _canvas.Selection.AddNode(nd, adjustBounds: false);
            }
        }
        _canvas.Selection.NotityBoundChanged();

        e.Handled = true;
        return this;
    }

    public CanvasPointerEventHandler HandleLeftRelease(PointerReleasedEventArgs e)
    {
        if (e.Handled)
        {
            return this;
        }
        e.Handled = true;

        _canvas.Selection.Dragging = false;

        return this;
    }

    private CanvasPointerEventHandler HandleShiftRightClick()
    {
        return this;
    }

    public CanvasPointerEventHandler HandleShiftLeftPressed(PointerPressedEventArgs e)
    {
        if (
            e.Handled
            || !e.Properties.IsLeftButtonPressed
            || !e.KeyModifiers.HasFlag(KeyModifiers.Shift)
        )
        {
            return this;
        }

        Point position = e.GetPosition(_canvas);
        var nd = _canvas.GetNodeAt(position);
        if (_canvas.Selection.Nodes.Count == 0 && nd is not null)
        {
            _canvas.Selection.TopLeft = nd.Origin;
            _canvas.Selection.BotRight = _canvas.Selection.TopLeft;
        }
        _canvas.Selection.AddNode(nd);

        e.Handled = true;
        return this;
    }

    public CanvasPointerEventHandler Action(Action action)
    {
        action.Invoke();
        return this;
    }
}
