using MediatR;
using RMS.Identity.Service.Infrastructure.Data;
using RMS.Identity.Service.Application.Commands;

public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IUnitOfWork _uow;

    public TransactionBehavior(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalCommand)
            return await next();

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