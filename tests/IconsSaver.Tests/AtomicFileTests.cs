using System.Text;
using IconsSaver.Core.Storage;
using Xunit;

namespace IconsSaver.Tests;

public sealed class AtomicFileTests
{
    [Fact]
    public void Writes_a_new_file()
    {
        using var folder = new TempFolder();
        string path = folder.File("layout.json");

        AtomicFile.WriteAllText(path, "hello");

        Assert.Equal("hello", File.ReadAllText(path));
    }

    [Fact]
    public void Replaces_an_existing_file()
    {
        using var folder = new TempFolder();
        string path = folder.File("layout.json");

        AtomicFile.WriteAllText(path, "first");
        AtomicFile.WriteAllText(path, "second");

        Assert.Equal("second", File.ReadAllText(path));
    }

    [Fact]
    public void Writes_utf8_without_a_byte_order_mark()
    {
        using var folder = new TempFolder();
        string path = folder.File("layout.json");

        AtomicFile.WriteAllText(path, "Γραφείο");

        byte[] bytes = File.ReadAllBytes(path);

        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal("Γραφείο", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Creates_missing_directories()
    {
        using var folder = new TempFolder();
        string path = Path.Combine(folder.Path, "nested", "deeper", "layout.json");

        AtomicFile.WriteAllText(path, "hello");

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Leaves_no_temporary_files_behind()
    {
        using var folder = new TempFolder();

        AtomicFile.WriteAllText(folder.File("layout.json"), "hello");

        Assert.Empty(Directory.GetFiles(folder.Path, "*.tmp"));
    }
}
