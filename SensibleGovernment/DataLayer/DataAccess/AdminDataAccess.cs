using System.Data;
using System.Data.SqlClient;
using SensibleGovernment.DataLayer.Models;
using SensibleGovernment.Models;
using SensibleGovernment.Services;

namespace SensibleGovernment.DataLayer.DataAccess;

public class AdminDataAccess
{
    private readonly SQLConnection _sql;
    private readonly ILogger<AdminDataAccess> _logger;

    public AdminDataAccess(SQLConnection sql, ILogger<AdminDataAccess> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    public async Task<AdminDashboardStats> GetDashboardStatsAsync()
    {
        try
        {
            var dt = await _sql.ExecuteDataTableAsync(StoredProcedures.GetDashboardStats);

            if (dt.Rows.Count == 0)
                return new AdminDashboardStats();

            var row = dt.Rows[0];

            return new AdminDashboardStats
            {
                TotalUsers = Convert.ToInt32(row["TotalUsers"]),
                ActiveUsers = Convert.ToInt32(row["ActiveUsers"]),
                TotalPosts = Convert.ToInt32(row["TotalPosts"]),
                TotalComments = Convert.ToInt32(row["TotalComments"]),
                PendingReports = Convert.ToInt32(row["PendingReports"]),
                UsersRegisteredToday = Convert.ToInt32(row["UsersRegisteredToday"]),
                CommentsToday = Convert.ToInt32(row["CommentsToday"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            throw;
        }
    }

    public async Task<List<UserReport>> GetPendingReportsAsync()
    {
        try
        {
            var dt = await _sql.ExecuteDataTableAsync(StoredProcedures.GetPendingReports);
            return ConvertToReportList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending reports");
            throw;
        }
    }

    public async Task<int> CreateReportAsync(UserReport report)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@ReportingUserId", report.ReportingUserId),
                SQLConnection.CreateParameter("@ReportedUserId", report.ReportedUserId),
                SQLConnection.CreateParameter("@CommentId", report.CommentId),
                SQLConnection.CreateParameter("@Reason", report.Reason),
                SQLConnection.CreateParameter("@Details", report.Details),
                SQLConnection.CreateOutputParameter("@NewReportId", SqlDbType.Int)
            };

            var result = await _sql.ExecuteWithOutputParametersAsync(StoredProcedures.CreateReport, parameters);
            return (int)result["@NewReportId"]!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report");
            throw;
        }
    }

    public async Task<bool> ResolveReportAsync(int reportId, string resolution)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@ReportId", reportId),
                SQLConnection.CreateParameter("@Resolution", resolution)
            };

            var rowsAffected = await _sql.ExecuteNonQueryAsync(StoredProcedures.ResolveReport, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving report: {ReportId}", reportId);
            throw;
        }
    }

    private List<UserReport> ConvertToReportList(DataTable dt)
    {
        var reports = new List<UserReport>();

        foreach (DataRow row in dt.Rows)
        {
            var report = new UserReport
            {
                Id = Convert.ToInt32(row["Id"]),
                ReportingUserId = Convert.ToInt32(row["ReportingUserId"]),
                ReportedUserId = Convert.ToInt32(row["ReportedUserId"]),
                CommentId = row["CommentId"] as int?,
                Reason = row["Reason"].ToString() ?? "",
                Details = row["Details"] as string,
                Created = Convert.ToDateTime(row["Created"]),
                IsResolved = Convert.ToBoolean(row["IsResolved"]),
                Resolution = row["Resolution"] as string
            };

            // If user info is included
            if (dt.Columns.Contains("ReportingUserName"))
            {
                report.ReportingUser = new User
                {
                    Id = report.ReportingUserId,
                    UserName = row["ReportingUserName"].ToString() ?? ""
                };
            }

            if (dt.Columns.Contains("ReportedUserName"))
            {
                report.ReportedUser = new User
                {
                    Id = report.ReportedUserId,
                    UserName = row["ReportedUserName"].ToString() ?? ""
                };
            }

            // If comment info is included
            if (report.CommentId.HasValue && dt.Columns.Contains("CommentContent"))
            {
                report.Comment = new Comment
                {
                    Id = report.CommentId.Value,
                    Content = row["CommentContent"].ToString() ?? ""
                };
            }

            reports.Add(report);
        }

        return reports;
    }
}