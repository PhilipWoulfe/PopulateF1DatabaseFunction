namespace F1.Core.Exceptions;

public class RaceMetadataConcurrencyException : Exception
{
    public RaceMetadataConcurrencyException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}