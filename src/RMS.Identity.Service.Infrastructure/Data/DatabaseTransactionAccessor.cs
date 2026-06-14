using RMS.Identity.Service.Application.Shared.Errors;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Infrastructure.Data;

public sealed class DatabaseTransactionAccessor : IDatabaseTransactionAccessor
{
    public IDatabaseTransaction? Current { get; set; }

    public IDatabaseTransaction GetCurrent()
    {
        return Current
            ?? throw new InternalServerErrorException(ServiceErrors.General.DatabaseTransactionMissing, null);
    }
}
