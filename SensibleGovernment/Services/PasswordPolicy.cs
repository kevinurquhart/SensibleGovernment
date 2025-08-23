// Create a new file: Services/PasswordPolicy.cs

using System.Text.RegularExpressions;

namespace SensibleGovernment.Services;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 100;
    public const bool RequireUppercase = true;
    public const bool RequireLowercase = true;
    public const bool RequireDigit = true;
    public const bool RequireSpecialCharacter = false; // Currently not required

    public static (bool IsValid, List<string> Errors) Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required");
            return (false, errors);
        }

        if (password.Length < MinimumLength)
            errors.Add($"Password must be at least {MinimumLength} characters long");

        if (password.Length > MaximumLength)
            errors.Add($"Password must not exceed {MaximumLength} characters");

        if (RequireUppercase && !password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");

        if (RequireLowercase && !password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");

        if (RequireDigit && !password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number");

        if (RequireSpecialCharacter && !Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]"))
            errors.Add("Password must contain at least one special character");

        // Check for common passwords
        if (IsCommonPassword(password))
            errors.Add("This password is too common. Please choose a more unique password");

        return (errors.Count == 0, errors);
    }

    private static bool IsCommonPassword(string password)
    {
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "12345678", "123456789", "qwerty", "abc123", "password123",
            "admin", "letmein", "welcome", "monkey", "dragon", "master", "superman"
        };

        return commonPasswords.Contains(password) ||
               commonPasswords.Any(common => password.ToLower().Contains(common));
    }

    public static string GetPolicyDescription()
    {
        return $"Password must be {MinimumLength}-{MaximumLength} characters and contain uppercase, lowercase, and numbers.";
    }
}