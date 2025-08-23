using Microsoft.AspNetCore.Components;
using SensibleGovernment.Models;

namespace SensibleGovernment.Components.Shared
{
    public partial class ArticleCard
    {
        [Parameter] public Post? Post { get; set; }
        [Parameter] public CardSize Size { get; set; } = CardSize.Medium;
        [Parameter] public bool ShowTopic { get; set; } = true;
        [Parameter] public bool ShowAuthor { get; set; } = true;
        [Parameter] public bool ShowStats { get; set; } = true;

        public enum CardSize
        {
            Hero,    // Featured story
            Large,   // Main stories
            Medium,  // Secondary stories
            Small,   // Grid items
            Minimal  // List items
        }

        private string SizeClass => Size.ToString().ToLower();

        private string ImageUrl => !string.IsNullOrEmpty(Post?.ThumbnailImageUrl)
            ? Post.ThumbnailImageUrl
            : GetPlaceholderImage(Post?.Topic ?? "");

        private int TitleLength => Size switch
        {
            CardSize.Hero => 100,
            CardSize.Large => 80,
            CardSize.Medium => 70,
            CardSize.Small => 60,
            CardSize.Minimal => 80,
            _ => 70
        };

        private int ExcerptLength => Size switch
        {
            CardSize.Hero => 200,
            CardSize.Large => 150,
            CardSize.Medium => 100,
            CardSize.Small => 80,
            _ => 0
        };

        private void NavigateToPost()
        {
            if (Post != null)
            {
                Navigation.NavigateTo($"/post/{Post.Id}");
            }
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
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";

            return dateTime.ToString("d MMM");
        }

        private string GetPlaceholderImage(string topic)
        {
            return topic?.ToLower() switch
            {
                "sport" => "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800&h=400&fit=crop",
                "news" or "politics" => "https://images.unsplash.com/photo-1529107386315-e1a2ed48a620?w=800&h=400&fit=crop",
                "technology" => "https://images.unsplash.com/photo-1518770660439-4636190af475?w=800&h=400&fit=crop",
                "health" => "https://images.unsplash.com/photo-1559757148-5c350d0d3c56?w=800&h=400&fit=crop",
                "education" => "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800&h=400&fit=crop",
                "economy" => "https://images.unsplash.com/photo-1611974789855-9c2a0a7236a3?w=800&h=400&fit=crop",
                _ => "https://images.unsplash.com/photo-1504711434969-e33886168f5c?w=800&h=400&fit=crop"
            };
        }
    }
}