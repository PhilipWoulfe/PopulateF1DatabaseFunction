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

    [Fact]
    public void Set_MockDate_Local_Normalizes_To_Utc()
    {
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var service = new GlobalMockDateService(cache);
        var localDate = DateTime.SpecifyKind(new DateTime(2025, 12, 19, 10, 0, 0), DateTimeKind.Local);

        service.SetMockDateUtc(localDate);

        var stored = service.GetMockDateUtc();
        Assert.NotNull(stored);
        Assert.Equal(DateTimeKind.Utc, stored!.Value.Kind);
        Assert.Equal(localDate.ToUniversalTime(), stored.Value);
    }

    [Fact]
    public void Set_MockDate_Unspecified_Assumes_Utc()
    {
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var service = new GlobalMockDateService(cache);
        var unspecified = new DateTime(2025, 12, 19, 10, 0, 0, DateTimeKind.Unspecified);

        service.SetMockDateUtc(unspecified);

        var stored = service.GetMockDateUtc();
        Assert.NotNull(stored);
        Assert.Equal(DateTimeKind.Utc, stored!.Value.Kind);
        Assert.Equal(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc), stored.Value);
    }
}
