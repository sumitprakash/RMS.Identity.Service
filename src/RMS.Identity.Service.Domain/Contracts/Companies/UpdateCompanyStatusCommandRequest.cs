using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record UpdateCompanyStatusCommandRequest(
    Guid ActorUserUuid,
    Guid CompanyUuid,
    CompanyStatusUpdate Status) : ICommand<UpdateCompanyStatusCommandResponse>;
