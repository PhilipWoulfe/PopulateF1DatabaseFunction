using F1.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace F1.E2E.Tests.Pages;

internal class AdminPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private readonly Action<string> _trace;

    public AdminPage(IWebDriver driver, WebDriverWait wait, string baseUrl, Action<string>? trace = null)
    {
        _driver = driver;
        _wait = wait;
        _baseUrl = baseUrl.TrimEnd('/');
        _trace = trace ?? (_ => { });
    }

    public void Navigate()
    {
        _trace($"Navigate -> {_baseUrl}/admin");
        _driver.Navigate().GoToUrl(_baseUrl + "/admin");
        _trace($"Navigation complete. Current URL: {_driver.Url}");
    }

    public void WaitUntilReady()
    {
        _trace("Waiting for admin metadata form to render...");
        PageReadiness.WaitForAppReady(
            _driver,
            _wait.Timeout,
            driver => driver.FindElements(By.Id("h2h-question")).Count > 0);
        _trace("Admin metadata form rendered.");
    }

    public void SetQuestions(string h2hQuestion, string bonusQuestion)
    {
        _trace("Populating admin metadata questions.");
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
            _trace("Publish toggle is off; clicking to enable.");
            checkbox.Click();
            return;
        }

        _trace("Publish toggle already enabled.");
    }

    public void ClickSave()
    {
        _trace("Clicking save metadata button.");
        _driver.FindElement(By.Id("save-metadata")).Click();
    }

    public void WaitForSaveConfirmation()
    {
        _trace("Waiting for 'Metadata saved' confirmation...");
        _wait.Until(driver => driver.PageSource.Contains("Metadata saved", StringComparison.Ordinal));
        _trace("Metadata save confirmation displayed.");
    }
}
