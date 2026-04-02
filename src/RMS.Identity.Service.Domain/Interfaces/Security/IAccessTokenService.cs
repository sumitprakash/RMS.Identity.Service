using RMS.Identity.Service.Domain.Models;

namespace RMS.Identity.Service.Domain.Interfaces.Security;

public interface IAccessTokenService
{
    AccessTokenResult Create(Guid userUuid, Guid? companyUuid, IReadOnlyCollection<string> roles);
}
