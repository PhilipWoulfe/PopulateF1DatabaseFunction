using F1.Core.Interfaces;
using F1.Core.Models;

namespace F1.Services;

public class RaceService : IRaceService
{
    public List<RaceResult> GetMockResults()
    {
        return new List<RaceResult>
        {
            new RaceResult { DriverId = "norbert2", Position = 1, Points = 25 },
            new RaceResult { DriverId = "pastrami", Position = 2, Points = 18 }
        };
    }
}