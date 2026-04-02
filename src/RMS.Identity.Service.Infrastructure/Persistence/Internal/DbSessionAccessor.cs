namespace RMS.Identity.Service.Infrastructure.Persistence.Internal;

internal sealed class DbSessionAccessor
{
    private readonly AsyncLocal<DbSession?> _current = new();

    public DbSession? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
