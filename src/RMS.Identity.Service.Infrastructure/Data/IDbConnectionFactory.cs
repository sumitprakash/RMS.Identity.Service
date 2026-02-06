using System.Data;

namespace RMS.Identity.Service.Infrastructure.Data
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateOpenConnectionAsync();
    }
}
