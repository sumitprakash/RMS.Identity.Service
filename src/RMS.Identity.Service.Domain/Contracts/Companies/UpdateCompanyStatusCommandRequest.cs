using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record UpdateCompanyStatusCommandRequest(
    Guid ActorUserUuid,
    Guid CompanyUuid,
    string Status) : ICommand<UpdateCompanyStatusCommandResponse>;
