namespace IconsSaver.App;

/// <summary>
/// A one-line text prompt, since WinForms does not ship one.
/// </summary>
internal sealed class InputDialog : Form
{
    private readonly TextBox _input;

    private InputDialog(string title, string prompt, string initialValue)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 132);
        Padding = new Padding(16);

        var label = new Label
        {
            Text = prompt,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 24,
        };

        _input = new TextBox
        {
            Text = initialValue,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 8, 0, 0),
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40,
            Padding = new Padding(0, 8, 0, 0),
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, MinimumSize = new Size(88, 28) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(88, 28) };

        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(_input);
        Controls.Add(label);
        Controls.Add(buttons);

        AcceptButton = ok;
        CancelButton = cancel;

        Shown += (_, _) =>
        {
            _input.Focus();
            _input.SelectAll();
        };
    }

    /// <summary>
    /// Shows the prompt and returns what the user typed, or <see langword="null"/> when cancelled.
    /// </summary>
    public static string? Ask(IWin32Window? owner, string title, string prompt, string initialValue = "")
    {
        using var dialog = new InputDialog(title, prompt, initialValue);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog._input.Text.Trim() : null;
    }
}
