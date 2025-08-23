namespace SensibleGovernment.Models;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public int PostId { get; set; }
    public Post? Post { get; set; }
    public int AuthorId { get; set; }
    public User? Author { get; set; }

    // Add reply support
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public List<Comment> Replies { get; set; } = new();

    // Add moderation properties
    public bool IsHidden { get; set; } = false;
    public bool RequiresReview { get; set; } = false;
    public string? ModerationReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByUserId { get; set; }

    // For tracking reports against this comment
    public int ReportCount { get; set; } = 0;
}