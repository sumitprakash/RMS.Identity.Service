namespace RMS.Identity.Service.Domain.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string code, string message, object? details = null)
        : base(code, message)
    {
        DetailsValue = details;
    }

    private object? DetailsValue { get; }

    public override int StatusCode => 409;

    public override object? Details => DetailsValue;
}
