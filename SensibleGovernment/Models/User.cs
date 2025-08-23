namespace SensibleGovernment.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Security fields
    public string PasswordHash { get; set; } = string.Empty;  // BCrypt hash
    public bool EmailConfirmed { get; set; } = false;
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationExpiry { get; set; }

    // Account lockout
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LastFailedLogin { get; set; }
    public DateTime? LockoutEnd { get; set; }

    // Two-factor authentication (for admins)
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    public string? RecoveryCode { get; set; }  // Backup code if 2FA device lost

    // Password management
    public DateTime? PasswordLastChanged { get; set; }
    public bool PasswordResetRequired { get; set; } = false;
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }

    // Admin and status
    public bool IsAdmin { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Tracking
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public string? LastLoginIp { get; set; }

    // Navigation properties
    public List<Post> Posts { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<Like> Likes { get; set; } = new();
    public List<UserReport> ReportsSubmitted { get; set; } = new();
    public List<UserReport> ReportsAgainst { get; set; } = new();
    public List<AdminActionLog> AdminActions { get; set; } = new();

    // Shadow ban property
    public bool IsShadowBanned { get; set; } = false;
    public DateTime? ShadowBannedUntil { get; set; }
    public string? ShadowBanReason { get; set; }
}

// Keep the existing UserReport model
public class UserReport
{
    public int Id { get; set; }
    public int ReportingUserId { get; set; }
    public User? ReportingUser { get; set; }
    public int ReportedUserId { get; set; }
    public User? ReportedUser { get; set; }
    public int? CommentId { get; set; }
    public Comment? Comment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public bool IsResolved { get; set; } = false;
    public string? Resolution { get; set; }
}

// New model for tracking admin actions
public class AdminActionLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}