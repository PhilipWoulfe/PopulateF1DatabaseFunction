using System;

namespace F1.Web.Services
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }

    public class DefaultTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
