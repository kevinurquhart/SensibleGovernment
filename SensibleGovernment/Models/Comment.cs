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
}