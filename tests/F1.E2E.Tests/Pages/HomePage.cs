using F1.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace F1.E2E.Tests.Pages;

internal class HomePage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public HomePage(IWebDriver driver, WebDriverWait wait, string baseUrl)
    {
        _driver = driver;
        _wait = wait;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public void Navigate()
    {
        _driver.Navigate().GoToUrl(_baseUrl + "/");
    }

    public void WaitForAuthenticatedNavigation()
    {
        PageReadiness.WaitForAppReady(
            _driver,
            _wait.Timeout,
            driver => driver.FindElements(By.CssSelector("a[href='yas-marina-selection']")).Count > 0);
    }

    public bool IsAccessDeniedVisible()
    {
        return _driver.PageSource.Contains("Access Denied", StringComparison.OrdinalIgnoreCase);
    }
}
