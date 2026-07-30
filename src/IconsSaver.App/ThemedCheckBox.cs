using System.Drawing.Drawing2D;

namespace IconsSaver.App;

/// <summary>
/// A check box that draws its own glyph in the current palette.
/// </summary>
/// <remarks>
/// The stock check box is drawn by the Windows theme engine and ignores <c>ForeColor</c> and
/// <c>BackColor</c> for the box itself, so on a dark window it renders as a pale square with a tick
/// that is nearly invisible. <see cref="FlatStyle.Flat"/> is no better: it produces a grey box with
/// a dark tick. Painting it is the only way to get a glyph that reads at a glance, and it has the
/// side benefit of looking identical in both palettes.
/// </remarks>
internal sealed class ThemedCheckBox : CheckBox
{
    private const int GlyphSize = 16;
    private const int GlyphGap = 8;

    public ThemedCheckBox()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor,
            true);

        AutoSize = true;
        FlatStyle = FlatStyle.Flat;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text = TextRenderer.MeasureText(Text, Font);
        return new Size(GlyphSize + GlyphGap + text.Width + 4, Math.Max(GlyphSize + 4, text.Height + 4));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.Clear(BackColor);

        int top = (Height - GlyphSize) / 2;
        var glyph = new Rectangle(0, top, GlyphSize, GlyphSize);

        Color fill = Checked && Enabled ? Theme.Link : Theme.Surface;
        Color border = Checked && Enabled ? Theme.Link : Theme.Border;

        using (var background = new SolidBrush(fill))
        {
            graphics.FillRectangle(background, glyph);
        }

        using (var outline = new Pen(border))
        {
            graphics.DrawRectangle(outline, glyph);
        }

        if (Checked)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var tick = new Pen(Enabled ? Color.White : Theme.TextDisabled, 2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };

            graphics.DrawLines(tick, new[]
            {
                new PointF(glyph.Left + 3.5f, glyph.Top + 8.0f),
                new PointF(glyph.Left + 6.5f, glyph.Top + 11.5f),
                new PointF(glyph.Left + 12.5f, glyph.Top + 4.5f),
            });

            graphics.SmoothingMode = SmoothingMode.Default;
        }

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            new Rectangle(GlyphSize + GlyphGap, 0, Width - GlyphSize - GlyphGap, Height),
            Enabled ? ForeColor : Theme.TextDisabled,
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        if (Focused)
        {
            ControlPaint.DrawFocusRectangle(graphics, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }
}
