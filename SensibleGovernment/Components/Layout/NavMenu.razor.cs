using Microsoft.AspNetCore.Components.Authorization;

namespace SensibleGovernment.Components.Layout
{
    public partial class NavMenu
    {
        private AuthenticationState? authState;

        protected override async Task OnInitializedAsync()
        {
            authState = await AuthProvider.GetAuthenticationStateAsync();
            AuthProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
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

        public void Dispose()
        {
            AuthProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }
}