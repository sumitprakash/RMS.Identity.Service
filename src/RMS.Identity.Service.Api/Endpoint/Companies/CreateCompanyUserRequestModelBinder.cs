using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace RMS.Identity.Service.Api.Endpoint.Companies;

public sealed class CreateCompanyUserRequestModelBinder : IModelBinder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CreateCompanyUserRequestModelBinder(IOptions<JsonOptions> jsonOptions)
    {
        _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<CreateCompanyUserRequestBody>(
                bindingContext.HttpContext.Request.Body,
                _jsonSerializerOptions,
                bindingContext.HttpContext.RequestAborted);

            if (body is null)
            {
                bindingContext.ModelState.AddModelError(nameof(CreateCompanyUserRequest.Body), "Request body is required.");
                return;
            }

            bindingContext.Result = ModelBindingResult.Success(new CreateCompanyUserRequest(body));
        }
        catch (JsonException exception)
        {
            bindingContext.ModelState.AddModelError(nameof(CreateCompanyUserRequest.Body), exception.Message);
        }
    }
}
