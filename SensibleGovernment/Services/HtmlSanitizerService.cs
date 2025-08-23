using System.Text.RegularExpressions;
using System.Web;

namespace SensibleGovernment.Services;

public class HtmlSanitizerService
{
    private readonly ILogger<HtmlSanitizerService> _logger;

    // Whitelist of allowed tags for comments
    private readonly HashSet<string> _allowedTags = new()
    {
        "b", "i", "u", "strong", "em", "p", "br", "blockquote", "code", "pre"
    };

    // Regex patterns for dangerous content
    private readonly Regex _scriptPattern = new(@"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", RegexOptions.IgnoreCase);
    private readonly Regex _onEventPattern = new(@"\s*on\w+\s*=", RegexOptions.IgnoreCase);
    private readonly Regex _javascriptProtocolPattern = new(@"javascript\s*:", RegexOptions.IgnoreCase);
    private readonly Regex _dataUriPattern = new(@"data:[^,]*script", RegexOptions.IgnoreCase);

    public HtmlSanitizerService(ILogger<HtmlSanitizerService> logger)
    {
        _logger = logger;
    }

    public string SanitizeHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        try
        {
            // First, encode everything
            string sanitized = HttpUtility.HtmlEncode(input);

            // Then selectively decode allowed tags
            foreach (var tag in _allowedTags)
            {
                // Decode opening tags
                sanitized = sanitized.Replace($"&lt;{tag}&gt;", $"<{tag}>");
                sanitized = sanitized.Replace($"&lt;{tag.ToUpper()}&gt;", $"<{tag}>");

                // Decode closing tags
                sanitized = sanitized.Replace($"&lt;/{tag}&gt;", $"</{tag}>");
                sanitized = sanitized.Replace($"&lt;/{tag.ToUpper()}&gt;", $"</{tag}>");
            }

            // Convert line breaks to <br>
            sanitized = sanitized.Replace("\n", "<br>");

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing HTML");
            // On error, return fully encoded version for safety
            return HttpUtility.HtmlEncode(input);
        }
    }

    public string SanitizeForDatabase(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove any potential SQL injection attempts
        string sanitized = input;

        // Remove dangerous patterns
        sanitized = _scriptPattern.Replace(sanitized, "");
        sanitized = _onEventPattern.Replace(sanitized, "");
        sanitized = _javascriptProtocolPattern.Replace(sanitized, "");
        sanitized = _dataUriPattern.Replace(sanitized, "");

        // Limit length to prevent overflow attacks
        if (sanitized.Length > 5000)
            sanitized = sanitized.Substring(0, 5000);

        return sanitized.Trim();
    }

    public bool ContainsDangerousContent(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return _scriptPattern.IsMatch(input) ||
               _onEventPattern.IsMatch(input) ||
               _javascriptProtocolPattern.IsMatch(input) ||
               _dataUriPattern.IsMatch(input) ||
               input.Contains("<?php", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("<%", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("eval(", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("expression(", StringComparison.OrdinalIgnoreCase);
    }
}