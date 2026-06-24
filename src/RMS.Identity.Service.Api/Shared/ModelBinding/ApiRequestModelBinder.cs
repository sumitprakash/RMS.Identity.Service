using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace RMS.Identity.Service.Api.Shared.ModelBinding;

public sealed class ApiRequestModelBinder<TRequest> : IModelBinder
    where TRequest : new()
{
    private static readonly RequestBindingMetadata<TRequest> Metadata = RequestBindingMetadata<TRequest>.Create();
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ApiRequestModelBinder(IOptions<JsonOptions> jsonOptions)
    {
        _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var request = new TRequest();

        if (Metadata.BodyProperty is not null)
        {
            if (!await BindBodyAsync(bindingContext, request, Metadata.BodyProperty))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }
        }

        BindRouteValues(bindingContext, request);
        BindQueryValues(bindingContext, request);

        bindingContext.Result = bindingContext.ModelState.IsValid
            ? ModelBindingResult.Success(request)
            : ModelBindingResult.Failed();
    }

    private async Task<bool> BindBodyAsync(
        ModelBindingContext bindingContext,
        TRequest request,
        PropertyBindingMetadata<TRequest> bodyProperty)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync(
                bindingContext.HttpContext.Request.Body,
                bodyProperty.Property.PropertyType,
                _jsonSerializerOptions,
                bindingContext.HttpContext.RequestAborted);

            if (body is null)
            {
                bindingContext.ModelState.AddModelError(bodyProperty.Property.Name, "Request body is required.");
                return false;
            }

            bodyProperty.SetValue(request, body);
            return true;
        }
        catch (JsonException exception)
        {
            bindingContext.ModelState.AddModelError(bodyProperty.Property.Name, exception.Message);
            return false;
        }
    }

    private static void BindRouteValues(ModelBindingContext bindingContext, TRequest request)
    {
        foreach (var property in Metadata.RouteProperties)
        {
            var value = bindingContext.ActionContext.RouteData.Values[property.Name];
            BindSimpleValue(bindingContext, request, property, value);
        }
    }

    private static void BindQueryValues(ModelBindingContext bindingContext, TRequest request)
    {
        foreach (var property in Metadata.QueryProperties)
        {
            var value = bindingContext.HttpContext.Request.Query[property.Name].FirstOrDefault();
            BindSimpleValue(bindingContext, request, property, value);
        }
    }

    private static void BindSimpleValue(
        ModelBindingContext bindingContext,
        TRequest request,
        PropertyBindingMetadata<TRequest> property,
        object? value)
    {
        if (value is null || string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture)))
        {
            bindingContext.ModelState.AddModelError(property.Property.Name, $"{property.Name} is required.");
            return;
        }

        try
        {
            property.SetValue(request, ConvertValue(value, property.Property.PropertyType));
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or NotSupportedException)
        {
            bindingContext.ModelState.AddModelError(property.Property.Name, $"{property.Name} is invalid.");
        }
    }

    private static object? ConvertValue(object value, Type destinationType)
    {
        var targetType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(string))
        {
            return stringValue;
        }

        if (targetType == typeof(Guid))
        {
            return Guid.Parse(stringValue ?? string.Empty);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, stringValue ?? string.Empty, ignoreCase: true);
        }

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            return converter.ConvertFromInvariantString(stringValue ?? string.Empty);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
