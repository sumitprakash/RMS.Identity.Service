using RMS.Identity.Service.Application.Identity.Internal;
using RMS.Identity.Service.Application.Identity.Requests;
using RMS.Identity.Service.Application.Identity.Results;
using RMS.Identity.Service.Domain.Exceptions;
using RMS.Identity.Service.Domain.Interfaces.Persistence;
using RMS.Identity.Service.Domain.Interfaces.System;

namespace RMS.Identity.Service.Application.Identity;

public class GetUserUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IClock _clock;

    public GetUserUseCase(
        IUserAccountRepository userAccountRepository,
        ICompanyRepository companyRepository,
        IRoleRepository roleRepository,
        IClock clock)
    {
        _userAccountRepository = userAccountRepository;
        _companyRepository = companyRepository;
        _roleRepository = roleRepository;
        _clock = clock;
    }

    public async Task<UserResult> ExecuteAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userAccountRepository.GetByUuidAsync(query.UserUuid, cancellationToken)
            ?? throw new NotFoundException("user_not_found", "user not found");

        if (query.RequestingCompanyUuid.HasValue)
        {
            if (user.CompanyID is null)
            {
                throw new NotFoundException("user_not_found", "user not found");
            }

            var company = await _companyRepository.GetByIdAsync(user.CompanyID.Value, cancellationToken);
            if (company?.CompanyUUID != query.RequestingCompanyUuid.Value)
            {
                throw new NotFoundException("user_not_found", "user not found");
            }
        }

        var roles = await _roleRepository.GetByUserIdAsync(user.UserID, cancellationToken);
        return UserMappings.ToResult(user, roles, _clock.UtcNow);
    }
}
