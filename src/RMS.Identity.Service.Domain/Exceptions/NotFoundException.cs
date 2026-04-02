namespace RMS.Identity.Service.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string code, string message)
        : base(code, message)
    {
    }

    public override int StatusCode => 404;
}
