namespace F1.Core.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
