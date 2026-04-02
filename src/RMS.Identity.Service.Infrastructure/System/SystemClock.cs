using RMS.Identity.Service.Domain.Interfaces.System;

namespace RMS.Identity.Service.Infrastructure.System;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
