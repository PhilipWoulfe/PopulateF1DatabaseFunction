using F1.E2E.Tests.Infrastructure;
using F1.E2E.Tests.Pages;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace F1.E2E.Tests.Flows;

public class CriticalFlowsTests(ITestOutputHelper output)
{
    [Fact]
    public void Login_ShouldSucceed()
    {
        var options = E2eOptions.FromEnvironment();
        if (!options.Enabled)
        {
            return;
        }

        using var driver = WebDriverFactory.Create(options);
        var wait = WebDriverFactory.CreateWait(driver, options.Timeout);
        var homePage = new HomePage(driver, wait, options.BaseUrl);
        var testPassed = false;
        try
        {
            homePage.Navigate();
            homePage.WaitForAuthenticatedNavigation();

            Assert.False(homePage.IsAccessDeniedVisible());
            testPassed = true;
        }
        finally
        {
            if (!testPassed) E2eArtifacts.CaptureOnFailure(driver, nameof(Login_ShouldSucceed), output);
            DebugHold.WaitIfEnabled("Login_ShouldSucceed teardown");
        }
    }

    [Fact]
    public async Task SubmitSelection_ShouldPersistServerSide()
    {
        var options = E2eOptions.FromEnvironment();
        if (!options.Enabled)
        {
            return;
        }

        using var driver = WebDriverFactory.Create(options);
        var wait = WebDriverFactory.CreateWait(driver, options.Timeout);
        var selectionPage = new SelectionPage(driver, wait, options.BaseUrl);
        var testPassed = false;

        try
        {
            using var api = new ApiVerificationClient(options);

            await api.SetMockDate("2026-03-07T23:00:00Z", options.Timeout, CancellationToken.None);

            selectionPage.Navigate();
            selectionPage.WaitUntilReady();

            var selectableDrivers = selectionPage.GetSelectableDriverIds();
            Assert.True(selectableDrivers.Count >= 5, "Selection page must expose at least 5 selectable drivers.");

            var selected = selectableDrivers.Take(5).ToList();
            selectionPage.SelectTopFive(selected);
            selectionPage.ClickSubmit();
            selectionPage.WaitForSaveConfirmation();

            await api.WaitForSelectionPersistenceAsync(selected[0], options.Timeout, CancellationToken.None);
            testPassed = true;
        }
        finally
        {
            if (!testPassed) E2eArtifacts.CaptureOnFailure(driver, nameof(SubmitSelection_ShouldPersistServerSide), output);
            DebugHold.WaitIfEnabled("SubmitSelection_ShouldPersistServerSide teardown");
        }
    }

    [Fact]
    public async Task AdminPanel_ShouldLoadAndSaveMetadata()
    {
        var options = E2eOptions.FromEnvironment();
        if (!options.Enabled)
        {
            return;
        }

        using var driver = WebDriverFactory.Create(options);
        var wait = WebDriverFactory.CreateWait(driver, options.Timeout);
        var adminPage = new AdminPage(driver, wait, options.BaseUrl);
        var testPassed = false;
        try
        {
            adminPage.Navigate();
            adminPage.WaitUntilReady();

            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var h2hQuestion = $"E2E H2H {stamp}: Who finishes higher?";
            var bonusQuestion = $"E2E Bonus {stamp}: How many DNFs?";

            adminPage.SetQuestions(h2hQuestion, bonusQuestion);
            adminPage.EnsurePublished();
            adminPage.ClickSave();
            adminPage.WaitForSaveConfirmation();

            using var api = new ApiVerificationClient(options);
            var metadata = await api.WaitForMetadataAsync(options.RaceId, h2hQuestion, options.Timeout, CancellationToken.None);

            Assert.Equal(h2hQuestion, metadata.H2HQuestion);
            Assert.Equal(bonusQuestion, metadata.BonusQuestion);
            Assert.True(metadata.IsPublished);
            testPassed = true;
        }
        finally
        {
            if (!testPassed) E2eArtifacts.CaptureOnFailure(driver, nameof(AdminPanel_ShouldLoadAndSaveMetadata), output);
            DebugHold.WaitIfEnabled("AdminPanel_ShouldLoadAndSaveMetadata teardown");
        }
    }

    [Fact]
    public async Task SubmitSelection_ShouldBeForbidden_AfterDeadline_Api()
    {
        var options = E2eOptions.FromEnvironment();
        if (!options.Enabled)
        {
            return;
        }

        // Set mock date header to after the final deadline
        var afterDeadline = "2026-03-08T03:31:00Z";
        using var api = new ApiVerificationClient(options);
        api.SetMockDateHeader(afterDeadline);

        var submission = new
        {
            betType = "Regular",
            orderedSelections = new[]
            {
                new { position = 1, driverId = "norris" },
                new { position = 2, driverId = "leclerc" },
                new { position = 3, driverId = "hamilton" },
                new { position = 4, driverId = "piastri" },
                new { position = 5, driverId = "verstappen" }
            }
        };

        var response = await api.PostSelectionAsync("2026-australia", submission);
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
            $"Expected forbidden, bad request, or method not allowed, got {response.StatusCode}");
    }

    [Fact]
    public async Task SubmitSelection_ShouldShowError_AfterDeadline_Ui()
    {
        var options = E2eOptions.FromEnvironment();
        if (!options.Enabled)
        {
            return;
        }

        var afterDeadline = "2026-03-08T03:31:00Z";
        using var driver = WebDriverFactory.Create(options);
        var wait = WebDriverFactory.CreateWait(driver, options.Timeout);
        var selectionPage = new SelectionPage(driver, wait, options.BaseUrl);
        var testPassed = false;

        try
        {
            using (var api = new ApiVerificationClient(options))
            {
                await api.SetMockDate(afterDeadline, options.Timeout, CancellationToken.None);
            }

            selectionPage.Navigate();
            selectionPage.WaitUntilReady();

            // Instead of trying to select, check if dropdowns or submit are disabled (locked state)
            Assert.True(selectionPage.IsAnyDropdownDisabled() || selectionPage.IsSubmitDisabled(),
                "Expected selection UI to be locked/disabled after deadline.");
            Assert.True(selectionPage.IsLockedMessageVisible(),
                "Expected a lock/error/forbidden message after deadline.");

            testPassed = true;
        }
        finally
        {
            if (!testPassed) E2eArtifacts.CaptureOnFailure(driver, nameof(SubmitSelection_ShouldShowError_AfterDeadline_Ui), output);
            DebugHold.WaitIfEnabled("SubmitSelection_ShouldShowError_AfterDeadline_Ui teardown");
        }
    }
}
