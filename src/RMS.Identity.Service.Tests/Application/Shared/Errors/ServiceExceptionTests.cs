using RMS.Identity.Service.Application.Shared.Errors;

namespace RMS.Identity.Service.Tests.Application.Shared.Errors;

public sealed class ServiceExceptionTests
{
    [Fact]
    public void Constructor_WithStructuredError_FormatsResponseCode()
    {
        var error = new ServiceError(new ServiceErrorCode(7, 3), "Something failed.");

        var exception = new ConflictException(error, null);

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("409-7-3", exception.Code);
        Assert.Equal("Something failed.", exception.Message);
        Assert.Same(error, exception.Error);
        Assert.Equal(new ServiceErrorCode(7, 3), exception.Error.Code);
    }

    [Fact]
    public void Constructor_WithStructuredCodeAndMessage_FormatsResponseCode()
    {
        var error = new ServiceError(new ServiceErrorCode(1, 2), "Failed to do something.");
        var exception = new InternalServerErrorException(error, null);

        Assert.Equal(500, exception.StatusCode);
        Assert.Equal("500-1-2", exception.Code);
        Assert.Equal("Failed to do something.", exception.Message);
        Assert.Same(error, exception.Error);
        Assert.Equal(new ServiceErrorCode(1, 2), exception.Error.Code);
    }

    [Fact]
    public void Constructor_WithMessageOnlyError_KeepsMessageAndStatus()
    {
        var exception = new ResourceNotFoundException("User could not be found.");

        Assert.Equal(404, exception.StatusCode);
        Assert.Equal("404", exception.Code);
        Assert.Equal("User could not be found.", exception.Message);
        Assert.NotNull(exception.Error);
        Assert.Null(exception.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithBlankMessage_Throws(string message)
    {
        Assert.Throws<ArgumentException>(() =>
            new InternalServerErrorException(message));
    }

    [Fact]
    public void Constructor_WithInvalidGroup_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ServiceErrorCode(0, 1));
    }

    [Fact]
    public void ServiceErrorDefinitions_WithCatalogEntry_FormatsResponseCode()
    {
        var exception = new ResourceNotFoundException(ServiceErrorDefinitions.Users.UserNotFound, null);

        Assert.Equal("404-3-1", exception.Code);
    }
}
