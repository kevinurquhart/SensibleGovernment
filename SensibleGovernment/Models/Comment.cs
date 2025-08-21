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
}