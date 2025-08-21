namespace SensibleGovernment.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime? LastLogin { get; set; }
    public List<Post> Posts { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<Like> Likes { get; set; } = new();
    public List<UserReport> ReportsSubmitted { get; set; } = new();
    public List<UserReport> ReportsAgainst { get; set; } = new();
}

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