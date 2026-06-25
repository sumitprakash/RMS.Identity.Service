using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace RMS.Identity.Service.Api.Shared.ModelBinding;

internal sealed class RequestBindingMetadata<TRequest>
{
    public RequestBindingMetadata(
        PropertyBindingMetadata<TRequest>? bodyProperty,
        IReadOnlyCollection<PropertyBindingMetadata<TRequest>> routeProperties,
        IReadOnlyCollection<PropertyBindingMetadata<TRequest>> queryProperties)
    {
        BodyProperty = bodyProperty;
        RouteProperties = routeProperties;
        QueryProperties = queryProperties;
    }

    public PropertyBindingMetadata<TRequest>? BodyProperty { get; }

    public IReadOnlyCollection<PropertyBindingMetadata<TRequest>> RouteProperties { get; }

    public IReadOnlyCollection<PropertyBindingMetadata<TRequest>> QueryProperties { get; }

    public static RequestBindingMetadata<TRequest> Create()
    {
        var properties = GetRequestProperties();

        return new RequestBindingMetadata<TRequest>(
            CreateBodyProperty(properties),
            CreateRouteProperties(properties),
            CreateQueryProperties(properties));
    }

    private static PropertyInfo[] GetRequestProperties()
    {
        return typeof(TRequest).GetProperties(BindingFlags.Instance | BindingFlags.Public);
    }

    private static PropertyBindingMetadata<TRequest>? CreateBodyProperty(IEnumerable<PropertyInfo> properties)
    {
        var bodyProperties = properties
            .Where(property => property.GetCustomAttribute<FromBodyAttribute>() is not null)
            .Select(property => CreateProperty(property, property.Name))
            .ToArray();

        if (bodyProperties.Length > 1)
        {
            throw new InvalidOperationException($"{typeof(TRequest).Name} can only define one FromBody property.");
        }

        return bodyProperties.SingleOrDefault();
    }

    private static IReadOnlyCollection<PropertyBindingMetadata<TRequest>> CreateRouteProperties(IEnumerable<PropertyInfo> properties)
    {
        return CreateNamedProperties<FromRouteAttribute>(properties, attribute => attribute.Name);
    }

    private static IReadOnlyCollection<PropertyBindingMetadata<TRequest>> CreateQueryProperties(IEnumerable<PropertyInfo> properties)
    {
        return CreateNamedProperties<FromQueryAttribute>(properties, attribute => attribute.Name);
    }

    private static IReadOnlyCollection<PropertyBindingMetadata<TRequest>> CreateNamedProperties<TAttribute>(
        IEnumerable<PropertyInfo> properties,
        Func<TAttribute, string?> getName)
        where TAttribute : Attribute
    {
        return properties
            .Select(property => CreateNamedProperty(property, getName))
            .Where(property => property is not null)
            .Cast<PropertyBindingMetadata<TRequest>>()
            .ToArray();
    }

    private static PropertyBindingMetadata<TRequest>? CreateNamedProperty<TAttribute>(
        PropertyInfo property,
        Func<TAttribute, string?> getName)
        where TAttribute : Attribute
    {
        var attribute = property.GetCustomAttribute<TAttribute>();

        if (attribute is null)
        {
            return null;
        }

        return CreateProperty(property, getName(attribute) ?? property.Name);
    }

    private static PropertyBindingMetadata<TRequest> CreateProperty(PropertyInfo property, string name)
    {
        return PropertyBindingMetadata<TRequest>.Create(property, name);
    }
}
