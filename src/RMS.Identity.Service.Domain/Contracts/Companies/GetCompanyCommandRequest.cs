using RMS.Identity.Service.Infrastructure.Abstractions.Cqrs;

namespace RMS.Identity.Service.Domain.Contracts.Companies;

public sealed record GetCompanyCommandRequest(
    Guid CompanyUuid) : ICommand<GetCompanyCommandResponse>;
