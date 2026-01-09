using Avalonia;
using Avalonia.Media;

namespace Drawie;

public enum GridType
{
    None,
    Square,
}

public partial class Canvas
{
    private static readonly IPen MinorGridPen = new Pen(
        new SolidColorBrush(Color.FromArgb(40, 11, 11, 11)),
        1
    );

    private static readonly IPen MajorGridPen = new Pen(
        new SolidColorBrush(Color.FromArgb(60, 11, 11, 11)),
        1.3
    );

    public static double GridSize { get; set; } = 8.0;
    public static double MajorGridEvery { get; set; } = 4;

    public static Point GetOrigin(Point position)
    {
        return new Point(
            Math.Floor(position.X / GridSize) * GridSize,
            Math.Floor(position.Y / GridSize) * GridSize
        );
    }

    private DrawingGroup GridDG = new();

    private void DrawGrid(DrawingContext ctx)
    {
        if (GridType == GridType.None)
        {
            return;
        }

        Point tl = new Point(0, 0);
        Point br = new Point(Bounds.Width, Bounds.Height);

        double startX = 0;
        double startY = 0;

        startX = ((int)(-PanOffset.X / Canvas.GridSize)) * Canvas.GridSize - Canvas.GridSize;
        startY = ((int)(-PanOffset.Y / Canvas.GridSize)) * Canvas.GridSize - Canvas.GridSize;

        for (double x = startX; x < Bounds.Width / Zoom - PanOffset.X; x += GridSize)
        {
            ctx.DrawLine(
                MinorGridPen,
                new Point(x, -PanOffset.Y),
                new Point(x, Bounds.Height / Zoom - PanOffset.Y)
            );
        }

        for (double y = startY; y < Bounds.Height / Zoom - PanOffset.Y; y += GridSize)
        {
            ctx.DrawLine(
                MinorGridPen,
                new Point(-PanOffset.X, y),
                new Point(Bounds.Width / Zoom - PanOffset.X, y)
            );
        }
    }

    private void UpdateGrid()
    {
        using var ctx = GridDG.Open();

        double startX = 0;
        double startY = 0;

        startX = ((int)(-PanOffset.X / Canvas.GridSize)) * Canvas.GridSize - Canvas.GridSize;
        startY = ((int)(-PanOffset.Y / Canvas.GridSize)) * Canvas.GridSize - Canvas.GridSize;

        var geometry = new StreamGeometry();

        using (var ct = geometry.Open())
        {
            double spacing = GridSize;

            for (double x = startX; x < Bounds.Width / Zoom - PanOffset.X; x += spacing)
            {
                for (double y = startY; y < Bounds.Height / Zoom - PanOffset.Y; y += spacing)
                {
                    // Draw each dot as a tiny rectangle (more efficient than ellipse)
                    ct.BeginFigure(new Point(x - 0.5, y - 0.5), true);
                    ct.LineTo(new Point(x + 0.5, y - 0.5));
                    ct.LineTo(new Point(x + 0.5, y + 0.5));
                    ct.LineTo(new Point(x - 0.5, y + 0.5));
                    ct.EndFigure(true);
                }
            }
        }

        ctx.DrawGeometry(Brushes.Black, null, geometry);
    }

    private void DrawDotedGrid(DrawingContext ctx)
    {
        GridDG.Draw(ctx);
    }

    private void DrawBackground(DrawingContext ctx)
    {
        Point tl = new Point(0, 0);
        Point br = new Point(Bounds.Width, Bounds.Height);

        double startX = tl.X;
        double endX = br.X;
        double startY = tl.Y;
        double endY = br.Y;

        ctx.FillRectangle(Brushes.Azure, new(tl.X, tl.Y, br.X, br.Y));
    }

    private DrawingGroup GetBackground()
    {
        Point tl = new Point(0, 0);
        Point br = new Point(Bounds.Width, Bounds.Height);

        double startX = tl.X;
        double endX = br.X;
        double startY = tl.Y;
        double endY = br.Y;

        var group = new DrawingGroup();
        using (var ctx = group.Open())
        {
            ctx.FillRectangle(Brushes.Red, new(tl.X, tl.Y, br.X, br.Y));
        }
        return group;
    }

    private void DrawZoom(DrawingContext ctx)
    {
        Point tl = new Point(0, 0);
        Point br = new Point(Bounds.Width, Bounds.Height);

        double w = 64;
        double h = 32;

        ctx.FillRectangle(Brushes.Black, new(16, Bounds.Height - 16 - h, w, h), 8);

        double availableSpaceX = 64 - 16;
        double availableSpaceY = 32 - 8;

        var text = new FormattedText(
            $"{Zoom * 100} %",
            new System.Globalization.CultureInfo(1),
            FlowDirection.LeftToRight,
            new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
            12,
            foreground: Brushes.White
        )
        {
            MaxTextWidth = availableSpaceX,
            MaxTextHeight = availableSpaceY,
            MaxLineCount = 1,
            Trimming = TextTrimming.WordEllipsis,
            TextAlignment = TextAlignment.Center,
        };

        ctx.DrawText(text, new(16 + 8, Bounds.Height - 16 - h + 8));
    }
}
