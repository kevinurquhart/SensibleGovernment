using System.Data;
using SensibleGovernment.Models;

namespace SensibleGovernment.DataLayer.DataAccess;

public class ModerationDataAccess
{
    private readonly SQLConnection _sql;
    private readonly ILogger<ModerationDataAccess> _logger;

    public ModerationDataAccess(SQLConnection sql, ILogger<ModerationDataAccess> logger)
    {
        _sql = sql;
        _logger = logger;
    }

    public async Task<ModerationKeywords> GetActiveKeywordsAsync()
    {
        try
        {
            var dt = await _sql.ExecuteDataTableAsync("moderation_GetActiveKeywords");

            var result = new ModerationKeywords
            {
                BlockedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                FlaggedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                AutoReplaceKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            foreach (DataRow row in dt.Rows)
            {
                var keyword = row["Keyword"].ToString() ?? "";
                var action = row["Action"].ToString() ?? "";
                var replacement = row["Replacement"]?.ToString();

                switch (action.ToLower())
                {
                    case "block":
                        result.BlockedKeywords.Add(keyword);
                        break;
                    case "flag":
                        result.FlaggedKeywords.Add(keyword);
                        break;
                    case "replace":
                        if (!string.IsNullOrEmpty(replacement))
                            result.AutoReplaceKeywords[keyword] = replacement;
                        break;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading moderation keywords");
            // Return empty sets on error
            return new ModerationKeywords
            {
                BlockedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                FlaggedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                AutoReplaceKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }
    }

    public async Task<bool> AddKeywordAsync(string keyword, string action, string? replacement, int userId)
    {
        try
        {
            var parameters = new[]
            {
                SQLConnection.CreateParameter("@Keyword", keyword),
                SQLConnection.CreateParameter("@Action", action),
                SQLConnection.CreateParameter("@Replacement", replacement ?? (object)DBNull.Value),
                SQLConnection.CreateParameter("@CreatedBy", userId)
            };

            await _sql.ExecuteNonQueryAsync("moderation_AddKeyword", parameters);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding keyword: {Keyword}", keyword);
            return false;
        }
    }
}

public class ModerationKeywords
{
    public HashSet<string> BlockedKeywords { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> FlaggedKeywords { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> AutoReplaceKeywords { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}