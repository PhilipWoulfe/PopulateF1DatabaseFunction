using Bunit.TestDoubles;
using F1.Web.Layout;

namespace F1.Web.Tests.Layout;

public class NavMenuTests : TestContext
{
    [Fact]
    public void NavMenu_ShouldHideAuthorizedLinks_WhenAnonymous()
    {
        var auth = this.AddTestAuthorization();
        auth.SetNotAuthorized();

        var cut = RenderComponent<NavMenu>();

        Assert.DoesNotContain("Driver Standings", cut.Markup);
        Assert.DoesNotContain("Australia GP", cut.Markup);
        Assert.DoesNotContain("Drivers", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShouldShowAuthorizedLinks_WhenAuthenticated()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("user@example.com");

        var cut = RenderComponent<NavMenu>();

        Assert.Contains("Driver Standings", cut.Markup);
        Assert.Contains("Australia GP", cut.Markup);
        Assert.Contains("Drivers", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShouldShowAdminLink_WhenInAdminRole()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("admin@example.com");
        auth.SetRoles("Admin");

        var cut = RenderComponent<NavMenu>();

        Assert.Contains("Admin", cut.Markup);
    }

    [Fact]
    public void NavMenu_ShouldToggleCollapsedState_WhenClicked()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("user@example.com");

        var cut = RenderComponent<NavMenu>();

        Assert.Contains("collapse", cut.Markup);
        cut.Find("button.navbar-toggler").Click();
        Assert.DoesNotContain("collapse nav-scrollable", cut.Markup);
    }
}
