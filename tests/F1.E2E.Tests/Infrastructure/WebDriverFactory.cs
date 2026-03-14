using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace F1.E2E.Tests.Infrastructure;

internal static class WebDriverFactory
{
    public static ChromeDriver Create(E2eOptions options)
    {
        var chromeOptions = new ChromeOptions();
        var chromeBinary = Environment.GetEnvironmentVariable("CHROME_BIN");
        if (!string.IsNullOrWhiteSpace(chromeBinary))
        {
            chromeOptions.BinaryLocation = chromeBinary;
        }

        chromeOptions.AddArgument("--window-size=1920,1400");
        chromeOptions.AddArgument("--disable-gpu");
        chromeOptions.AddArgument("--no-sandbox");
        chromeOptions.AddArgument("--disable-dev-shm-usage");
        chromeOptions.AddArgument("--remote-debugging-port=9222");

        if (options.Headless)
        {
            chromeOptions.AddArgument("--headless=new");
        }

        var driver = CreateDriver(chromeOptions);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;

        var headers = options.BuildCloudflareHeaders();
        if (headers.Count > 0)
        {
            driver.ExecuteCdpCommand("Network.enable", new Dictionary<string, object>());
            driver.ExecuteCdpCommand("Network.setExtraHTTPHeaders", new Dictionary<string, object>
            {
                ["headers"] = headers.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value, StringComparer.OrdinalIgnoreCase)
            });
        }

        return driver;
    }

    private static ChromeDriver CreateDriver(ChromeOptions chromeOptions)
    {
        var chromedriverPath = ResolveExecutablePath("chromedriver");
        if (!string.IsNullOrWhiteSpace(chromedriverPath))
        {
            var service = ChromeDriverService.CreateDefaultService();
            service.EnableVerboseLogging = true;

            var driverLogPath = Environment.GetEnvironmentVariable("CHROMEDRIVER_LOG");
            if (!string.IsNullOrWhiteSpace(driverLogPath))
            {
                var fullPath = Path.GetFullPath(driverLogPath);
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                service.LogPath = fullPath;
            }

            return new ChromeDriver(service, chromeOptions, TimeSpan.FromSeconds(60));
        }

        // Fall back to Selenium Manager when chromedriver is not preinstalled on the host.
        return new ChromeDriver(chromeOptions);
    }

    private static string? ResolveExecutablePath(string executableName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var fullPath = Path.Combine(directory, executableName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    public static WebDriverWait CreateWait(IWebDriver driver, TimeSpan timeout)
    {
        return new WebDriverWait(driver, timeout);
    }
}
