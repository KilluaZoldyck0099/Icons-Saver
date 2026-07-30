using System.Diagnostics;
using IconsSaver.Core;
using IconsSaver.Core.Storage;

namespace IconsSaver.App;

/// <summary>The about box.</summary>
internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = $"About {AppInfo.Name}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 260);
        Padding = new Padding(18);
        Icon = AppIcons.Application;

        var title = new Label
        {
            Text = $"{AppInfo.Name} {AppInfo.Version}",
            Font = new Font(Font.FontFamily, Font.Size + 4f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(18, 18),
        };

        var blurb = new Label
        {
            Text =
                "Saves the position of every icon on your desktop and puts them all back " +
                "when Windows rearranges them.\r\n\r\n" +
                "Positions are read straight out of the shell's own icon view, so a restore " +
                "is exact rather than a best guess.",
            AutoSize = false,
            Location = new Point(18, 58),
            Size = new Size(424, 82),
        };

        var project = new LinkLabel
        {
            Text = AppInfo.ProjectUrl,
            AutoSize = true,
            Location = new Point(18, 148),
        };
        project.LinkClicked += (_, _) => OpenUrl(AppInfo.ProjectUrl);

        var dataFolder = new LinkLabel
        {
            Text = "Open the data folder",
            AutoSize = true,
            Location = new Point(18, 174),
        };
        dataFolder.LinkClicked += (_, _) => OpenUrl(AppPaths.Root);

        var license = new Label
        {
            Text = "MIT licensed. " + AppInfo.Copyright,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Location = new Point(18, 202),
        };

        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Size = new Size(96, 30),
            Location = new Point(346, 196),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };

        Controls.AddRange(new Control[] { title, blurb, project, dataFolder, license, close });
        AcceptButton = close;
        CancelButton = close;
    }

    private static void OpenUrl(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            MessageBox.Show($"Could not open {target}.", AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
