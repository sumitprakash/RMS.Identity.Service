using RMS.Identity.Service.Application.Identity.Requests;
using RMS.Identity.Service.Application.Identity.Results;
using RMS.Identity.Service.Domain.Exceptions;
using RMS.Identity.Service.Domain.Interfaces.Persistence;

namespace RMS.Identity.Service.Application.Identity;

public class GetCompanyUseCase
{
    private readonly ICompanyRepository _companyRepository;

    public GetCompanyUseCase(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<CompanyResult> ExecuteAsync(GetCompanyQuery query, CancellationToken cancellationToken = default)
    {
        if (query.RequestingCompanyUuid.HasValue && query.RequestingCompanyUuid.Value != query.CompanyUuid)
        {
            throw new NotFoundException("company_not_found", "company not found");
        }

        var company = await _companyRepository.GetByUuidAsync(query.CompanyUuid, cancellationToken)
            ?? throw new NotFoundException("company_not_found", "company not found");

        return new CompanyResult(
            company.CompanyUUID,
            company.CompanyCode,
            company.CompanyName,
            company.CompanyGSTIN);
    }
}
