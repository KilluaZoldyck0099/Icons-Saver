namespace IconsSaver.App;

/// <summary>
/// Draws tool strips, status strips and menus in the current palette.
/// </summary>
/// <remarks>
/// Setting <c>BackColor</c> on a <see cref="ToolStrip"/> is not enough: the gradients, the hover
/// highlight, the drop-down frame and the separators all come from the renderer, and the stock one
/// hard-codes a light scheme.
/// </remarks>
internal sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
{
    public ThemedToolStripRenderer()
        : base(new ThemedColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.Background);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // A single hairline under a top-docked strip, instead of the stock etched border.
        if (e.ToolStrip is StatusStrip)
        {
            return;
        }

        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawLine(pen, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.Text : Theme.TextDisabled;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        Rectangle bounds = e.Item.ContentRectangle;
        using var pen = new Pen(Theme.Border);

        if (e.Vertical)
        {
            int x = bounds.Left + (bounds.Width / 2);
            e.Graphics.DrawLine(pen, x, bounds.Top + 3, x, bounds.Bottom - 3);
        }
        else
        {
            int y = bounds.Top + (bounds.Height / 2);
            e.Graphics.DrawLine(pen, bounds.Left + 3, y, bounds.Right - 3, y);
        }
    }
}

/// <summary>The colours <see cref="ToolStripProfessionalRenderer"/> asks for.</summary>
internal sealed class ThemedColorTable : ProfessionalColorTable
{
    public override Color ToolStripGradientBegin => Theme.Background;

    public override Color ToolStripGradientMiddle => Theme.Background;

    public override Color ToolStripGradientEnd => Theme.Background;

    public override Color ToolStripContentPanelGradientBegin => Theme.Background;

    public override Color ToolStripContentPanelGradientEnd => Theme.Background;

    public override Color StatusStripGradientBegin => Theme.Background;

    public override Color StatusStripGradientEnd => Theme.Background;

    public override Color ToolStripDropDownBackground => Theme.Surface;

    public override Color ImageMarginGradientBegin => Theme.Surface;

    public override Color ImageMarginGradientMiddle => Theme.Surface;

    public override Color ImageMarginGradientEnd => Theme.Surface;

    public override Color MenuBorder => Theme.Border;

    public override Color MenuItemBorder => Theme.Selection;

    public override Color MenuItemSelected => Theme.Selection;

    public override Color MenuItemSelectedGradientBegin => Theme.Selection;

    public override Color MenuItemSelectedGradientEnd => Theme.Selection;

    public override Color MenuItemPressedGradientBegin => Theme.Surface;

    public override Color MenuItemPressedGradientMiddle => Theme.Surface;

    public override Color MenuItemPressedGradientEnd => Theme.Surface;

    public override Color ButtonSelectedHighlight => Theme.Hover;

    public override Color ButtonSelectedGradientBegin => Theme.Hover;

    public override Color ButtonSelectedGradientMiddle => Theme.Hover;

    public override Color ButtonSelectedGradientEnd => Theme.Hover;

    public override Color ButtonSelectedBorder => Theme.Border;

    public override Color ButtonPressedGradientBegin => Theme.Selection;

    public override Color ButtonPressedGradientMiddle => Theme.Selection;

    public override Color ButtonPressedGradientEnd => Theme.Selection;

    public override Color ButtonCheckedGradientBegin => Theme.Selection;

    public override Color ButtonCheckedGradientMiddle => Theme.Selection;

    public override Color ButtonCheckedGradientEnd => Theme.Selection;

    public override Color SeparatorDark => Theme.Border;

    public override Color SeparatorLight => Theme.Border;
}
