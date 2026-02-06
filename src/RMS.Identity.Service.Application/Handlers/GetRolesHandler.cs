using MediatR;
using RMS.Identity.Service.Application.Queries;
using RMS.Identity.Service.Infrastructure.Repositories.Implementation;

namespace RMS.Identity.Service.Application.Handlers
{
    public class GetRolesHandler : IRequestHandler<GetRolesQuery, List<Domain.Entities.Role>>
    {
        private readonly UserRoleRepository _userRoleRepo;
        public GetRolesHandler(UserRoleRepository userRoleRepo) => _userRoleRepo = userRoleRepo;

        public Task<List<Domain.Entities.Role>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
            => _userRoleRepo.GetRolesForUserAsync(request.UserId) ?? Task.FromResult(new List<Domain.Entities.Role>());
    }
}