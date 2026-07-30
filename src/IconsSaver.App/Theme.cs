using IconsSaver.Core.Models;
using Microsoft.Win32;

namespace IconsSaver.App;

/// <summary>
/// The colour scheme, and the code that pushes it onto a window tree.
/// </summary>
/// <remarks>
/// <para>
/// WinForms has no theming of its own, so this is done by hand: every control gets its colours
/// set, the two that Windows draws itself (the title bar and the list view's scroll bars) are
/// repointed at the dark visual style, and the tool strips get a custom renderer.
/// </para>
/// <para>
/// A handful of things stay light no matter what — <see cref="MessageBox"/> and the common file
/// dialogs are drawn by the shell and take no instruction from us.
/// </para>
/// </remarks>
internal static class Theme
{
    private static readonly Color DarkBackground = Color.FromArgb(0x1F, 0x1F, 0x1F);
    private static readonly Color DarkSurface = Color.FromArgb(0x2B, 0x2B, 0x2B);
    private static readonly Color DarkSurfaceAlternate = Color.FromArgb(0x26, 0x26, 0x26);
    private static readonly Color DarkBorder = Color.FromArgb(0x3D, 0x3D, 0x3D);
    private static readonly Color DarkText = Color.FromArgb(0xE8, 0xE8, 0xE8);
    private static readonly Color DarkTextMuted = Color.FromArgb(0x9A, 0x9A, 0x9A);
    private static readonly Color DarkTextDisabled = Color.FromArgb(0x6B, 0x6B, 0x6B);

    /// <summary>Matches the blue in the application icon.</summary>
    private static readonly Color Accent = Color.FromArgb(0x4C, 0x8D, 0xF6);

    private static readonly Color DarkSelection = Color.FromArgb(0x2D, 0x4E, 0x7E);
    private static readonly Color DarkHover = Color.FromArgb(0x38, 0x38, 0x38);

    /// <summary>Whether the dark palette is currently in force.</summary>
    public static bool IsDark { get; private set; } = true;

    public static Color Background => IsDark ? DarkBackground : SystemColors.Control;

    public static Color Surface => IsDark ? DarkSurface : SystemColors.Window;

    public static Color SurfaceAlternate => IsDark ? DarkSurfaceAlternate : Color.FromArgb(0xF6, 0xF6, 0xF6);

    public static Color Border => IsDark ? DarkBorder : SystemColors.ControlDark;

    public static Color Text => IsDark ? DarkText : SystemColors.ControlText;

    public static Color TextMuted => IsDark ? DarkTextMuted : SystemColors.GrayText;

    public static Color TextDisabled => IsDark ? DarkTextDisabled : SystemColors.GrayText;

    public static Color Selection => IsDark ? DarkSelection : SystemColors.Highlight;

    public static Color SelectionText => IsDark ? DarkText : SystemColors.HighlightText;

    public static Color Hover => IsDark ? DarkHover : SystemColors.ControlLight;

    public static Color Link => Accent;

    /// <summary>Background of the preview control, which is always darker than the window.</summary>
    public static Color PreviewBackground => IsDark ? Color.FromArgb(0x15, 0x17, 0x1C) : Color.FromArgb(0x22, 0x25, 0x2B);

    /// <summary>Resolves a preference into the palette to use.</summary>
    public static void Use(AppTheme preference) =>
        IsDark = preference switch
        {
            AppTheme.Light => false,
            AppTheme.Dark => true,
            _ => SystemPrefersDark(),
        };

    /// <summary>Reads the Windows "choose your default app mode" setting.</summary>
    private static bool SystemPrefersDark()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception exception) when (exception is IOException or System.Security.SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Applies the palette to a window and everything inside it.</summary>
    public static void Apply(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);

        form.BackColor = Background;
        form.ForeColor = Text;

        ApplyTitleBar(form);

        foreach (Control child in form.Controls)
        {
            ApplyTo(child);
        }

        form.Invalidate(invalidateChildren: true);
    }

    /// <summary>
    /// Paints the window frame to match. Needs the handle to exist, so it is re-applied whenever the
    /// window is recreated.
    /// </summary>
    public static void ApplyTitleBar(Form form)
    {
        if (!form.IsHandleCreated)
        {
            form.HandleCreated += (_, _) => ApplyTitleBar(form);
            return;
        }

        int enabled = IsDark ? 1 : 0;

        // Windows 10 builds before 19041 used attribute 19 for this; newer ones use 20. Setting the
        // wrong one is harmless, so both are attempted rather than version-sniffing.
        if (NativeMethods.DwmSetWindowAttribute(form.Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int)) != 0)
        {
            NativeMethods.DwmSetWindowAttribute(form.Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY, ref enabled, sizeof(int));
        }
    }

    private static void ApplyTo(Control control)
    {
        switch (control)
        {
            // StatusStrip derives from ToolStrip, so it has to be matched first or it never is.
            case StatusStrip statusStrip:
                statusStrip.Renderer = new ThemedToolStripRenderer();
                statusStrip.BackColor = Background;
                statusStrip.ForeColor = TextMuted;
                break;

            case ToolStrip toolStrip:
                toolStrip.Renderer = new ThemedToolStripRenderer();
                toolStrip.BackColor = Background;
                toolStrip.ForeColor = Text;
                break;

            case ListView listView:
                listView.BackColor = Surface;
                listView.ForeColor = Text;
                listView.BorderStyle = BorderStyle.None;
                ApplyExplorerTheme(listView);
                break;

            case Button button:
                button.FlatStyle = IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                button.BackColor = IsDark ? Surface : SystemColors.Control;
                button.ForeColor = Text;
                button.UseVisualStyleBackColor = !IsDark;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = Hover;
                button.FlatAppearance.MouseDownBackColor = Selection;
                break;

            // ThemedCheckBox paints its own glyph from this palette, so only the surrounding
            // colours need setting here.
            case CheckBox checkBox:
                checkBox.BackColor = Background;
                checkBox.ForeColor = Text;
                break;

            case LinkLabel linkLabel:
                linkLabel.BackColor = Background;
                linkLabel.LinkColor = Link;
                linkLabel.ActiveLinkColor = Link;
                linkLabel.VisitedLinkColor = Link;
                break;

            case TextBox textBox:
                textBox.BackColor = Surface;
                textBox.ForeColor = Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;

            case NumericUpDown numeric:
                numeric.BackColor = Surface;
                numeric.ForeColor = Text;
                numeric.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ComboBox comboBox:
                comboBox.FlatStyle = IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                comboBox.BackColor = Surface;
                comboBox.ForeColor = Text;
                break;

            case SplitContainer splitContainer:
                splitContainer.BackColor = Border;
                break;

            case DesktopPreview preview:
                preview.BackColor = PreviewBackground;
                break;

            case Label label:
                label.BackColor = Color.Transparent;

                // A label already coloured as grey text was meant to be secondary; keep it that way
                // rather than promoting it to the same weight as everything else.
                label.ForeColor = label.ForeColor == SystemColors.GrayText ? TextMuted : Text;
                break;

            default:
                control.BackColor = Background;
                control.ForeColor = Text;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyTo(child);
        }
    }

    /// <summary>Repoints a control at the dark visual style so its scroll bars follow suit.</summary>
    private static void ApplyExplorerTheme(Control control)
    {
        if (!control.IsHandleCreated)
        {
            control.HandleCreated += (_, _) => ApplyExplorerTheme(control);
            return;
        }

        NativeMethods.SetWindowTheme(control.Handle, IsDark ? "DarkMode_Explorer" : "Explorer", null);
    }
}
