namespace RMS.Identity.Service.Domain.Interfaces.System;

public interface IClock
{
    DateTime UtcNow { get; }
}
