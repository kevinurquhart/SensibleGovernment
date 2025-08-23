using Microsoft.AspNetCore.Components.Authorization;
using SensibleGovernment.Models;
using SensibleGovernment.Services;
using System.ComponentModel.DataAnnotations;

namespace SensibleGovernment.Components.Pages
{
    public partial class AdminCreatePost
    {
        private PostModel newPost = new();
        private List<PostSource> sources = new();
        private bool isCreating = false;
        private bool isSaving = false;
        private string? errorMessage;
        private string? successMessage;
        private string activeSection = "basic";
        private AuthenticationState? authState;
        private int currentUserId = 0;

        protected override async Task OnInitializedAsync()
        {
            authState = await AuthProvider.GetAuthenticationStateAsync();
            if (authState?.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    currentUserId = userId;
                }
            }
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