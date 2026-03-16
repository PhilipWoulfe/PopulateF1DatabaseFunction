using F1.E2E.Tests.Infrastructure;
using F1.E2E.Tests.Pages;
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
            selectionPage.Navigate();
            selectionPage.WaitUntilReady();

            var selectableDrivers = selectionPage.GetSelectableDriverIds();
            Assert.True(selectableDrivers.Count >= 5, "Selection page must expose at least 5 selectable drivers.");

            var selected = selectableDrivers.Take(5).ToList();
            selectionPage.SelectTopFive(selected);
            selectionPage.ClickSubmit();
            selectionPage.WaitForSaveConfirmation();

            using var api = new ApiVerificationClient(options);
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
}
