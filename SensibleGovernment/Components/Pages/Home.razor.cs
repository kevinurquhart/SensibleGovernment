using Microsoft.AspNetCore.Components.Authorization;
using SensibleGovernment.Models;
using SensibleGovernment.Services;

namespace SensibleGovernment.Components.Pages
{
    public partial class Home
    {
        private bool loading = true;
        private Post? breakingNews;
        private Post? heroPost;
        private List<Post> topStories = new();
        private List<Post> opinionPosts = new();
        private List<Post> moreNews = new();
        private List<Post> mostReadPosts = new();
        private List<Post> allPosts = new();
        private AuthenticationState? authState;

        protected override async Task OnInitializedAsync()
        {
            authState = await AuthProvider.GetAuthenticationStateAsync();
            await LoadPosts();

            AuthProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            authState = await task;
            await LoadPosts();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadPosts()
        {
            loading = true;

            // Get query parameters
            var uri = new Uri(Navigation.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var topic = query["topic"];
            var search = query["search"];

            allPosts = await PostService.GetAllPostsAsync();

            // Apply filters
            var filteredPosts = allPosts.AsEnumerable();

            if (!string.IsNullOrEmpty(topic))
            {
                filteredPosts = filteredPosts.Where(p => p.Topic == topic);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                filteredPosts = filteredPosts.Where(p =>
                    p.Title.ToLower().Contains(searchLower) ||
                    p.Content.ToLower().Contains(searchLower));
            }

            var posts = filteredPosts.ToList();

            // Set breaking news (posts less than 2 hours old with high importance)
            breakingNews = posts
                .Where(p => (DateTime.Now - p.Created).TotalHours < 2 && p.IsFeatured)
                .FirstOrDefault();

            // Set hero post (most important recent story)
            heroPost = posts
                .Where(p => p != breakingNews)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Created)
                .FirstOrDefault();

            // Top stories (next 3 most recent, excluding hero and breaking)
            topStories = posts
                .Where(p => p != heroPost && p != breakingNews)
                .OrderByDescending(p => p.Created)
                .Take(3)
                .ToList();

            // Opinion posts (posts with substantial opinion content)
            opinionPosts = posts
                .Where(p => !string.IsNullOrEmpty(p.Opinion) && p != heroPost && p != breakingNews)
                .OrderByDescending(p => p.Created)
                .Take(4)
                .ToList();

            // More news (everything else)
            var usedPosts = new[] { heroPost, breakingNews }
                .Concat(topStories)
                .Concat(opinionPosts)
                .Where(p => p != null);

            moreNews = posts
                .Where(p => !usedPosts.Contains(p))
                .OrderByDescending(p => p.Created)
                .ToList();

            // Most read (by view count)
            mostReadPosts = allPosts
                .OrderByDescending(p => p.ViewCount)
                .Take(5)
                .ToList();

            loading = false;
        }

        private int GetDaysInPower()
        {
            // Days since last election (example: July 4, 2024)
            var electionDate = new DateTime(2024, 7, 4);
            return (DateTime.Now - electionDate).Days;
        }

        private int GetDaysToElection()
        {
            // Next election (max 5 years from last)
            var nextElection = new DateTime(2029, 7, 4);
            return Math.Max(0, (nextElection - DateTime.Now).Days);
        }

        private List<(string Topic, int Count)> GetTopicCounts()
        {
            var topics = new[] { "Politics", "Economy", "Health", "Education", "Technology", "Sport" };
            return topics
                .Select(t => (Topic: t, Count: allPosts.Count(p => p.Topic == t || (t == "Politics" && p.Topic == "News"))))
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        public void Dispose()
        {
            AuthProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }
}