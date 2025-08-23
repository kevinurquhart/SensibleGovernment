using System.Data;
using System.Data.SqlClient;
using SensibleGovernment.DataLayer.Models;
using SensibleGovernment.Models;

namespace SensibleGovernment.DataLayer.DataAccess;

public class UserDataAccess
{
    private readonly SQLConnection _sql;
    private readonly ILogger<UserDataAccess> _logger;

    public UserDataAccess(SQLConnection sql, ILogger<UserDataAccess> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            var dt = await _sql.ExecuteDataTableAsync(StoredProcedures.GetAllUsers);
            return ConvertToUserList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@UserId", id)
            };

            var dt = await _sql.ExecuteDataTableAsync(StoredProcedures.GetUserById, parameters);

            if (dt.Rows.Count == 0)
                return null;

            return ConvertToUser(dt.Rows[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id: {UserId}", id);
            throw;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@Email", email)
            };

            var dt = await _sql.ExecuteDataTableAsync(StoredProcedures.GetUserByEmail, parameters);

            if (dt.Rows.Count == 0)
                return null;

            return ConvertToUser(dt.Rows[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            throw;
        }
    }

    public async Task<int> CreateUserAsync(User user)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@UserName", user.UserName),
                SQLConnection.CreateParameter("@Email", user.Email),
                SQLConnection.CreateParameter("@PasswordHash", user.PasswordHash),
                SQLConnection.CreateParameter("@IsAdmin", user.IsAdmin),
                SQLConnection.CreateParameter("@IsActive", user.IsActive),
                SQLConnection.CreateParameter("@EmailConfirmed", user.EmailConfirmed),
                SQLConnection.CreateOutputParameter("@NewUserId", SqlDbType.Int)
            };

            var result = await _sql.ExecuteWithOutputParametersAsync(StoredProcedures.CreateUser, parameters);
            return (int)result["@NewUserId"]!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw;
        }
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@UserId", user.Id),
                SQLConnection.CreateParameter("@UserName", user.UserName),
                SQLConnection.CreateParameter("@Email", user.Email),
                SQLConnection.CreateParameter("@IsAdmin", user.IsAdmin),
                SQLConnection.CreateParameter("@IsActive", user.IsActive),
                SQLConnection.CreateParameter("@EmailConfirmed", user.EmailConfirmed)
            };

            var rowsAffected = await _sql.ExecuteNonQueryAsync(StoredProcedures.UpdateUser, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@UserId", userId),
                SQLConnection.CreateParameter("@PasswordHash", passwordHash)
            };

            var rowsAffected = await _sql.ExecuteNonQueryAsync(StoredProcedures.UpdateUserPassword, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@UserId", userId),
                SQLConnection.CreateOutputParameter("@NewStatus", SqlDbType.Bit)
            };

            var result = await _sql.ExecuteWithOutputParametersAsync(StoredProcedures.ToggleUserStatus, parameters);
            return (bool)result["@NewStatus"]!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status: {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateLoginInfoAsync(int userId, DateTime lastLogin, string? ipAddress)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@UserId", userId),
                SQLConnection.CreateParameter("@LastLogin", lastLogin),
                SQLConnection.CreateParameter("@LastLoginIp", ipAddress),
                SQLConnection.CreateParameter("@FailedLoginAttempts", 0),
                SQLConnection.CreateParameter("@LockoutEnd", DBNull.Value)
            };

            await _sql.ExecuteNonQueryAsync(StoredProcedures.UpdateUserLoginInfo, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating login info for user: {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateFailedLoginAsync(int userId)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@UserId", userId)
            };

            await _sql.ExecuteNonQueryAsync(StoredProcedures.UpdateUserFailedLogin, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating failed login for user: {UserId}", userId);
            throw;
        }
    }

    // Helper methods
    private List<User> ConvertToUserList(DataTable dt)
    {
        var users = new List<User>();

        foreach (DataRow row in dt.Rows)
        {
            users.Add(ConvertToUser(row));
        }

        return users;
    }

    private User ConvertToUser(DataRow row)
    {
        return new User
        {
            Id = Convert.ToInt32(row["Id"]),
            UserName = row["UserName"].ToString() ?? "",
            Email = row["Email"].ToString() ?? "",
            PasswordHash = row["PasswordHash"].ToString() ?? "",
            EmailConfirmed = Convert.ToBoolean(row["EmailConfirmed"]),
            FailedLoginAttempts = Convert.ToInt32(row["FailedLoginAttempts"]),
            LastFailedLogin = row["LastFailedLogin"] as DateTime?,
            LockoutEnd = row["LockoutEnd"] as DateTime?,
            IsAdmin = Convert.ToBoolean(row["IsAdmin"]),
            IsActive = Convert.ToBoolean(row["IsActive"]),
            Created = Convert.ToDateTime(row["Created"]),
            LastLogin = row["LastLogin"] as DateTime?,
            LastLoginIp = row["LastLoginIp"] as string,
            TwoFactorEnabled = row.Table.Columns.Contains("TwoFactorEnabled") && Convert.ToBoolean(row["TwoFactorEnabled"]),
            Posts = new List<Post>(),
            Comments = new List<Comment>(),
            Likes = new List<Like>(),
            ReportsSubmitted = new List<UserReport>(),
            ReportsAgainst = new List<UserReport>(),
            AdminActions = new List<AdminActionLog>()
        };
    }
}