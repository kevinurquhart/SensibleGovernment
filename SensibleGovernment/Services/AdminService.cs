using SensibleGovernment.DataLayer.DataAccess;
using SensibleGovernment.Models;

namespace SensibleGovernment.Services;

public class AdminService
{
    private readonly AdminDataAccess _adminDataAccess;
    private readonly UserDataAccess _userDataAccess;
    private readonly CommentDataAccess _commentDataAccess;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        AdminDataAccess adminDataAccess,
        UserDataAccess userDataAccess,
        CommentDataAccess commentDataAccess,
        ILogger<AdminService> logger)
    {
        _adminDataAccess = adminDataAccess;
        _userDataAccess = userDataAccess;
        _commentDataAccess = commentDataAccess;
        _logger = logger;
    }

    // User Management
    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            return await _userDataAccess.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return new List<User>();
        }
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        try
        {
            return await _userDataAccess.ToggleUserStatusAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> MakeAdminAsync(int userId)
    {
        try
        {
            var user = await _userDataAccess.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsAdmin = true;
            return await _userDataAccess.UpdateUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making user admin: {UserId}", userId);
            return false;
        }
    }

    // Reporting System
    public async Task<UserReport> ReportUserAsync(UserReport report)
    {
        try
        {
            var reportId = await _adminDataAccess.CreateReportAsync(report);
            report.Id = reportId;
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report");
            throw;
        }
    }

    public async Task<List<UserReport>> GetPendingReportsAsync()
    {
        try
        {
            return await _adminDataAccess.GetPendingReportsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending reports");
            return new List<UserReport>();
        }
    }

    public async Task<bool> ResolveReportAsync(int reportId, string resolution)
    {
        try
        {
            return await _adminDataAccess.ResolveReportAsync(reportId, resolution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving report: {ReportId}", reportId);
            return false;
        }
    }

    // Comment Moderation
    public async Task<List<Comment>> GetRecentCommentsAsync(int days = 7)
    {
        try
        {
            return await _commentDataAccess.GetRecentCommentsAsync(days);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent comments");
            return new List<Comment>();
        }
    }

    public async Task<bool> DeleteCommentAsync(int commentId)
    {
        try
        {
            // Admin can delete any comment
            return await _commentDataAccess.DeleteCommentAsync(commentId, 0, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment: {CommentId}", commentId);
            return false;
        }
    }

    // Statistics
    public async Task<AdminDashboardStats> GetDashboardStatsAsync()
    {
        try
        {
            return await _adminDataAccess.GetDashboardStatsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return new AdminDashboardStats();
        }
    }
}

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int PendingReports { get; set; }
    public int UsersRegisteredToday { get; set; }
    public int CommentsToday { get; set; }
}