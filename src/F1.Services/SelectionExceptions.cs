namespace F1.Services;

public class SelectionValidationException : Exception
{
    public SelectionValidationException(string message) : base(message)
    {
    }
}

public class SelectionForbiddenException : Exception
{
    public SelectionForbiddenException(string message) : base(message)
    {
    }
}

public class SelectionRaceNotFoundException : Exception
{
    public SelectionRaceNotFoundException(string message) : base(message)
    {
    }
}
