using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record GetMyCompaniesCommandRequest(
    Guid UserUuid) : ICommand<GetMyCompaniesCommandResponse>;
