using System.Net;

namespace RMS.Identity.Service.Application.Shared.Errors;

public enum ServiceStatusErrorCodes
{
    BadRequest = HttpStatusCode.BadRequest,
    Unauthorized = HttpStatusCode.Unauthorized,
    Forbidden = HttpStatusCode.Forbidden,
    NotFound = HttpStatusCode.NotFound,
    Conflict = HttpStatusCode.Conflict,
    InternalServerError = HttpStatusCode.InternalServerError
}
