using Microsoft.AspNetCore.Components.Authorization;

namespace SensibleGovernment.Components.Shared
{
    public partial class BritishNavBar
    {
        private bool showSearch = false;
        private string searchQuery = "";
        private string currentPath = "";
        private string currentTopic = "All";
        private AuthenticationState? authState;

        protected override async Task OnInitializedAsync()
        {
            currentPath = Navigation.Uri;
            Navigation.LocationChanged += OnLocationChanged;

            authState = await AuthProvider.GetAuthenticationStateAsync();
            AuthProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            currentPath = e.Location;
            InvokeAsync(StateHasChanged);
        }

        private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            authState = await task;
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleLogout()
        {
            await AuthProvider.LogoutAsync();
            Navigation.NavigateTo("/", true);
        }

        private void ToggleSearch()
        {
            showSearch = !showSearch;
            searchQuery = "";
        }

        private void PerformSearch()
        {
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                Navigation.NavigateTo($"/?search={Uri.EscapeDataString(searchQuery)}");
                showSearch = false;
            }
        }

        private void NavigateToTopic(string topic)
        {
            currentTopic = topic;
            if (topic == "All")
            {
                Navigation.NavigateTo("/", forceLoad: true);
            }
            else
            {
                Navigation.NavigateTo($"/?topic={topic}", forceLoad: true);
            }
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= OnLocationChanged;
            AuthProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }
}