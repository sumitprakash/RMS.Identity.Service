namespace RMS.Identity.Service.Api.Shared.Validation;

public abstract class RequestValidator<TRequest> : IRequestValidator
{
    public Type RequestType => typeof(TRequest);

    public abstract void Validate(TRequest request);

    void IRequestValidator.Validate(object request)
    {
        if (request is TRequest typedRequest)
        {
            Validate(typedRequest);
        }
    }
}
