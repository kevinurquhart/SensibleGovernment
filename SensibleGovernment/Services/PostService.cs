using Microsoft.EntityFrameworkCore;
using SensibleGovernment.Data;
using SensibleGovernment.Models;

namespace SensibleGovernment.Services;

public class PostService
{
    private readonly AppDbContext _context;

    public PostService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Post>> GetAllPostsAsync()
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Likes)
            .Include(p => p.Sources)
            .OrderByDescending(p => p.Created)
            .ToListAsync();
    }

    public async Task<List<Post>> GetPostsByTopicAsync(string topic)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Likes)
            .Where(p => p.Topic == topic)
            .OrderByDescending(p => p.Created)
            .ToListAsync();
    }

    public async Task<Post?> GetPostByIdAsync(int id)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Likes)
                .ThenInclude(l => l.User)
            .Include(p => p.Sources)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Post> CreatePostAsync(Post post)
    {
        post.Created = DateTime.Now;
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<bool> DeletePostAsync(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null) return false;

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleLikeAsync(int postId, int userId)
    {
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existingLike != null)
        {
            _context.Likes.Remove(existingLike);
            await _context.SaveChangesAsync();
            return false; // Unlike
        }
        else
        {
            var like = new Like { PostId = postId, UserId = userId };
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return true; // Like
        }
    }

    public async Task<Comment> AddCommentAsync(Comment comment)
    {
        comment.Created = DateTime.Now;
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Reload with author info
        return await _context.Comments
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id);
    }

    public async Task<bool> DeleteCommentAsync(int id, int userId)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null || comment.AuthorId != userId) return false;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }
}