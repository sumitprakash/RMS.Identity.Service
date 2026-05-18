namespace RMS.Identity.Service.Domain.Interfaces.Persistence;

public interface IDatabaseTransactionAccessor
{
    IDatabaseTransaction? Current { get; set; }

    IDatabaseTransaction GetCurrent();
}
