using RMS.Identity.Service.Infrastructure.Data;

namespace RMS.Identity.Service.Infrastructure.Repositories.Implementation
{
    public abstract class BaseRepository
    {
        protected readonly DbExecutor Db;

        protected BaseRepository(DbExecutor db)
        {
            Db = db;
        }
    }

}
