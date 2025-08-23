using System.Data;
using System.Data.SqlClient;
using SensibleGovernment.DataLayer.Models;
using SensibleGovernment.Models;

namespace SensibleGovernment.DataLayer.DataAccess;

public class PostDataAccess
{
    private readonly SQLConnection _sql;
    private readonly ILogger<PostDataAccess> _logger;

    public PostDataAccess(SQLConnection sql, ILogger<PostDataAccess> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    public async Task<List<Post>> GetAllPostsAsync()
    {
        try
        {
            var dataSet = await _sql.ExecuteDataSetAsync(StoredProcedures.GetAllPosts);

            // Expecting 4 tables: Posts, Comments, Likes, Sources
            var posts = ConvertToPostList(dataSet.Tables[0]);
            var comments = ConvertToCommentList(dataSet.Tables[1]);
            var likes = ConvertToLikeList(dataSet.Tables[2]);
            var sources = ConvertToSourceList(dataSet.Tables[3]);

            // Map relationships
            MapRelationships(posts, comments, likes, sources);

            return posts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all posts");
            throw;
        }
    }

    public async Task<Post?> GetPostByIdAsync(int id)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@PostId", id)
            };

            var dataSet = await _sql.ExecuteDataSetAsync(StoredProcedures.GetPostById, parameters);

            if (dataSet.Tables[0].Rows.Count == 0)
                return null;

            var posts = ConvertToPostList(dataSet.Tables[0]);
            var comments = ConvertToCommentList(dataSet.Tables[1]);
            var likes = ConvertToLikeList(dataSet.Tables[2]);
            var sources = ConvertToSourceList(dataSet.Tables[3]);

            MapRelationships(posts, comments, likes, sources);

            return posts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post by id: {PostId}", id);
            throw;
        }
    }

    public async Task<List<Post>> GetPostsByTopicAsync(string topic)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@Topic", topic)
            };

            var dataSet = await _sql.ExecuteDataSetAsync(StoredProcedures.GetPostsByTopic, parameters);

            var posts = ConvertToPostList(dataSet.Tables[0]);
            var comments = ConvertToCommentList(dataSet.Tables[1]);
            var likes = ConvertToLikeList(dataSet.Tables[2]);
            var sources = ConvertToSourceList(dataSet.Tables[3]);

            MapRelationships(posts, comments, likes, sources);

            return posts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts by topic: {Topic}", topic);
            throw;
        }
    }

    public async Task<int> CreatePostAsync(Post post)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@Title", post.Title),
                SQLConnection.CreateParameter("@Content", post.Content),
                SQLConnection.CreateParameter("@Opinion", post.Opinion),
                SQLConnection.CreateParameter("@FeaturedImageUrl", post.FeaturedImageUrl),
                SQLConnection.CreateParameter("@ThumbnailImageUrl", post.ThumbnailImageUrl),
                SQLConnection.CreateParameter("@ImageCaption", post.ImageCaption),
                SQLConnection.CreateParameter("@Topic", post.Topic),
                SQLConnection.CreateParameter("@AuthorId", post.AuthorId),
                SQLConnection.CreateParameter("@IsPublished", post.IsPublished),
                SQLConnection.CreateParameter("@IsFeatured", post.IsFeatured),
                SQLConnection.CreateOutputParameter("@NewPostId", SqlDbType.Int)
            };

            var result = await _sql.ExecuteWithOutputParametersAsync(StoredProcedures.CreatePost, parameters);
            var newPostId = (int)result["@NewPostId"]!;

            // Create sources if any
            if (post.Sources?.Any() == true)
            {
                foreach (var source in post.Sources)
                {
                    await CreatePostSourceAsync(newPostId, source);
                }
            }

            return newPostId;
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
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@PostId", post.Id),
                SQLConnection.CreateParameter("@Title", post.Title),
                SQLConnection.CreateParameter("@Content", post.Content),
                SQLConnection.CreateParameter("@Opinion", post.Opinion),
                SQLConnection.CreateParameter("@FeaturedImageUrl", post.FeaturedImageUrl),
                SQLConnection.CreateParameter("@ThumbnailImageUrl", post.ThumbnailImageUrl),
                SQLConnection.CreateParameter("@ImageCaption", post.ImageCaption),
                SQLConnection.CreateParameter("@Topic", post.Topic),
                SQLConnection.CreateParameter("@IsPublished", post.IsPublished),
                SQLConnection.CreateParameter("@IsFeatured", post.IsFeatured)
            };

            var rowsAffected = await _sql.ExecuteNonQueryAsync(StoredProcedures.UpdatePost, parameters);

            // Update sources - delete and recreate
            await DeletePostSourcesAsync(post.Id);
            if (post.Sources?.Any() == true)
            {
                foreach (var source in post.Sources)
                {
                    await CreatePostSourceAsync(post.Id, source);
                }
            }

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post: {PostId}", post.Id);
            throw;
        }
    }

    public async Task<bool> DeletePostAsync(int id)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@PostId", id)
            };

            var rowsAffected = await _sql.ExecuteNonQueryAsync(StoredProcedures.DeletePost, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post: {PostId}", id);
            throw;
        }
    }

    public async Task IncrementViewCountAsync(int postId)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@PostId", postId)
            };

            await _sql.ExecuteNonQueryAsync(StoredProcedures.IncrementPostViewCount, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing view count for post: {PostId}", postId);
            throw;
        }
    }

    public async Task<bool> ToggleLikeAsync(int postId, int userId)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@PostId", postId),
                SQLConnection.CreateParameter("@UserId", userId),
                SQLConnection.CreateOutputParameter("@IsLiked", SqlDbType.Bit)
            };

            var result = await _sql.ExecuteWithOutputParametersAsync(StoredProcedures.ToggleLike, parameters);
            return (bool)result["@IsLiked"]!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling like for post: {PostId}, user: {UserId}", postId, userId);
            throw;
        }
    }

    // Helper methods
    private async Task CreatePostSourceAsync(int postId, PostSource source)
    {
        var parameters = new[]
        {
            SQLConnection.CreateParameter("@PostId", postId),
            SQLConnection.CreateParameter("@Title", source.Title),
            SQLConnection.CreateParameter("@Url", source.Url),
            SQLConnection.CreateParameter("@Description", source.Description)
        };

        await _sql.ExecuteNonQueryAsync(StoredProcedures.CreatePostSource, parameters);
    }

    private async Task DeletePostSourcesAsync(int postId)
    {
        var parameters = new[]
        {
            SQLConnection.CreateParameter("@PostId", postId)
        };

        await _sql.ExecuteNonQueryAsync(StoredProcedures.DeletePostSources, parameters);
    }

    private List<Post> ConvertToPostList(DataTable dt)
    {
        var posts = new List<Post>();

        foreach (DataRow row in dt.Rows)
        {
            var post = new Post
            {
                Id = Convert.ToInt32(row["Id"]),
                Title = row["Title"].ToString() ?? "",
                Content = row["Content"].ToString() ?? "",
                Opinion = row["Opinion"] as string,
                FeaturedImageUrl = row["FeaturedImageUrl"] as string,
                ThumbnailImageUrl = row["ThumbnailImageUrl"] as string,
                ImageCaption = row["ImageCaption"] as string,
                Created = Convert.ToDateTime(row["Created"]),
                Updated = row["Updated"] as DateTime?,
                Topic = row["Topic"].ToString() ?? "",
                AuthorId = Convert.ToInt32(row["AuthorId"]),
                IsPublished = Convert.ToBoolean(row["IsPublished"]),
                IsFeatured = Convert.ToBoolean(row["IsFeatured"]),
                ViewCount = Convert.ToInt32(row["ViewCount"]),
                Comments = new List<Comment>(),
                Likes = new List<Like>(),
                Sources = new List<PostSource>()
            };

            // If author info is included
            if (dt.Columns.Contains("AuthorUserName"))
            {
                post.Author = new User
                {
                    Id = post.AuthorId,
                    UserName = row["AuthorUserName"].ToString() ?? "",
                    Email = row["AuthorEmail"].ToString() ?? ""
                };
            }

            posts.Add(post);
        }

        return posts;
    }

    private List<Comment> ConvertToCommentList(DataTable dt)
    {
        var comments = new List<Comment>();

        foreach (DataRow row in dt.Rows)
        {
            var comment = new Comment
            {
                Id = Convert.ToInt32(row["Id"]),
                Content = row["Content"].ToString() ?? "",
                Created = Convert.ToDateTime(row["Created"]),
                PostId = Convert.ToInt32(row["PostId"]),
                AuthorId = Convert.ToInt32(row["AuthorId"])
            };

            // If author info is included
            if (dt.Columns.Contains("AuthorUserName"))
            {
                comment.Author = new User
                {
                    Id = comment.AuthorId,
                    UserName = row["AuthorUserName"].ToString() ?? "",
                    IsAdmin = dt.Columns.Contains("AuthorIsAdmin") && Convert.ToBoolean(row["AuthorIsAdmin"])
                };
            }

            comments.Add(comment);
        }

        return comments;
    }

    private List<Like> ConvertToLikeList(DataTable dt)
    {
        var likes = new List<Like>();

        foreach (DataRow row in dt.Rows)
        {
            likes.Add(new Like
            {
                Id = Convert.ToInt32(row["Id"]),
                PostId = Convert.ToInt32(row["PostId"]),
                UserId = Convert.ToInt32(row["UserId"])
            });
        }

        return likes;
    }

    private List<PostSource> ConvertToSourceList(DataTable dt)
    {
        var sources = new List<PostSource>();

        foreach (DataRow row in dt.Rows)
        {
            sources.Add(new PostSource
            {
                Id = Convert.ToInt32(row["Id"]),
                PostId = Convert.ToInt32(row["PostId"]),
                Title = row["Title"].ToString() ?? "",
                Url = row["Url"].ToString() ?? "",
                Description = row["Description"] as string
            });
        }

        return sources;
    }

    private void MapRelationships(List<Post> posts, List<Comment> comments, List<Like> likes, List<PostSource> sources)
    {
        foreach (var post in posts)
        {
            post.Comments = comments.Where(c => c.PostId == post.Id).ToList();
            post.Likes = likes.Where(l => l.PostId == post.Id).ToList();
            post.Sources = sources.Where(s => s.PostId == post.Id).ToList();
        }
    }
}