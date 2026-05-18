using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed class SignUpRequestModelBinder : IModelBinder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public SignUpRequestModelBinder(IOptions<JsonOptions> jsonOptions)
    {
        _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<SignUpRequestBody>(
                bindingContext.HttpContext.Request.Body,
                _jsonSerializerOptions,
                bindingContext.HttpContext.RequestAborted);

            if (body is null)
            {
                bindingContext.ModelState.AddModelError(nameof(SignUpRequest.Body), "Request body is required.");
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            bindingContext.Result = ModelBindingResult.Success(new SignUpRequest(body));
        }
        catch (JsonException exception)
        {
            bindingContext.ModelState.AddModelError(nameof(SignUpRequest.Body), exception.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }
}
