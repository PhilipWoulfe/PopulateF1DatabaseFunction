namespace F1.Services;

public class MetadataValidationException : Exception
{
    public MetadataValidationException(string message) : base(message)
    {
    }
}