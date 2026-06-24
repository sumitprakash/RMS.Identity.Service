using System.Linq.Expressions;
using System.Reflection;

namespace RMS.Identity.Service.Api.Shared.ModelBinding;

internal sealed class PropertyBindingMetadata<TRequest>
{
    public PropertyBindingMetadata(
        PropertyInfo property,
        string name,
        Action<TRequest, object?> setValue)
    {
        Property = property;
        Name = name;
        SetValue = setValue;
    }

    public PropertyInfo Property { get; }

    public string Name { get; }

    public Action<TRequest, object?> SetValue { get; }

    public static PropertyBindingMetadata<TRequest> Create(PropertyInfo property, string name)
    {
        return new PropertyBindingMetadata<TRequest>(property, name, CreateSetter(property));
    }

    private static Action<TRequest, object?> CreateSetter(PropertyInfo property)
    {
        if (property.SetMethod is null)
        {
            throw new InvalidOperationException($"{typeof(TRequest).Name}.{property.Name} must have a setter.");
        }

        var request = Expression.Parameter(typeof(TRequest), "request");
        var value = Expression.Parameter(typeof(object), "value");
        var convertedValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(request, property), convertedValue);

        return Expression.Lambda<Action<TRequest, object?>>(assign, request, value).Compile();
    }
}
