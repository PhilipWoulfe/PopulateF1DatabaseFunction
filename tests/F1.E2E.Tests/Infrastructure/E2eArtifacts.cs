using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit.Abstractions;

namespace F1.E2E.Tests.Infrastructure;

internal static class E2eArtifacts
{
    private static readonly HashSet<char> InvalidFileNameChars = new(Path.GetInvalidFileNameChars());

    private static readonly string ArtifactsDir =
        ResolveArtifactsDir();

    /// <summary>
    /// Captures a screenshot and page HTML to the artifacts directory when a test fails.
    /// Exceptions during capture are suppressed so the original test failure is preserved.
    /// </summary>
    public static void CaptureOnFailure(ChromeDriver driver, string testName, ITestOutputHelper? output = null)
    {
        try
        {
            Directory.CreateDirectory(ArtifactsDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var safeName = string.Concat(testName.Where(c => !InvalidFileNameChars.Contains(c)));

            var screenshotPath = Path.Combine(ArtifactsDir, $"{safeName}-{timestamp}.png");
            ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(screenshotPath);
            output?.WriteLine($"[E2E] Screenshot saved: {screenshotPath}");

            var htmlPath = Path.Combine(ArtifactsDir, $"{safeName}-{timestamp}.html");
            File.WriteAllText(htmlPath, driver.PageSource);
            output?.WriteLine($"[E2E] Page HTML saved: {htmlPath}");
        }
        catch (Exception ex)
        {
            output?.WriteLine($"[E2E] Artifact capture failed (non-fatal): {ex.Message}");
        }
    }

    private static string ResolveArtifactsDir()
    {
        var configuredPath = Environment.GetEnvironmentVariable("E2E_ARTIFACT_PATH");
        return E2ePathResolver.ResolveArtifactsDir(
            configuredPath,
            "TestResults",
            "e2e",
            "failure-artifacts");
    }
}
