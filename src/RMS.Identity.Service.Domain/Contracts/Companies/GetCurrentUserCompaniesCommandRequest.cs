using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record GetCurrentUserCompaniesCommandRequest(
    Guid UserUuid) : ICommand<GetCurrentUserCompaniesCommandResponse>;
