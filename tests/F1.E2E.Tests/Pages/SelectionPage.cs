using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace F1.E2E.Tests.Pages;

internal class SelectionPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public SelectionPage(IWebDriver driver, WebDriverWait wait, string baseUrl)
    {
        _driver = driver;
        _wait = wait;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public void Navigate()
    {
        _driver.Navigate().GoToUrl(_baseUrl + "/australia-selection");
    }

    public void WaitUntilReady()
    {
        _wait.Until(driver => driver.FindElements(By.CssSelector("select[id^='driver-select-']")).Count == 5);
    }

    public IReadOnlyList<string> GetSelectableDriverIds()
    {
        var firstDropdown = _driver.FindElement(By.Id("driver-select-1"));
        var select = new SelectElement(firstDropdown);

        return select.Options
            .Select(option => option.GetAttribute("value"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void SelectTopFive(IReadOnlyList<string> driverIds)
    {
        if (driverIds.Count < 5)
        {
            throw new InvalidOperationException("At least 5 unique driver IDs are required.");
        }

        for (var i = 0; i < 5; i++)
        {
            var dropdown = new SelectElement(_driver.FindElement(By.Id($"driver-select-{i + 1}")));
            dropdown.SelectByValue(driverIds[i]);
        }
    }

    public void ClickSubmit()
    {
        var submit = _wait.Until(driver =>
        {
            var element = driver.FindElement(By.Id("submit-selection"));
            return element.Displayed && element.Enabled ? element : null;
        })!;

        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].scrollIntoView({block: 'center', inline: 'center'});",
            submit);

        _wait.Until(_ => submit.Displayed && submit.Enabled);

        try
        {
            submit.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", submit);
        }
    }

    public void WaitForSaveConfirmation()
    {
        _wait.Until(driver => driver.PageSource.Contains("Selection saved successfully.", StringComparison.Ordinal));
    }

    public bool IsAnyDropdownDisabled()
    {
        for (var i = 0; i < 5; i++)
        {
            var dropdown = _driver.FindElement(By.Id($"driver-select-{i + 1}"));
            if (dropdown.GetAttribute("disabled") != null)
                return true;
        }
        return false;
    }

    public bool IsSubmitDisabled()
    {
        var submit = _driver.FindElement(By.Id("submit-selection"));
        return !submit.Enabled || submit.GetAttribute("disabled") != null;
    }

    public bool IsLockedMessageVisible()
    {
        return _driver.PageSource.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
               _driver.PageSource.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
               _driver.PageSource.Contains("error", StringComparison.OrdinalIgnoreCase);
    }
}
