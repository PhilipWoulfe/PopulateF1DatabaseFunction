using System;
using F1.Services;
using Xunit;

public class GlobalMockDateServiceTests
{
    [Fact]
    public void Set_And_Get_MockDate_Works()
    {
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var service = new GlobalMockDateService(cache);
        var now = new DateTime(2025, 12, 19, 10, 0, 0, DateTimeKind.Utc);
        service.SetMockDateUtc(now);
        Assert.Equal(now, service.GetMockDateUtc());
    }

    [Fact]
    public void Set_Null_Removes_MockDate()
    {
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var service = new GlobalMockDateService(cache);
        service.SetMockDateUtc(new DateTime(2025, 12, 19, 10, 0, 0, DateTimeKind.Utc));
        service.SetMockDateUtc(null);
        Assert.Null(service.GetMockDateUtc());
    }
}
