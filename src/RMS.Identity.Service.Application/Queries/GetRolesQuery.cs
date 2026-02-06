using MediatR;
using System.Collections.Generic;

namespace RMS.Identity.Service.Application.Queries
{
    public sealed class GetRolesQuery : IRequest<List<RMS.Identity.Service.Domain.Entities.Role>>
    {
        public long UserId { get; init; }
    }
}