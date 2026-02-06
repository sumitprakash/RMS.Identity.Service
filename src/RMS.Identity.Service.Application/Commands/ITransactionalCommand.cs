namespace RMS.Identity.Service.Application.Commands
{
    /// <summary>
    /// Marker: commands implementing this will be executed inside a DB transaction
    /// by the TransactionBehavior pipeline.
    /// </summary>
    public interface ITransactionalCommand { }
}
