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

                    try
                    {
                        return readyCondition(d);
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
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
        foreach (var element in elements)
        {
            try
            {
                if (element.Displayed)
                {
                    return true;
                }
            }
            catch (StaleElementReferenceException)
            {
                // DOM is still settling; treat as not-ready and keep waiting.
                return true;
            }
        }

        return false;
    }

    private static bool IsLoadingScreenVisible(IWebDriver driver)
    {
        var loadingElements = driver.FindElements(By.CssSelector("#app .loading-progress, #app .loading-progress-text"));
        foreach (var element in loadingElements)
        {
            try
            {
                if (element.Displayed)
                {
                    return true;
                }
            }
            catch (StaleElementReferenceException)
            {
                // A stale loading node usually means hydration/re-render in progress.
                return true;
            }
        }

        return false;
    }
}