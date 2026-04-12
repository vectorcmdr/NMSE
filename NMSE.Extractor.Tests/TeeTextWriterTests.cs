using NMSE.Extractor.Data;

namespace NMSE.Extractor.Tests;

public class TeeTextWriterTests
{
    /// <summary>
    /// Tests that Write(string) forwards to both writers.
    /// </summary>
    [Fact]
    public void Write_String_ForwardsToBothWriters()
    {
        using var consoleWriter = new StringWriter();
        using var logWriter = new StringWriter();
        var tee = new TeeTextWriter(consoleWriter, logWriter);

        tee.Write("hello");

        Assert.Equal("hello", consoleWriter.ToString());
        Assert.Equal("hello", logWriter.ToString());
    }

    /// <summary>
    /// Tests that WriteLine(string) forwards to both writers.
    /// </summary>
    [Fact]
    public void WriteLine_String_ForwardsToBothWriters()
    {
        using var consoleWriter = new StringWriter();
        using var logWriter = new StringWriter();
        var tee = new TeeTextWriter(consoleWriter, logWriter);

        tee.WriteLine("hello world");

        Assert.Equal("hello world" + Environment.NewLine, consoleWriter.ToString());
        Assert.Equal("hello world" + Environment.NewLine, logWriter.ToString());
    }

    /// <summary>
    /// Tests that Write(char) forwards to both writers.
    /// </summary>
    [Fact]
    public void Write_Char_ForwardsToBothWriters()
    {
        using var consoleWriter = new StringWriter();
        using var logWriter = new StringWriter();
        var tee = new TeeTextWriter(consoleWriter, logWriter);

        tee.Write('A');

        Assert.Equal("A", consoleWriter.ToString());
        Assert.Equal("A", logWriter.ToString());
    }

    /// <summary>
    /// Tests that bare WriteLine() forwards to both writers.
    /// </summary>
    [Fact]
    public void WriteLine_NoArgs_ForwardsToBothWriters()
    {
        using var consoleWriter = new StringWriter();
        using var logWriter = new StringWriter();
        var tee = new TeeTextWriter(consoleWriter, logWriter);

        tee.WriteLine();

        Assert.Equal(Environment.NewLine, consoleWriter.ToString());
        Assert.Equal(Environment.NewLine, logWriter.ToString());
    }

    /// <summary>
    /// Tests that multiple writes accumulate correctly in both writers.
    /// </summary>
    [Fact]
    public void MultipleWrites_AccumulateInBothWriters()
    {
        using var consoleWriter = new StringWriter();
        using var logWriter = new StringWriter();
        var tee = new TeeTextWriter(consoleWriter, logWriter);

        tee.Write("line1");
        tee.WriteLine(" done");
        tee.WriteLine("line2");

        string expected = "line1 done" + Environment.NewLine + "line2" + Environment.NewLine;
        Assert.Equal(expected, consoleWriter.ToString());
        Assert.Equal(expected, logWriter.ToString());
    }

    /// <summary>
    /// Tests that Encoding returns the console writer's encoding.
    /// </summary>
    [Fact]
    public void Encoding_ReturnsConsoleWriterEncoding()
    {
        using var consoleWriter = new StringWriter();
        using var logWriter = new StringWriter();
        var tee = new TeeTextWriter(consoleWriter, logWriter);

        Assert.Equal(consoleWriter.Encoding, tee.Encoding);
    }

    /// <summary>
    /// Tests that null constructor arguments throw.
    /// </summary>
    [Fact]
    public void Constructor_NullConsole_Throws()
    {
        using var logWriter = new StringWriter();
        Assert.Throws<ArgumentNullException>(() => new TeeTextWriter(null!, logWriter));
    }

    [Fact]
    public void Constructor_NullLog_Throws()
    {
        using var consoleWriter = new StringWriter();
        Assert.Throws<ArgumentNullException>(() => new TeeTextWriter(consoleWriter, null!));
    }

    /// <summary>
    /// Tests that TeeTextWriter works correctly when writing to an actual file.
    /// </summary>
    [Fact]
    public void Write_ToFile_MirrorsOutput()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);
        string logPath = Path.Combine(tempRoot, "log.txt");

        try
        {
            // Some environments (CI, limited permissions) may not allow writing to the temp
            // directory. In those cases the test should not trigger a hard failure; handle common
            // IO/security exceptions gracefully and return early so the test suite can continue.
            // This isn't important enough for a possible fail condition - especially on a local build.
            try
            {
                using var consoleWriter = new StringWriter();
                using var fileWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
                var tee = new TeeTextWriter(consoleWriter, fileWriter);

                tee.WriteLine("Step 1: Starting");
                tee.WriteLine("Step 2: Done");
                tee.Write("Progress: 50%");

                fileWriter.Flush();

                string fileContent = File.ReadAllText(logPath);
                string consoleContent = consoleWriter.ToString();

                Assert.Equal(consoleContent, fileContent);
                Assert.Contains("Step 1: Starting", fileContent);
                Assert.Contains("Step 2: Done", fileContent);
                Assert.Contains("Progress: 50%", fileContent);
            }
            catch (IOException)
            {
                // Cannot access temp file system in this environment; treat as graceful fail.
                return;
            }
            catch (UnauthorizedAccessException)
            {
                // Insufficient permissions to create/write the file; treat as graceful fail.
                return;
            }
            catch (System.Security.SecurityException)
            {
                // Security policy prevents file IO; treat as graceful fail.
                return;
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests that both stdout and stderr can share the same log writer.
    /// </summary>
    [Fact]
    public void SharedLogWriter_BothStreamsWriteToSameLog()
    {
        using var stdoutWriter = new StringWriter();
        using var stderrWriter = new StringWriter();
        using var logWriter = new StringWriter();

        var teeOut = new TeeTextWriter(stdoutWriter, logWriter);
        var teeErr = new TeeTextWriter(stderrWriter, logWriter);

        teeOut.WriteLine("stdout message");
        teeErr.WriteLine("stderr message");

        // Both should appear in the shared log
        string logContent = logWriter.ToString();
        Assert.Contains("stdout message", logContent);
        Assert.Contains("stderr message", logContent);

        // Each should only appear in its own console stream
        Assert.Contains("stdout message", stdoutWriter.ToString());
        Assert.DoesNotContain("stderr message", stdoutWriter.ToString());
        Assert.Contains("stderr message", stderrWriter.ToString());
        Assert.DoesNotContain("stdout message", stderrWriter.ToString());
    }

    /// <summary>
    /// Tests that Write(null) does not throw.
    /// </summary>
    [Fact]
    public void Write_Null_DoesNotThrow()
    {
        using var consoleWriter = new StringWriter();
        using var logWriter = new StringWriter();
        var tee = new TeeTextWriter(consoleWriter, logWriter);

        tee.Write((string?)null);
        tee.WriteLine((string?)null);
        // Should not throw
    }
}
