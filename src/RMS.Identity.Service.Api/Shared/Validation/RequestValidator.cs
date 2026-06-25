namespace RMS.Identity.Service.Api.Shared.Validation;

public abstract class RequestValidator<TRequest> : IRequestValidator
    where TRequest : IValidatableRequest
{
    public Type RequestType => typeof(TRequest);

    public void Validate(TRequest request)
    {
        DataAnnotationsObjectGraphValidator.Validate(request);
        ValidateRequest(request);
    }

    protected virtual void ValidateRequest(TRequest request)
    {
    }

    void IRequestValidator.Validate(object request)
    {
        Validate((TRequest)request);
    }
}
