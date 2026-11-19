using System.Text.RegularExpressions;
using JobFlow.Core.Interfaces;

namespace JobFlow.Infrastructure.Services;

public class TelemetrySanitizer : ITelemetrySanitizer
{
    private static readonly Regex JwtRegex = new(
        @"\beyJ[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\b",
        RegexOptions.Compiled);

    private static readonly Regex ConnectionStringRegex = new(
        @"(?i)(User ID|Password|Pwd|Server|Host|Database)=[^;]+",
        RegexOptions.Compiled);

    private static readonly Regex ApiKeyRegex = new(
        @"(?i)(bearer\s+[a-zA-Z0-9_\-\.]+)|(sk-[a-zA-Z0-9]{20,})|(\b(api[_-]?key|secret|token)\s*[:=]\s*[""']?[^""'\r\n;]+)",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);

    public string Sanitize(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        var sanitized = JwtRegex.Replace(rawText, "[REDACTED_JWT_TOKEN]");
        sanitized = ConnectionStringRegex.Replace(sanitized, "$1=[REDACTED]");
        sanitized = ApiKeyRegex.Replace(sanitized, "[REDACTED_SECRET]");
        sanitized = EmailRegex.Replace(sanitized, "[REDACTED_EMAIL]");

        return sanitized;
    }
}
