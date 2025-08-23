using SensibleGovernment.DataLayer.DataAccess;
using SensibleGovernment.Models;

namespace SensibleGovernment.Services;

public class PostService
{
    private readonly PostDataAccess _postDataAccess;
    private readonly CommentDataAccess _commentDataAccess;
    private readonly HtmlSanitizerService _sanitizer;
    private readonly InputValidationService _validationService;
    private readonly ILogger<PostService> _logger;
    private readonly CommentModerationService _moderationService;

    public PostService(
        PostDataAccess postDataAccess,
        CommentDataAccess commentDataAccess,
        HtmlSanitizerService sanitizer,
        InputValidationService validationService,
        CommentModerationService moderationService, // Add this
        ILogger<PostService> logger)
    {
        _postDataAccess = postDataAccess;
        _commentDataAccess = commentDataAccess;
        _sanitizer = sanitizer;
        _validationService = validationService;
        _logger = logger;
        _moderationService = moderationService;
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
            // Sanitize post content and opinion
            post.Content = _sanitizer.SanitizeForDatabase(post.Content);
            if (!string.IsNullOrEmpty(post.Opinion))
            {
                post.Opinion = _sanitizer.SanitizeForDatabase(post.Opinion);
            }

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

    public async Task<Comment> AddCommentAsync(Comment comment, User author)
    {
        try
        {
            // Validate
            var validation = _validationService.ValidateComment(comment.Content);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.GetErrorString());
            }

            // Moderate
            var moderation = await _moderationService.ModerateCommentAsync(comment.Content, author);

            if (moderation.IsBlocked)
            {
                throw new InvalidOperationException(moderation.BlockReason ?? "Comment blocked");
            }

            // Apply moderated content
            comment.Content = _sanitizer.SanitizeForDatabase(moderation.ModeratedContent);
            comment.IsHidden = !moderation.IsVisible;
            comment.RequiresReview = moderation.RequiresReview;

            if (moderation.IsShadowBanned)
            {
                comment.IsHidden = false; // Store as not hidden, but filter in display
                comment.ModerationReason = "Shadow banned";
            }

            comment.Created = DateTime.Now;

            var commentId = await _commentDataAccess.CreateCommentAsync(comment);
            comment.Id = commentId;

            // Log if requires review
            if (moderation.RequiresReview)
            {
                _logger.LogWarning($"Comment {commentId} flagged for review: {moderation.FlagReason}");
            }

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