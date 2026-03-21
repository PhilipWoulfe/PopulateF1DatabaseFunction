using Xunit.Abstractions;

namespace F1.E2E.Tests.Infrastructure;

internal sealed class E2eStepTrace : IDisposable
{
    private static readonly HashSet<char> InvalidFileNameChars = new(Path.GetInvalidFileNameChars());

    private readonly object _sync = new();
    private readonly StreamWriter _writer;
    private readonly ITestOutputHelper? _output;

    public string LogPath { get; }

    private E2eStepTrace(string logPath, ITestOutputHelper? output)
    {
        LogPath = logPath;
        _output = output;
        _writer = new StreamWriter(new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
        Log("Step trace started.");
    }

    public static E2eStepTrace Start(string testName, ITestOutputHelper? output = null)
    {
        var artifactsDir = ResolveArtifactsDir();
        Directory.CreateDirectory(artifactsDir);

        var safeName = string.Concat(testName.Where(c => !InvalidFileNameChars.Contains(c)));
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var logPath = Path.Combine(artifactsDir, $"{safeName}-{timestamp}.log");

        var trace = new E2eStepTrace(logPath, output);
        trace.Log($"Log file: {logPath}");
        return trace;
    }

    public void Log(string message)
    {
        var line = $"[{DateTime.UtcNow:O}] {message}";
        lock (_sync)
        {
            _writer.WriteLine(line);
        }

        _output?.WriteLine($"[E2E TRACE] {message}");
    }

    public void Dispose()
    {
        Log("Step trace completed.");
        _writer.Dispose();
    }

    private static string ResolveArtifactsDir()
    {
        var configuredPath = Environment.GetEnvironmentVariable("E2E_STEP_TRACE_PATH");
        return E2ePathResolver.ResolveArtifactsDir(
            configuredPath,
            "TestResults",
            "e2e",
            "step-traces");
    }
}
