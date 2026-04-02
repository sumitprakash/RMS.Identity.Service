namespace RMS.Identity.Service.Domain.Exceptions;

public class ValidationException : DomainException
{
    public ValidationException(string code, string message, object? details = null)
        : base(code, message)
    {
        DetailsValue = details;
    }

    private object? DetailsValue { get; }

    public override object? Details => DetailsValue;
}
