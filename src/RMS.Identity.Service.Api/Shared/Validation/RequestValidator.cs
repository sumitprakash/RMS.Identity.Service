namespace RMS.Identity.Service.Api.Shared.Validation;

public abstract class RequestValidator<TRequest> : IRequestValidator
    where TRequest : IValidatableRequest
{
    public Type RequestType => typeof(TRequest);

    public virtual void Validate(TRequest request)
    {
    }

    void IRequestValidator.Validate(object request)
    {
        Validate((TRequest)request);
    }
}
