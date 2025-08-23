using System.Text.RegularExpressions;

namespace SensibleGovernment.Services;

public class InputValidationService
{
    private readonly HtmlSanitizerService _sanitizer;
    private readonly ILogger<InputValidationService> _logger;

    // Validation patterns
    private readonly Regex _emailPattern = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    private readonly Regex _usernamePattern = new(@"^[a-zA-Z0-9_-]{3,50}$");
    private readonly Regex _urlPattern = new(@"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$");

    // Profanity list (basic - expand as needed)
    private readonly HashSet<string> _profanityList = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add your list here
    };

    public InputValidationService(HtmlSanitizerService sanitizer, ILogger<InputValidationService> logger)
    {
        _sanitizer = sanitizer;
        _logger = logger;
    }

    public ValidationResult ValidateComment(string? content, bool allowReplies = true)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(content))
        {
            result.AddError("Comment cannot be empty");
            return result;
        }

        if (content.Length < 2)
        {
            result.AddError("Comment is too short (minimum 2 characters)");
        }

        if (content.Length > 2000)
        {
            result.AddError("Comment is too long (maximum 2000 characters)");
        }

        if (_sanitizer.ContainsDangerousContent(content))
        {
            result.AddError("Comment contains potentially dangerous content");
            _logger.LogWarning("Dangerous content detected in comment");
        }

        // Check for spam patterns
        if (ContainsSpamPatterns(content))
        {
            result.AddError("Comment appears to contain spam");
        }

        // Check for excessive caps
        if (IsExcessiveCaps(content))
        {
            result.AddError("Please don't use excessive capital letters");
        }

        // Check for repeated characters
        if (HasExcessiveRepeatedCharacters(content))
        {
            result.AddError("Comment contains excessive repeated characters");
        }

        return result;
    }

    public ValidationResult ValidateEmail(string? email)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(email))
        {
            result.AddError("Email is required");
            return result;
        }

        if (!_emailPattern.IsMatch(email))
        {
            result.AddError("Invalid email format");
        }

        // Check for disposable email domains
        if (IsDisposableEmail(email))
        {
            result.AddError("Disposable email addresses are not allowed");
        }

        return result;
    }

    public ValidationResult ValidateUsername(string? username)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(username))
        {
            result.AddError("Username is required");
            return result;
        }

        if (!_usernamePattern.IsMatch(username))
        {
            result.AddError("Username must be 3-50 characters and contain only letters, numbers, underscores, and hyphens");
        }

        if (ContainsProfanity(username))
        {
            result.AddError("Username contains inappropriate content");
        }

        return result;
    }

    private bool ContainsSpamPatterns(string content)
    {
        var lowerContent = content.ToLower();

        // Common spam patterns
        var spamPatterns = new[]
        {
            "click here", "buy now", "free money", "work from home",
            "make money fast", "viagra", "casino", "lottery",
            "congratulations you won", "increase your"
        };

        // Check for multiple URLs
        var urlCount = Regex.Matches(content, @"https?://").Count;
        if (urlCount > 2) return true;

        return spamPatterns.Any(pattern => lowerContent.Contains(pattern));
    }

    private bool IsExcessiveCaps(string content)
    {
        if (content.Length < 10) return false;

        var capsCount = content.Count(char.IsUpper);
        var letterCount = content.Count(char.IsLetter);

        if (letterCount == 0) return false;

        return (double)capsCount / letterCount > 0.7;
    }

    private bool HasExcessiveRepeatedCharacters(string content)
    {
        // Check for patterns like "!!!!!!!" or "........"
        return Regex.IsMatch(content, @"(.)\1{5,}");
    }

    private bool ContainsProfanity(string content)
    {
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Any(word => _profanityList.Contains(word));
    }

    private bool IsDisposableEmail(string email)
    {
        var disposableDomains = new[]
        {
            "tempmail.com", "throwaway.email", "guerrillamail.com",
            "mailinator.com", "10minutemail.com", "trashmail.com"
        };

        var domain = email.Split('@').LastOrDefault()?.ToLower();
        return domain != null && disposableDomains.Any(d => domain.Contains(d));
    }
}

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; } = new();

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public string GetErrorString()
    {
        return string.Join("; ", Errors);
    }
}