using SensibleGovernment.DataLayer.DataAccess;
using SensibleGovernment.Models;

namespace SensibleGovernment.Services;

public class PostService
{
    private readonly PostDataAccess _postDataAccess;
    private readonly CommentDataAccess _commentDataAccess;
    private readonly ILogger<PostService> _logger;

    public PostService(PostDataAccess postDataAccess, CommentDataAccess commentDataAccess, ILogger<PostService> logger)
    {
        _postDataAccess = postDataAccess;
        _commentDataAccess = commentDataAccess;
        _logger = logger;
    }

    public async Task<List<Post>> GetAllPostsAsync()
    {
        try
        {
            return await _postDataAccess.GetAllPostsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all posts");
            return new List<Post>();
        }
    }

    public async Task<List<Post>> GetPostsByTopicAsync(string topic)
    {
        try
        {
            return await _postDataAccess.GetPostsByTopicAsync(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts by topic: {Topic}", topic);
            return new List<Post>();
        }
    }

    public async Task<Post?> GetPostByIdAsync(int id)
    {
        try
        {
            var post = await _postDataAccess.GetPostByIdAsync(id);

            // Increment view count asynchronously
            if (post != null)
            {
                _ = Task.Run(async () => await _postDataAccess.IncrementViewCountAsync(id));
            }

            return post;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post by id: {PostId}", id);
            return null;
        }
    }

    public async Task<Post> CreatePostAsync(Post post)
    {
        try
        {
            var newPostId = await _postDataAccess.CreatePostAsync(post);
            post.Id = newPostId;

            // Reload the post to get all related data
            var createdPost = await _postDataAccess.GetPostByIdAsync(newPostId);
            return createdPost ?? post;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            throw;
        }
    }

    public async Task<bool> UpdatePostAsync(Post post)
    {
        try
        {
            return await _postDataAccess.UpdatePostAsync(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post: {PostId}", post.Id);
            return false;
        }
    }

    public async Task<bool> DeletePostAsync(int id)
    {
        try
        {
            return await _postDataAccess.DeletePostAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post: {PostId}", id);
            return false;
        }
    }

    public async Task<bool> ToggleLikeAsync(int postId, int userId)
    {
        try
        {
            return await _postDataAccess.ToggleLikeAsync(postId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling like for post: {PostId}, user: {UserId}", postId, userId);
            return false;
        }
    }

    public async Task<Comment> AddCommentAsync(Comment comment)
    {
        try
        {
            comment.Created = DateTime.Now;
            var commentId = await _commentDataAccess.CreateCommentAsync(comment);
            comment.Id = commentId;

            // Get the full comment with author info
            var comments = await _commentDataAccess.GetRecentCommentsAsync(1);
            return comments.FirstOrDefault(c => c.Id == commentId) ?? comment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            throw;
        }
    }

    public async Task<bool> DeleteCommentAsync(int id, int userId)
    {
        try
        {
            return await _commentDataAccess.DeleteCommentAsync(id, userId, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment: {CommentId}", id);
            return false;
        }
    }
}