using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace RMS.Identity.Service.Api.Endpoint.Auth.Login;

public sealed class LoginRequestModelBinder : IModelBinder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public LoginRequestModelBinder(IOptions<JsonOptions> jsonOptions)
    {
        _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<LoginRequestBody>(
                bindingContext.HttpContext.Request.Body,
                _jsonSerializerOptions,
                bindingContext.HttpContext.RequestAborted);

            if (body is null)
            {
                bindingContext.ModelState.AddModelError(nameof(LoginRequest.Body), "Request body is required.");
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            bindingContext.Result = ModelBindingResult.Success(new LoginRequest(body));
        }
        catch (JsonException exception)
        {
            bindingContext.ModelState.AddModelError(nameof(LoginRequest.Body), exception.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }
}
