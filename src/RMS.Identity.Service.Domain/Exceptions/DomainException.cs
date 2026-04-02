namespace RMS.Identity.Service.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public virtual int StatusCode => 400;

    public virtual object? Details => null;
}
