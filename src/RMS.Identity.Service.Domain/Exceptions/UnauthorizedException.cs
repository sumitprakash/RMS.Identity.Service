namespace RMS.Identity.Service.Domain.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string code, string message)
        : base(code, message)
    {
    }

    public override int StatusCode => 401;
}
