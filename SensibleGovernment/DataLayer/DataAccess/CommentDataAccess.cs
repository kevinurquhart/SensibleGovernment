using System.Data;
using System.Data.SqlClient;
using SensibleGovernment.Models;

namespace SensibleGovernment.DataLayer.DataAccess;

public class CommentDataAccess
{
    private readonly SQLConnection _sql;
    private readonly ILogger<CommentDataAccess> _logger;

    public CommentDataAccess(SQLConnection sql, ILogger<CommentDataAccess> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    public async Task<int> CreateCommentAsync(Comment comment)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@Content", comment.Content),
                SQLConnection.CreateParameter("@PostId", comment.PostId),
                SQLConnection.CreateParameter("@AuthorId", comment.AuthorId),
                SQLConnection.CreateOutputParameter("@NewCommentId", SqlDbType.Int)
            };

            var result = await _sql.ExecuteWithOutputParametersAsync(StoredProcedures.CreateComment, parameters);
            return (int)result["@NewCommentId"]!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment");
            throw;
        }
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId, bool isAdmin)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@CommentId", commentId),
                SQLConnection.CreateParameter("@UserId", userId),
                SQLConnection.CreateParameter("@IsAdmin", isAdmin)
            };

            var rowsAffected = await _sql.ExecuteNonQueryAsync(StoredProcedures.DeleteComment, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment: {CommentId}", commentId);
            throw;
        }
    }

    public async Task<List<Comment>> GetRecentCommentsAsync(int days = 7)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@Days", days)
            };

            var dt = await _sql.ExecuteDataTableAsync(StoredProcedures.GetRecentComments, parameters);
            return ConvertToCommentList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent comments");
            throw;
        }
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

            // If post info is included
            if (dt.Columns.Contains("PostTitle"))
            {
                comment.Post = new Post
                {
                    Id = comment.PostId,
                    Title = row["PostTitle"].ToString() ?? ""
                };
            }

            comments.Add(comment);
        }

        return comments;
    }
}