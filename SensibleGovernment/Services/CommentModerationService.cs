using SensibleGovernment.DataLayer.DataAccess;
using SensibleGovernment.Models;
using System.Text.RegularExpressions;

namespace SensibleGovernment.Services;

public class CommentModerationService
{
    private readonly ModerationDataAccess _moderationDataAccess;
    private readonly ILogger<CommentModerationService> _logger;
    private readonly IConfiguration _configuration;

    // Cache keywords with expiration
    private ModerationKeywords? _cachedKeywords;
    private DateTime _cacheExpiration = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    // Configurable thresholds
    private readonly int _autoHideReportThreshold;
    private readonly int _autoHideNegativeScoreThreshold;

    public CommentModerationService(
        ModerationDataAccess moderationDataAccess,
        ILogger<CommentModerationService> logger,
        IConfiguration configuration)
    {
        _moderationDataAccess = moderationDataAccess;
        _logger = logger;
        _configuration = configuration;

        // Load thresholds from config
        _autoHideReportThreshold = configuration.GetValue<int>("Moderation:AutoHideReportThreshold", 3);
        _autoHideNegativeScoreThreshold = configuration.GetValue<int>("Moderation:AutoHideNegativeScore", -5);
    }

    private async Task<ModerationKeywords> GetKeywordsAsync()
    {
        // Check cache
        if (_cachedKeywords != null && _cacheExpiration > DateTime.UtcNow)
        {
            return _cachedKeywords;
        }

        // Load from database
        _cachedKeywords = await _moderationDataAccess.GetActiveKeywordsAsync();
        _cacheExpiration = DateTime.UtcNow.Add(_cacheDuration);

        _logger.LogInformation($"Loaded {_cachedKeywords.BlockedKeywords.Count} blocked, " +
                              $"{_cachedKeywords.FlaggedKeywords.Count} flagged, " +
                              $"{_cachedKeywords.AutoReplaceKeywords.Count} replacement keywords");

        return _cachedKeywords;
    }

    public async Task<ModerationResult> ModerateCommentAsync(string content, User author, int existingReportCount = 0)
    {
        var keywords = await GetKeywordsAsync();

        var result = new ModerationResult
        {
            OriginalContent = content,
            ModeratedContent = content,
            IsVisible = true,
            RequiresReview = false
        };

        // Check if user is shadow banned
        if (author.IsShadowBanned)
        {
            if (author.ShadowBannedUntil == null || author.ShadowBannedUntil > DateTime.UtcNow)
            {
                result.IsShadowBanned = true;
                result.IsVisible = false;
                _logger.LogInformation($"Shadow banned user {author.Id} attempted to comment");
                return result;
            }
        }

        // Check for blocked keywords
        if (ContainsBlockedKeywords(content, keywords.BlockedKeywords))
        {
            result.IsBlocked = true;
            result.IsVisible = false;
            result.BlockReason = "Comment contains prohibited content";
            _logger.LogWarning($"Comment blocked for user {author.Id}: Contains blocked keywords");
            return result;
        }

        // Apply keyword filtering/replacement
        result.ModeratedContent = ApplyKeywordFiltering(content, keywords.AutoReplaceKeywords);

        // Check for flagged keywords
        if (ContainsFlaggedKeywords(content, keywords.FlaggedKeywords))
        {
            result.RequiresReview = true;
            result.FlagReason = "Contains flagged keywords";
        }

        // Check auto-hide thresholds
        if (existingReportCount >= _autoHideReportThreshold)
        {
            result.IsAutoHidden = true;
            result.IsVisible = false;
            result.AutoHideReason = $"Exceeded report threshold ({existingReportCount} reports)";
        }

        // Additional spam checks
        if (IsLikelySpam(content))
        {
            result.SpamScore = CalculateSpamScore(content);
            if (result.SpamScore > 0.7)
            {
                result.RequiresReview = true;
                result.FlagReason = "Likely spam";
            }
        }

        return result;
    }

    // Force refresh the cache (useful after adding new keywords)
    public void InvalidateCache()
    {
        _cachedKeywords = null;
        _cacheExpiration = DateTime.MinValue;
    }

    private bool ContainsBlockedKeywords(string content, HashSet<string> blockedKeywords)
    {
        var words = ExtractWords(content);
        return words.Any(word => blockedKeywords.Contains(word));
    }

    private bool ContainsFlaggedKeywords(string content, HashSet<string> flaggedKeywords)
    {
        var words = ExtractWords(content);
        return words.Any(word => flaggedKeywords.Contains(word));
    }

    private string ApplyKeywordFiltering(string content, Dictionary<string, string> autoReplaceKeywords)
    {
        var result = content;

        foreach (var replacement in autoReplaceKeywords)
        {
            var pattern = $@"\b{Regex.Escape(replacement.Key)}\b";
            result = Regex.Replace(result, pattern, replacement.Value, RegexOptions.IgnoreCase);
        }

        return result;
    }

    private bool IsLikelySpam(string content)
    {
        // Check for spam indicators
        var indicators = 0;

        // Too many URLs
        if (Regex.Matches(content, @"https?://").Count > 2) indicators++;

        // Too many exclamation marks
        if (content.Count(c => c == '!') > 5) indicators++;

        // All caps (more than 50% of letters)
        var letters = content.Where(char.IsLetter).ToList();
        if (letters.Count > 10 && letters.Count(char.IsUpper) > letters.Count * 0.5) indicators++;

        // Repeated characters
        if (Regex.IsMatch(content, @"(.)\1{4,}")) indicators++;

        // Common spam phrases
        var spamPhrases = new[] { "click here", "buy now", "limited offer", "act now" };
        if (spamPhrases.Any(phrase => content.Contains(phrase, StringComparison.OrdinalIgnoreCase))) indicators++;

        return indicators >= 2;
    }

    private double CalculateSpamScore(string content)
    {
        var score = 0.0;
        var factors = 0;

        // URL density
        var urlCount = Regex.Matches(content, @"https?://").Count;
        score += Math.Min(urlCount * 0.2, 0.4);
        factors++;

        // Caps ratio
        var letters = content.Where(char.IsLetter).ToList();
        if (letters.Count > 0)
        {
            score += (double)letters.Count(char.IsUpper) / letters.Count * 0.3;
            factors++;
        }

        // Exclamation density
        score += Math.Min(content.Count(c => c == '!') * 0.05, 0.3);
        factors++;

        return factors > 0 ? score / factors : 0;
    }

    private List<string> ExtractWords(string content)
    {
        return Regex.Matches(content, @"\b[\w']+\b")
            .Select(m => m.Value)
            .ToList();
    }

    public bool ShouldAutoHide(Comment comment, List<UserReport> reports)
    {
        // Check report threshold
        if (reports.Count >= _autoHideReportThreshold)
            return true;

        // Check negative score (if you implement voting)
        // if (comment.Score <= _autoHideNegativeScoreThreshold)
        //     return true;

        return false;
    }
}

public class ModerationResult
{
    public string OriginalContent { get; set; } = string.Empty;
    public string ModeratedContent { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public bool IsShadowBanned { get; set; } = false;
    public bool IsBlocked { get; set; } = false;
    public bool IsAutoHidden { get; set; } = false;
    public bool RequiresReview { get; set; } = false;
    public string? BlockReason { get; set; }
    public string? FlagReason { get; set; }
    public string? AutoHideReason { get; set; }
    public double SpamScore { get; set; } = 0;
}