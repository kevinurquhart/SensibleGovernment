namespace SensibleGovernment.Models;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;  // Factual reporting
    public string? Opinion { get; set; }  // Editorial opinion/review
    public string? FeaturedImageUrl { get; set; }  // Main article image
    public string? ThumbnailImageUrl { get; set; }  // Smaller image for cards/lists
    public string? ImageCaption { get; set; }  // Image attribution/caption
    public string? SourceLinks { get; set; }  // JSON or delimited list of source URLs
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    public List<Comment> Comments { get; set; } = new();
    public List<Like> Likes { get; set; } = new();
    public List<PostSource> Sources { get; set; } = new();  // Structured source links
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false;  // For homepage featuring
    public int ViewCount { get; set; } = 0;  // Track popularity
}

public class PostSource
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post? Post { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
}