using MediatR;
using RMS.Identity.Service.Application.Queries;
using RMS.Identity.Service.Application.Repositories;

namespace RMS.Identity.Service.Application.Handlers
{
    public class GetRolesHandler : IRequestHandler<GetRolesQuery, List<Domain.Entities.Role>>
    {
        private readonly IUserRoleRepository _userRoleRepo;
        public GetRolesHandler(IUserRoleRepository userRoleRepo) => _userRoleRepo = userRoleRepo;

        public async Task<List<Domain.Entities.Role>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _userRoleRepo.GetRolesForUserAsync(request.UserId);
            return roles?.ToList() ?? new List<Domain.Entities.Role>();
        }
    }
}