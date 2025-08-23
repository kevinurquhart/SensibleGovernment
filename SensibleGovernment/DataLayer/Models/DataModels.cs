namespace SensibleGovernment.DataLayer.Models;

// These are simple DTOs for data transfer
// They map directly to database tables/stored procedure results

public class PostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Opinion { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? ThumbnailImageUrl { get; set; }
    public string? ImageCaption { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }

    // Joined fields from User table
    public string? AuthorUserName { get; set; }
    public string? AuthorEmail { get; set; }

    // Aggregated counts
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
}

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public int PostId { get; set; }
    public int AuthorId { get; set; }

    // Joined fields
    public string? AuthorUserName { get; set; }
    public string? AuthorEmail { get; set; }
    public bool AuthorIsAdmin { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LastFailedLogin { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; }
    public DateTime? LastLogin { get; set; }

    // Aggregated counts
    public int PostCount { get; set; }
    public int CommentCount { get; set; }
    public int ReportsAgainstCount { get; set; }
}

public class LikeDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
}

public class PostSourceDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UserReportDto
{
    public int Id { get; set; }
    public int ReportingUserId { get; set; }
    public int ReportedUserId { get; set; }
    public int? CommentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Created { get; set; }
    public bool IsResolved { get; set; }
    public string? Resolution { get; set; }

    // Joined fields
    public string? ReportingUserName { get; set; }
    public string? ReportedUserName { get; set; }
    public string? CommentContent { get; set; }
    public string? PostTitle { get; set; }
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int TotalComments { get; set; }
    public int PendingReports { get; set; }
    public int UsersRegisteredToday { get; set; }
    public int CommentsToday { get; set; }
    public int PostsToday { get; set; }
}