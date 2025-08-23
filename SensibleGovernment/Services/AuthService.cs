using Microsoft.EntityFrameworkCore;
using SensibleGovernment.Data;
using SensibleGovernment.Models;
using BCrypt.Net;
using System.Security.Cryptography;

namespace SensibleGovernment.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private User? _currentUser;

    // Security settings
    private const int MAX_LOGIN_ATTEMPTS = 5;
    private const int LOCKOUT_DURATION_MINUTES = 15;
    private const int SESSION_TIMEOUT_MINUTES = 30;
    private const int MIN_PASSWORD_LENGTH = 8;

    // Track failed login attempts (in production, use distributed cache)
    private static readonly Dictionary<string, LoginAttemptInfo> _loginAttempts = new();

    public AuthService(AppDbContext context, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
    public event Action? OnAuthStateChanged;

    public async Task<(bool Success, string Message, bool RequiresCaptcha)> RegisterAsync(
        string userName, string email, string password, string? confirmPassword = null)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "All fields are required", false);
            }

            // Validate password strength
            var passwordValidation = ValidatePasswordStrength(password);
            if (!passwordValidation.IsValid)
            {
                return (false, passwordValidation.Message, false);
            }

            // Check password confirmation if provided
            if (confirmPassword != null && password != confirmPassword)
            {
                return (false, "Passwords do not match", false);
            }

            // Normalize email
            email = email.ToLowerInvariant().Trim();

            // Check for existing user
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email || u.UserName == userName);

            if (existingUser != null)
            {
                // Don't reveal which field is duplicate (security)
                _logger.LogWarning("Registration attempt with existing credentials: {Email}", email);
                return (false, "An account with these details already exists", false);
            }

            // Hash the password using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));

            // Create new user
            var user = new User
            {
                UserName = userName.Trim(),
                Email = email,
                PasswordHash = passwordHash,
                Created = DateTime.UtcNow,
                IsActive = true,
                IsAdmin = false,
                EmailConfirmed = false,
                TwoFactorEnabled = false,
                FailedLoginAttempts = 0,
                LastFailedLogin = null,
                LockoutEnd = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {UserName} ({Email})", userName, email);

            // Auto-login after registration
            _currentUser = user;
            await UpdateSessionAsync(user);
            OnAuthStateChanged?.Invoke();

            return (true, "Registration successful! Welcome to The Sensible Citizen.", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", email);
            return (false, "An error occurred during registration. Please try again.", false);
        }
    }

    public async Task<(bool Success, string Message, bool RequiresCaptcha)> LoginAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Email and password are required", false);
            }

            email = email.ToLowerInvariant().Trim();
            var ipAddress = GetClientIpAddress();

            // Check for IP-based lockout
            if (IsIpLockedOut(ipAddress))
            {
                _logger.LogWarning("Login attempt from locked IP: {IP}", ipAddress);
                return (false, "Too many failed attempts. Please try again later.", true);
            }

            // Get user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Track failed attempt even for non-existent users (prevent enumeration)
                await TrackFailedLoginAsync(email, ipAddress);
                _logger.LogWarning("Login attempt for non-existent user: {Email}", email);
                return (false, "Invalid email or password", ShouldRequireCaptcha(ipAddress));
            }

            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remainingTime = (user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
                _logger.LogWarning("Login attempt for locked account: {Email}", email);
                return (false, $"Account is locked. Try again in {Math.Ceiling(remainingTime)} minutes.", true);
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for suspended account: {Email}", email);
                return (false, "Your account has been suspended. Please contact support.", false);
            }

            // Verify password
            bool passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!passwordValid)
            {
                await TrackFailedLoginAsync(email, ipAddress, user);

                // Check if should lock account
                if (user.FailedLoginAttempts >= MAX_LOGIN_ATTEMPTS)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Account locked due to failed attempts: {Email}", email);
                    return (false, $"Account locked due to multiple failed attempts. Try again in {LOCKOUT_DURATION_MINUTES} minutes.", true);
                }

                var remainingAttempts = MAX_LOGIN_ATTEMPTS - user.FailedLoginAttempts;
                return (false, $"Invalid email or password. {remainingAttempts} attempts remaining.", ShouldRequireCaptcha(ipAddress));
            }

            // Check if 2FA is required for admin accounts
            if (user.IsAdmin && user.TwoFactorEnabled)
            {
                // In production, implement proper 2FA flow
                _logger.LogInformation("2FA required for admin user: {Email}", email);
                // For now, we'll proceed but log it
            }

            // Successful login - reset failed attempts
            user.FailedLoginAttempts = 0;
            user.LastFailedLogin = null;
            user.LockoutEnd = null;
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Set current user and session
            _currentUser = user;
            await UpdateSessionAsync(user);
            OnAuthStateChanged?.Invoke();

            // Clear IP-based tracking
            ClearFailedAttempts(ipAddress);

            _logger.LogInformation("Successful login: {Email} from IP: {IP}", email, ipAddress);

            // Log admin logins specially
            if (user.IsAdmin)
            {
                await LogAdminActionAsync(user.Id, "Admin Login", $"IP: {ipAddress}");
            }

            return (true, "Login successful", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", email);
            return (false, "An error occurred during login. Please try again.", false);
        }
    }

    public void Logout()
    {
        if (_currentUser != null)
        {
            _logger.LogInformation("User logged out: {Email}", _currentUser.Email);

            if (_currentUser.IsAdmin)
            {
                // Fire and forget the admin log - don't await
                Task.Run(async () => await LogAdminActionAsync(_currentUser.Id, "Admin Logout", ""));
            }
        }

        _currentUser = null;
        ClearSession();
        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Failed password change attempt for user: {UserId}", userId);
                return false;
            }

            // Validate new password
            var validation = ValidatePasswordStrength(newPassword);
            if (!validation.IsValid) return false;

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
            user.PasswordLastChanged = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<string> GenerateTwoFactorSecretAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsAdmin) return string.Empty;

        // Generate a random secret for 2FA (in production, use a proper TOTP library)
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.TwoFactorSecret = secret;
        user.TwoFactorEnabled = false; // Not enabled until confirmed
        await _context.SaveChangesAsync();

        return secret;
    }

    public async Task<bool> EnableTwoFactorAsync(int userId, string verificationCode)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret)) return false;

        // In production, verify the TOTP code here
        // For now, we'll just enable it
        user.TwoFactorEnabled = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("2FA enabled for user: {UserId}", userId);
        return true;
    }

    // Helper methods
    private (bool IsValid, string Message) ValidatePasswordStrength(string password)
    {
        if (password.Length < MIN_PASSWORD_LENGTH)
            return (false, $"Password must be at least {MIN_PASSWORD_LENGTH} characters long");

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasNumber = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        if (!hasUpper || !hasLower || !hasNumber)
            return (false, "Password must contain uppercase, lowercase, and numbers");

        // Check for common passwords (in production, use a comprehensive list)
        var commonPasswords = new[] { "password", "12345678", "qwerty", "admin", "letmein" };
        if (commonPasswords.Any(common => password.ToLower().Contains(common)))
            return (false, "Password is too common. Please choose a stronger password");

        return (true, "Password is strong");
    }

    private async Task TrackFailedLoginAsync(string email, string ipAddress, User? user = null)
    {
        // Track by IP
        if (!_loginAttempts.ContainsKey(ipAddress))
        {
            _loginAttempts[ipAddress] = new LoginAttemptInfo();
        }

        _loginAttempts[ipAddress].FailedAttempts++;
        _loginAttempts[ipAddress].LastAttempt = DateTime.UtcNow;

        // Track by user account
        if (user != null)
        {
            user.FailedLoginAttempts++;
            user.LastFailedLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        _logger.LogWarning("Failed login tracked - Email: {Email}, IP: {IP}", email, ipAddress);
    }

    private bool IsIpLockedOut(string ipAddress)
    {
        if (!_loginAttempts.ContainsKey(ipAddress)) return false;

        var attempts = _loginAttempts[ipAddress];

        // Clean up old attempts
        if (attempts.LastAttempt < DateTime.UtcNow.AddMinutes(-LOCKOUT_DURATION_MINUTES))
        {
            _loginAttempts.Remove(ipAddress);
            return false;
        }

        return attempts.FailedAttempts >= MAX_LOGIN_ATTEMPTS;
    }

    private bool ShouldRequireCaptcha(string ipAddress)
    {
        if (!_loginAttempts.ContainsKey(ipAddress)) return false;
        return _loginAttempts[ipAddress].FailedAttempts >= 3;
    }

    private void ClearFailedAttempts(string ipAddress)
    {
        if (_loginAttempts.ContainsKey(ipAddress))
        {
            _loginAttempts.Remove(ipAddress);
        }
    }

    private string GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return "Unknown";

        // Check for proxy headers
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private async Task UpdateSessionAsync(User user)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Store session info (in production, use proper session management)
        httpContext.Session.SetString("UserId", user.Id.ToString());
        httpContext.Session.SetString("UserName", user.UserName);
        httpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());
        httpContext.Session.SetString("SessionStart", DateTime.UtcNow.ToString());
    }

    private void ClearSession()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        httpContext?.Session.Clear();
    }

    public async Task<bool> ValidateSessionAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return false;

        var sessionStart = httpContext.Session.GetString("SessionStart");
        if (string.IsNullOrEmpty(sessionStart)) return false;

        if (DateTime.TryParse(sessionStart, out var startTime))
        {
            if (DateTime.UtcNow - startTime > TimeSpan.FromMinutes(SESSION_TIMEOUT_MINUTES))
            {
                Logout();
                return false;
            }
        }

        return true;
    }

    private async Task LogAdminActionAsync(int userId, string action, string details)
    {
        var log = new AdminActionLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            IpAddress = GetClientIpAddress(),
            Timestamp = DateTime.UtcNow
        };

        _context.AdminActionLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    // Inner class for tracking login attempts
    private class LoginAttemptInfo
    {
        public int FailedAttempts { get; set; }
        public DateTime LastAttempt { get; set; }
    }
}