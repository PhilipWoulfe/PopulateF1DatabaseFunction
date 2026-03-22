using F1.Web.Components;
using F1.Web.Models;

namespace F1.Web.Tests.Components;

public class DriverSelectionListTests : BunitContext
{
    [Fact]
    public void DriverSelectionList_ShouldRenderFiveSelectors_WithDriverOptions()
    {
        var selectedDriverIds = new List<string> { "", "", "", "", "" };
        var drivers = new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" }
        };

        var cut = Render<DriverSelectionList>(parameters => parameters
            .Add(p => p.Drivers, drivers)
            .Add(p => p.SelectedDriverIds, selectedDriverIds)
            .Add(p => p.IsReadOnly, false));

        Assert.Equal(5, cut.FindAll("select").Count);
        Assert.Contains("Lando Norris (norris)", cut.Markup);
        Assert.Contains("Charles Leclerc (leclerc)", cut.Markup);
    }

    [Fact]
    public void DriverSelectionList_ShouldUpdateSelectedDriverIds_WhenSelectionChanges()
    {
        var selectedDriverIds = new List<string> { "", "", "", "", "" };
        var drivers = new[]
        {
            new Driver { DriverId = "norris", FullName = "Lando Norris" },
            new Driver { DriverId = "leclerc", FullName = "Charles Leclerc" }
        };

        var cut = Render<DriverSelectionList>(parameters => parameters
            .Add(p => p.Drivers, drivers)
            .Add(p => p.SelectedDriverIds, selectedDriverIds)
            .Add(p => p.IsReadOnly, false));

        cut.Find("#driver-select-1").Change("norris");
        cut.Find("#driver-select-2").Change("leclerc");

        Assert.Equal("norris", selectedDriverIds[0]);
        Assert.Equal("leclerc", selectedDriverIds[1]);
    }

    [Fact]
    public void DriverSelectionList_ShouldDisableSelectors_WhenReadOnly()
    {
        var selectedDriverIds = new List<string> { "", "", "", "", "" };

        var cut = Render<DriverSelectionList>(parameters => parameters
            .Add(p => p.Drivers, Array.Empty<Driver>())
            .Add(p => p.SelectedDriverIds, selectedDriverIds)
            .Add(p => p.IsReadOnly, true));

        Assert.True(cut.FindAll("select").All(element => element.HasAttribute("disabled")));
    }
}
