using Microsoft.EntityFrameworkCore;
using SensibleGovernment.Data;
using SensibleGovernment.Models;

namespace SensibleGovernment.Services;

public class AdminService
{
    private readonly AppDbContext _context;

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    // User Management
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Posts)
            .Include(u => u.Comments)
            .Include(u => u.ReportsAgainst)
            .OrderByDescending(u => u.Created)
            .ToListAsync();
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.IsAdmin) return false;

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return user.IsActive;
    }

    public async Task<bool> MakeAdminAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsAdmin = true;
        await _context.SaveChangesAsync();
        return true;
    }

    // Reporting System
    public async Task<UserReport> ReportUserAsync(UserReport report)
    {
        _context.UserReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<List<UserReport>> GetPendingReportsAsync()
    {
        return await _context.UserReports
            .Include(r => r.ReportingUser)
            .Include(r => r.ReportedUser)
            .Include(r => r.Comment)
            .Where(r => !r.IsResolved)
            .OrderByDescending(r => r.Created)
            .ToListAsync();
    }

    public async Task<bool> ResolveReportAsync(int reportId, string resolution)
    {
        var report = await _context.UserReports.FindAsync(reportId);
        if (report == null) return false;

        report.IsResolved = true;
        report.Resolution = resolution;
        await _context.SaveChangesAsync();
        return true;
    }

    // Comment Moderation
    public async Task<List<Comment>> GetRecentCommentsAsync(int days = 7)
    {
        var cutoff = DateTime.Now.AddDays(-days);
        return await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Where(c => c.Created >= cutoff)
            .OrderByDescending(c => c.Created)
            .ToListAsync();
    }

    public async Task<bool> DeleteCommentAsync(int commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null) return false;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }

    // Statistics
    public async Task<AdminDashboardStats> GetDashboardStatsAsync()
    {
        var stats = new AdminDashboardStats
        {
            TotalUsers = await _context.Users.CountAsync(),
            ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
            TotalPosts = await _context.Posts.CountAsync(),
            TotalComments = await _context.Comments.CountAsync(),
            PendingReports = await _context.UserReports.CountAsync(r => !r.IsResolved),
            UsersRegisteredToday = await _context.Users.CountAsync(u => u.Created.Date == DateTime.Today),
            CommentsToday = await _context.Comments.CountAsync(c => c.Created.Date == DateTime.Today)
        };

        return stats;
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