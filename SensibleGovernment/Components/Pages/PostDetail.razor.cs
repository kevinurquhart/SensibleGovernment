using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using SensibleGovernment.Models;
using SensibleGovernment.Services;

namespace SensibleGovernment.Components.Pages
{
    public partial class PostDetail
    {
        [Parameter] public int PostId { get; set; }

        [Inject] private HtmlSanitizerService HtmlSanitizer { get; set; } = default!;
        [Inject] private InputValidationService ValidationService { get; set; } = default!;

        private Post? post;
        private Comment newComment = new();
        private bool loading = true;
        private bool addingComment = false;
        private bool userHasLiked = false;
        private AuthenticationState? authState;
        private int currentUserId = 0;
        private bool isUserActive = true;

        private int? isReplyingTo = null;
        private Comment replyComment = new();
        private bool addingReply = false;
        private string? commentError;
        private string? replyError;

        // Report modal
        private bool showReportModal = false;
        private Comment? reportedComment;
        private string reportReason = "";
        private string reportDetails = "";
        private List<string> reportReasons = new()
    {
        "Spam or advertising",
        "Offensive or inappropriate",
        "Harassment or bullying",
        "Misinformation",
        "Other"
    };

        protected override async Task OnInitializedAsync()
        {
            authState = await AuthStateProvider.GetAuthenticationStateAsync();  // Changed from AuthProvider

            if (authState?.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    currentUserId = userId;

                    // Check if user is active
                    var user = await AuthService.GetUserByIdAsync(userId);
                    isUserActive = user?.IsActive ?? false;
                }
            }

            await LoadPost();
        }

        private async Task LoadPost()
        {
            loading = true;
            post = await PostService.GetPostByIdAsync(PostId);

            if (post != null)
            {
                // Increment view count
                post.ViewCount++;
                // In production, you'd save this to the database

                if (currentUserId > 0)
                {
                    userHasLiked = post.Likes.Any(l => l.UserId == currentUserId);
                }
            }

            loading = false;
        }

        private async Task ToggleLike()
        {
            if (currentUserId == 0) return;

            var liked = await PostService.ToggleLikeAsync(PostId, currentUserId);
            userHasLiked = liked;
            await LoadPost();
        }

        private async Task AddComment()
        {
            if (currentUserId == 0 || string.IsNullOrWhiteSpace(newComment.Content))
                return;

            addingComment = true;
            commentError = null;

            try
            {
                // Validate
                var validation = ValidationService.ValidateComment(newComment.Content);
                if (!validation.IsValid)
                {
                    commentError = validation.GetErrorString();
                    return;
                }

                var comment = new Comment
                {
                    Content = newComment.Content,
                    PostId = PostId,
                    AuthorId = currentUserId,
                    ParentCommentId = null // Top-level comment
                };

                await PostService.AddCommentAsync(comment);
                newComment = new Comment();
                await LoadPost();
            }
            catch (Exception ex)
            {
                commentError = "Failed to post comment. Please try again.";
                Console.WriteLine($"Error posting comment: {ex.Message}");
            }
            finally
            {
                addingComment = false;
            }
        }

        private async Task DeleteComment(int commentId)
        {
            if (currentUserId == 0) return;

            if (authState?.User.IsInRole("Admin") == true)
            {
                await AdminService.DeleteCommentAsync(commentId);
            }
            else
            {
                await PostService.DeleteCommentAsync(commentId, currentUserId);
            }

            await LoadPost();
        }

        private void ShowReportDialog(Comment comment)
        {
            reportedComment = comment;
            showReportModal = true;
            reportReason = "";
            reportDetails = "";
        }

        private void CloseReportDialog()
        {
            showReportModal = false;
            reportedComment = null;
        }

        private async Task SubmitReport()
        {
            if (reportedComment == null || currentUserId == 0 || string.IsNullOrEmpty(reportReason))
                return;

            var report = new UserReport
            {
                ReportingUserId = currentUserId,
                ReportedUserId = reportedComment.AuthorId,
                CommentId = reportedComment.Id,
                Reason = reportReason,
                Details = reportDetails
            };

            await AdminService.ReportUserAsync(report);
            CloseReportDialog();
        }

        private async Task SharePost()
        {
            // In production, implement proper share functionality
            await JS.InvokeVoidAsync("navigator.share", new
            {
                title = post?.Title,
                text = TruncateText(post?.Content ?? "", 100),
                url = Navigation.Uri
            });
        }

        private string FormatContent(string content)
        {
            // Convert line breaks to paragraphs for better formatting
            var paragraphs = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", paragraphs.Select(p => $"<p>{p}</p>"));
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";

            return dateTime.ToString("MMM dd, yyyy 'at' h:mm tt");
        }

        private void StartReply(Comment parentComment)
        {
            isReplyingTo = parentComment.Id;
            replyComment = new Comment { ParentCommentId = parentComment.Id };
            replyError = null;
        }

        private void CancelReply()
        {
            isReplyingTo = null;
            replyComment = new Comment();
            replyError = null;
        }

        private async Task AddReply()
        {
            if (currentUserId == 0 || string.IsNullOrWhiteSpace(replyComment.Content))
                return;

            addingReply = true;
            replyError = null;

            try
            {
                // Validate
                var validation = ValidationService.ValidateComment(replyComment.Content);
                if (!validation.IsValid)
                {
                    replyError = validation.GetErrorString();
                    return;
                }

                var reply = new Comment
                {
                    Content = replyComment.Content,
                    PostId = PostId,
                    AuthorId = currentUserId,
                    ParentCommentId = isReplyingTo
                };

                await PostService.AddCommentAsync(reply);

                // Reset form
                CancelReply();

                // Reload post to show new reply
                await LoadPost();
            }
            catch (Exception ex)
            {
                replyError = "Failed to post reply. Please try again.";
                Console.WriteLine($"Error posting reply: {ex.Message}");
            }
            finally
            {
                addingReply = false;
            }
        }

        private bool HasReplies(Comment comment)
        {
            return post?.Comments.Any(c => c.ParentCommentId == comment.Id) ?? false;
        }

        private int GetReplyCount(Comment comment)
        {
            return post?.Comments.Count(c => c.ParentCommentId == comment.Id) ?? 0;
        }

        private string SanitizeCommentForDisplay(string content)
        {
            return HtmlSanitizer.SanitizeHtml(content);
        }
    }
}