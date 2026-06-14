namespace RMS.Identity.Service.Application.Shared.Errors;

public readonly record struct ServiceErrorCode
{
    public byte Group { get; }

    public byte Number { get; }

    public string ErrorCode { get; }

    public ServiceErrorCode(byte group, byte number)
    {
        if (group <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(group), "Error code group must be greater than zero.");
        }

        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Error code number must be greater than zero.");
        }

        Group = group;
        Number = number;

        ErrorCode = $"{Group}-{Number}";
    }

    public string ToResponseCode(int statusCode)
    {
        if (Group <= 0 || Number <= 0)
        {
            throw new InvalidOperationException("Service error code is not initialized.");
        }

        if (statusCode is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), "HTTP status code must be between 100 and 599.");
        }

        return $"{statusCode}-{ErrorCode}";
    }
}
