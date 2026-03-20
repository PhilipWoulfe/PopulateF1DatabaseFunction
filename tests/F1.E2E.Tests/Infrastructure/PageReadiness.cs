using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace F1.E2E.Tests.Infrastructure;

internal static class PageReadiness
{
    public static void WaitForAppReady(
        IWebDriver driver,
        TimeSpan timeout,
        Func<IWebDriver, bool> readyCondition,
        int maxAttempts = 3)
    {
        var perAttemptTimeout = TimeSpan.FromMilliseconds(Math.Max(1000, timeout.TotalMilliseconds / maxAttempts));
        Exception? lastTimeout = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var wait = new WebDriverWait(driver, perAttemptTimeout);
                wait.Until(d =>
                {
                    if (IsBlazorErrorVisible(d) || IsLoadingScreenVisible(d))
                    {
                        return false;
                    }

                    return readyCondition(d);
                });
                return;
            }
            catch (WebDriverTimeoutException ex)
            {
                lastTimeout = ex;
                if (attempt < maxAttempts)
                {
                    driver.Navigate().Refresh();
                }
            }
        }

        throw new WebDriverTimeoutException(
            $"App did not become ready after {maxAttempts} attempts.",
            lastTimeout);
    }

    private static bool IsBlazorErrorVisible(IWebDriver driver)
    {
        var elements = driver.FindElements(By.Id("blazor-error-ui"));
        return elements.Count > 0 && elements[0].Displayed;
    }

    private static bool IsLoadingScreenVisible(IWebDriver driver)
    {
        return driver.FindElements(By.CssSelector("#app .loading-progress, #app .loading-progress-text"))
            .Any(el => el.Displayed);
    }
}