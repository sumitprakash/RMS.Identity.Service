using RMS.Identity.Service.Infrastructure.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record GetCompanyCommandRequest(
    Guid CompanyUuid) : ICommand<GetCompanyCommandResponse>;
