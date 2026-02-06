using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RMS.Identity.Service.Infrastructure.Data;

namespace RMS.Identity.Service.Infrastructure.MediatR
{
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IUnitOfWork _uow;

        public TransactionBehavior(IUnitOfWork uow) => _uow = uow;

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            // If request is not a transactional command, just execute
            if (request is not RMS.Identity.Service.Application.Commands.ITransactionalCommand)
                return await next();

            // Begin transaction, execute handler, commit/rollback
            _uow.Begin();
            try
            {
                var response = await next();
                _uow.Commit();
                return response;
            }
            catch
            {
                _uow.Rollback();
                throw;
            }
        }
    }
}