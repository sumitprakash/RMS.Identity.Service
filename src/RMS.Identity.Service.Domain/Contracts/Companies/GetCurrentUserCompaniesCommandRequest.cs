using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record GetCurrentUserCompaniesCommandRequest(
    Guid UserUuid) : ICommand<GetCurrentUserCompaniesCommandResponse>;
