using Microsoft.AspNetCore.Components.Authorization;
using SensibleGovernment.Models;
using SensibleGovernment.Services;
using System.ComponentModel.DataAnnotations;

namespace SensibleGovernment.Components.Pages
{
    public partial class AdminCreatePost : IDisposable
    {
        // Auth check fields
        private bool isLoading = true;
        private bool isAuthorized = false;

        // Existing fields
        private PostModel newPost = new();
        private List<PostSource> sources = new();
        private bool isCreating = false;
        private bool isSaving = false;
        private string? errorMessage;
        private string? successMessage;
        private string activeSection = "basic";
        private AuthenticationState? authState;
        private int currentUserId = 0;

        // Rich text editor fields
        private bool useRichTextEditor = true;

        // Media upload fields
        private bool mediaUploadMode = true;

        // Auto-save fields
        private System.Timers.Timer? autoSaveTimer;
        private bool hasUnsavedChanges = false;
        private DateTime? lastAutoSave;
        private bool isAutoSaving = false;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;

            authState = await AuthStateProvider.GetAuthenticationStateAsync();

            // Check if user is authenticated
            if (authState?.User.Identity?.IsAuthenticated != true)
            {
                Console.WriteLine("AdminCreatePost - Not authenticated, redirecting to login");
                Navigation.NavigateTo("/login");
                return;
            }

            // Check if user is admin
            if (!authState.User.IsInRole("Admin"))
            {
                Console.WriteLine("AdminCreatePost - Not admin, showing error");
                isAuthorized = false;
                isLoading = false;
                return;
            }

            Console.WriteLine("AdminCreatePost - User is authorized admin");
            isAuthorized = true;

            // Get current user ID for post creation
            var userIdClaim = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                currentUserId = userId;
            }

            // Set up auto-save timer (every 30 seconds)
            autoSaveTimer = new System.Timers.Timer(30000);
            autoSaveTimer.Elapsed += async (sender, e) => await AutoSave();
            autoSaveTimer.Start();

            // Load draft if exists
            await LoadDraft();

            isLoading = false;
        }

        private void NextSection()
        {
            switch (activeSection)
            {
                case "basic":
                    activeSection = "content";
                    break;
                case "content":
                    activeSection = "media";
                    break;
                case "media":
                    activeSection = "sources";
                    break;
            }
        }

        private void PreviousSection()
        {
            switch (activeSection)
            {
                case "content":
                    activeSection = "basic";
                    break;
                case "media":
                    activeSection = "content";
                    break;
                case "sources":
                    activeSection = "media";
                    break;
            }
        }

        private void AddSource()
        {
            sources.Add(new PostSource());
        }

        private void RemoveSource(PostSource source)
        {
            sources.Remove(source);
        }

        private async Task SaveDraft()
        {
            isSaving = true;
            newPost.IsPublished = false;
            await HandleCreatePost();
            isSaving = false;
        }

        private void PreviewArticle()
        {
            // In production, implement a preview modal or page
            successMessage = "Preview feature coming soon!";
        }

        private async Task HandleCreatePost()
        {
            if (currentUserId == 0)
            {
                errorMessage = "You must be logged in to create a post";
                return;
            }

            isCreating = true;
            errorMessage = null;
            successMessage = null;

            try
            {
                var post = new Post
                {
                    Title = newPost.Title,
                    Content = newPost.Content,
                    Opinion = newPost.Opinion,
                    Topic = newPost.Topic,
                    FeaturedImageUrl = newPost.FeaturedImageUrl,
                    ThumbnailImageUrl = newPost.ThumbnailImageUrl,
                    ImageCaption = newPost.ImageCaption,
                    IsPublished = newPost.IsPublished,
                    IsFeatured = newPost.IsFeatured,
                    AuthorId = currentUserId,
                    Created = DateTime.Now,
                    Sources = sources.Where(s => !string.IsNullOrEmpty(s.Title) && !string.IsNullOrEmpty(s.Url)).ToList()
                };

                var createdPost = await PostService.CreatePostAsync(post);

                successMessage = newPost.IsPublished ?
                    "Article published successfully! Redirecting..." :
                    "Draft saved successfully! Redirecting...";
                StateHasChanged();
                await Task.Delay(1000);

                Navigation.NavigateTo($"/post/{createdPost.Id}");
            }
            catch (Exception ex)
            {
                errorMessage = $"An error occurred: {ex.Message}";
                Console.WriteLine($"Error creating post: {ex.Message}");
            }
            finally
            {
                isCreating = false;
            }
        }

        // Rich text editor methods
        private void ToggleEditor()
        {
            useRichTextEditor = !useRichTextEditor;
        }

        private int GetPlainTextLength()
        {
            if (string.IsNullOrEmpty(newPost.Content))
                return 0;

            // Strip HTML tags for character count if using rich text editor
            if (useRichTextEditor)
            {
                var plainText = System.Text.RegularExpressions.Regex.Replace(newPost.Content, "<.*?>", "");
                return plainText.Length;
            }

            return newPost.Content.Length;
        }

        // Auto-save methods
        private async Task AutoSave()
        {
            if (!hasUnsavedChanges || isAutoSaving || isCreating || isSaving)
                return;

            await InvokeAsync(async () =>
            {
                isAutoSaving = true;
                StateHasChanged();

                try
                {
                    // Save to localStorage or temporary database table
                    await SaveDraftToLocalStorage();
                    lastAutoSave = DateTime.Now;
                    hasUnsavedChanges = false;

                    Console.WriteLine("Auto-saved draft");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to auto-save: {ex.Message}");
                }
                finally
                {
                    isAutoSaving = false;
                    StateHasChanged();
                }
            });
        }

        private async Task SaveDraftToLocalStorage()
        {
            var draft = new
            {
                Title = newPost.Title,
                Content = newPost.Content,
                Opinion = newPost.Opinion,
                Topic = newPost.Topic,
                FeaturedImageUrl = newPost.FeaturedImageUrl,
                ThumbnailImageUrl = newPost.ThumbnailImageUrl,
                ImageCaption = newPost.ImageCaption,
                IsPublished = newPost.IsPublished,
                IsFeatured = newPost.IsFeatured,
                Sources = sources,
                SavedAt = DateTime.Now
            };

            // In a real implementation, you'd use IJSRuntime to save to localStorage
            // or save to a Drafts table in the database
            await Task.CompletedTask; // Placeholder
        }

        private async Task LoadDraft()
        {
            // Load from localStorage or database
            // This is a placeholder - implement actual loading logic
            await Task.CompletedTask;
        }

        // Track changes - now properly implemented
        private void OnFieldChanged()
        {
            hasUnsavedChanges = true;
        }

        // Clean up
        public void Dispose()
        {
            autoSaveTimer?.Stop();
            autoSaveTimer?.Dispose();
        }

        public class PostModel
        {
            [Required(ErrorMessage = "Title is required")]
            [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please select a topic")]
            public string Topic { get; set; } = string.Empty;

            [Required(ErrorMessage = "Content is required")]
            [StringLength(50000, MinimumLength = 50, ErrorMessage = "Content must be between 50 and 50000 characters")]
            public string Content { get; set; } = string.Empty;

            public string? Opinion { get; set; }
            public string? FeaturedImageUrl { get; set; }
            public string? ThumbnailImageUrl { get; set; }
            public string? ImageCaption { get; set; }
            public bool IsPublished { get; set; } = true;
            public bool IsFeatured { get; set; } = false;
        }
    }
}