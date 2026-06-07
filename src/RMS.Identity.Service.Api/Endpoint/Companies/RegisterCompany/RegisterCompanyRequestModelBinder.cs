using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace RMS.Identity.Service.Api.Endpoint.Companies.RegisterCompany;

public sealed class RegisterCompanyRequestModelBinder : IModelBinder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public RegisterCompanyRequestModelBinder(IOptions<JsonOptions> jsonOptions)
    {
        _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<RegisterCompanyRequestBody>(
                bindingContext.HttpContext.Request.Body,
                _jsonSerializerOptions,
                bindingContext.HttpContext.RequestAborted);

            if (body is null)
            {
                bindingContext.ModelState.AddModelError(nameof(RegisterCompanyRequest.Body), "Request body is required.");
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            bindingContext.Result = ModelBindingResult.Success(new RegisterCompanyRequest(body));
        }
        catch (JsonException exception)
        {
            bindingContext.ModelState.AddModelError(nameof(RegisterCompanyRequest.Body), exception.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }
}
