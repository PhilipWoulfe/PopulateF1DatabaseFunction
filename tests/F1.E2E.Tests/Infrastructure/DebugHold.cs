namespace F1.E2E.Tests.Infrastructure;

internal static class DebugHold
{
    public static void WaitIfEnabled(string checkpoint)
    {
        var rawValue = Environment.GetEnvironmentVariable("E2E_DEBUG_HOLD_SECONDS");
        if (!int.TryParse(rawValue, out var seconds) || seconds <= 0)
        {
            return;
        }

        Console.WriteLine($"[E2E DEBUG] Holding at '{checkpoint}' for {seconds}s so DevTools can stay attached.");
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
    }
}