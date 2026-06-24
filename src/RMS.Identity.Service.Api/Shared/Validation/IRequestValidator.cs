namespace RMS.Identity.Service.Api.Shared.Validation;

public interface IRequestValidator
{
    Type RequestType { get; }

    void Validate(object request);
}
