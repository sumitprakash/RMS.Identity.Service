namespace RMS.Identity.Service.Domain.Shared.Errors;

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
}
