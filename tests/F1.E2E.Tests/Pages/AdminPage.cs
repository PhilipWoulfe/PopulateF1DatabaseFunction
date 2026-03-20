using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace F1.E2E.Tests.Pages;

internal class AdminPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public AdminPage(IWebDriver driver, WebDriverWait wait, string baseUrl)
    {
        _driver = driver;
        _wait = wait;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public void Navigate()
    {
        _driver.Navigate().GoToUrl(_baseUrl + "/admin");
    }

    public void WaitUntilReady()
    {
        const int maxAttempts = 3;
        var attemptTimeout = TimeSpan.FromSeconds(10);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var attemptWait = new WebDriverWait(_driver, attemptTimeout);
                attemptWait.Until(driver =>
                {
                    if (IsBlazorErrorVisible(driver))
                    {
                        return false;
                    }

                    return driver.FindElements(By.Id("h2h-question")).Count > 0;
                });
                return;
            }
            catch (WebDriverTimeoutException) when (attempt < maxAttempts)
            {
                _driver.Navigate().Refresh();
            }
        }

        _wait.Until(driver => driver.FindElements(By.Id("h2h-question")).Count > 0);
    }

    private static bool IsBlazorErrorVisible(IWebDriver driver)
    {
        var elements = driver.FindElements(By.Id("blazor-error-ui"));
        return elements.Count > 0 && elements[0].Displayed;
    }

    public void SetQuestions(string h2hQuestion, string bonusQuestion)
    {
        var h2h = _driver.FindElement(By.Id("h2h-question"));
        h2h.Clear();
        h2h.SendKeys(h2hQuestion);

        var bonus = _driver.FindElement(By.Id("bonus-question"));
        bonus.Clear();
        bonus.SendKeys(bonusQuestion);
    }

    public void EnsurePublished()
    {
        var checkbox = _driver.FindElement(By.Id("publish-toggle"));
        if (!checkbox.Selected)
        {
            checkbox.Click();
        }
    }

    public void ClickSave()
    {
        _driver.FindElement(By.Id("save-metadata")).Click();
    }

    public void WaitForSaveConfirmation()
    {
        _wait.Until(driver => driver.PageSource.Contains("Metadata saved", StringComparison.Ordinal));
    }
}
