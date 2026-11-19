using JobFlow.Infrastructure.Services;
using Xunit;

namespace JobFlow.Tests.UnitTests;

public class TelemetrySanitizerTests
{
    private readonly TelemetrySanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_ShouldRedact_JwtToken()
    {
        var raw = "Authorization failed for token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.doNotLeakThisSignature!";
        var result = _sanitizer.Sanitize(raw);

        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", result);
        Assert.Contains("[REDACTED_JWT_TOKEN]", result);
    }

    [Fact]
    public void Sanitize_ShouldRedact_ConnectionStringCredentials()
    {
        var raw = "Failed to connect with Host=db.internal;Port=5432;Database=prod;Password=SuperSecretPass!;Username=admin";
        var result = _sanitizer.Sanitize(raw);

        Assert.DoesNotContain("SuperSecretPass!", result);
        Assert.Contains("Password=[REDACTED]", result);
    }

    [Fact]
    public void Sanitize_ShouldRedact_OpenAiApiKey()
    {
        var raw = "API call failed with bearer sk-abcdef12345678901234567890123456";
        var result = _sanitizer.Sanitize(raw);

        Assert.DoesNotContain("sk-abcdef12345678901234567890123456", result);
        Assert.Contains("[REDACTED_SECRET]", result);
    }

    [Fact]
    public void Sanitize_ShouldRedact_EmailAddress()
    {
        var raw = "Notification dispatched to support.team@enterprise.org successfully.";
        var result = _sanitizer.Sanitize(raw);

        Assert.DoesNotContain("support.team@enterprise.org", result);
        Assert.Contains("[REDACTED_EMAIL]", result);
    }

    [Fact]
    public void Sanitize_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        Assert.Equal(string.Empty, _sanitizer.Sanitize(string.Empty));
    }
}
