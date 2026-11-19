using JobFlow.Core.Interfaces;
using JobFlow.Infrastructure.Services;
using Xunit;

namespace JobFlow.Tests.UnitTests;

public class AuthHashTests
{
    [Fact]
    public void VerifiedAdminHash_ValidatesCorrectly()
    {
        var validHash = "$2a$11$TKHys93kM2KIp9A57Cimh.jmBJKGCAKsDCC8XllMkKM/2o.SXzeoi";

        Assert.True(BCrypt.Net.BCrypt.Verify("AdminPassword123!", validHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("WrongPassword!", validHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("Admin123!", validHash));
    }

    [Fact]
    public void LegacyBogusHash_FailsVerification()
    {
        // Old placeholder hash that caused authentication failures
        var legacyPlaceholderHash = "$2a$11$0Ff8wZ8J9J/LqVv0pZ0rI.bZc0V0Wb8J9J/LqVv0pZ0rI.bZc0V0W";

        Assert.False(BCrypt.Net.BCrypt.Verify("AdminPassword123!", legacyPlaceholderHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("Admin123!", legacyPlaceholderHash));
    }

    [Fact]
    public void DatabaseReadinessService_TracksStatusCorrectly()
    {
        IDatabaseReadinessService readiness = new DatabaseReadinessService();

        Assert.False(readiness.IsReady);
        Assert.Equal("Initializing", readiness.Status);
        Assert.Null(readiness.FailureReason);

        readiness.SetFailed("Connection refused on port 5432");
        Assert.False(readiness.IsReady);
        Assert.Equal("Unhealthy", readiness.Status);
        Assert.Equal("Connection refused on port 5432", readiness.FailureReason);
        Assert.NotNull(readiness.InitializedAt);

        readiness.SetReady();
        Assert.True(readiness.IsReady);
        Assert.Equal("Ready", readiness.Status);
        Assert.Null(readiness.FailureReason);
    }
}
