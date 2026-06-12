using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Tests.Application.Shared.Errors;

public sealed class ServiceExceptionTests
{
    [Fact]
    public void Constructor_WithStructuredError_FormatsResponseCode()
    {
        var error = new ServiceError(new ServiceErrorCode(7, 3), "Something failed.");

        var exception = new ServiceException(ServiceStatusErrorCodes.Conflict, error);

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("409-7-3", exception.Code);
        Assert.Equal("Something failed.", exception.Message);
        Assert.Equal(ServiceStatusErrorCodes.Conflict, exception.ExceptionType);
        Assert.Same(error, exception.Error);
        Assert.Equal(new ServiceErrorCode(7, 3), exception.ErrorCode);
        Assert.Equal(new ServiceErrorCode(7, 3), exception.StructuredCode);
    }

    [Fact]
    public void Constructor_WithStructuredCodeAndMessage_FormatsResponseCode()
    {
        var exception = new ServiceException(
            ServiceStatusErrorCodes.InternalServerError,
            new ServiceErrorCode(1, 2),
            "Failed to do something.");

        Assert.Equal(500, exception.StatusCode);
        Assert.Equal("500-1-2", exception.Code);
        Assert.Equal("Failed to do something.", exception.Message);
        Assert.Equal(ServiceStatusErrorCodes.InternalServerError, exception.ExceptionType);
        Assert.Null(exception.Error);
        Assert.Equal(new ServiceErrorCode(1, 2), exception.ErrorCode);
        Assert.Equal(new ServiceErrorCode(1, 2), exception.StructuredCode);
    }

    [Fact]
    public void Constructor_WithLegacyCode_KeepsLegacyResponseFields()
    {
        var exception = new ServiceException(404, "USER_NOT_FOUND", "User could not be found.");

        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("USER_NOT_FOUND", exception.Code);
        Assert.Equal("User could not be found.", exception.Message);
        Assert.Null(exception.ExceptionType);
        Assert.Null(exception.Error);
        Assert.Null(exception.ErrorCode);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void Constructor_WithInvalidStatusCode_Throws(int statusCode)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ServiceException(statusCode, "ERROR", "Something failed."));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithBlankCode_Throws(string code)
    {
        Assert.Throws<ArgumentException>(() =>
            new ServiceException(500, code, "Something failed."));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithBlankMessage_Throws(string message)
    {
        Assert.Throws<ArgumentException>(() =>
            new ServiceException(500, "ERROR", message));
    }

    [Fact]
    public void ToResponseCode_WithDefaultCode_Throws()
    {
        var code = default(ServiceErrorCode);

        Assert.Throws<InvalidOperationException>(() => code.ToResponseCode((int)ServiceStatusErrorCodes.InternalServerError));
    }

    [Fact]
    public void ServiceErrors_WithCatalogEntry_FormatsResponseCode()
    {
        var code = ServiceErrors.Users.UserNotFound.ToResponseCode((int)ServiceStatusErrorCodes.NotFound);

        Assert.Equal("404-3-1", code);
    }
}
