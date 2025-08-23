using Microsoft.AspNetCore.Components.Authorization;
using SensibleGovernment.Models;
using SensibleGovernment.Services;

namespace SensibleGovernment.Components.Pages
{
    public partial class AdminDashboard
    {
        // Auth check fields
        private bool isLoading = true;
        private bool isAuthorized = false;

        // Existing fields
        private string activeTab = "users";
        private AdminDashboardStats? stats;
        private List<User>? users;
        private List<UserReport>? pendingReports;
        private List<Comment>? recentComments;
        private AuthenticationState? authState;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;

            try
            {
                // Use the injected AuthStateProvider (which is AuthenticationStateProvider)
                authState = await AuthStateProvider.GetAuthenticationStateAsync();

                // Log for debugging
                Console.WriteLine($"AdminDashboard - User authenticated: {authState?.User.Identity?.IsAuthenticated}");
                Console.WriteLine($"AdminDashboard - User name: {authState?.User.Identity?.Name}");
                Console.WriteLine($"AdminDashboard - Is Admin: {authState?.User.IsInRole("Admin")}");

                // Check if user is authenticated
                if (authState?.User.Identity?.IsAuthenticated != true)
                {
                    Console.WriteLine("AdminDashboard - Not authenticated, redirecting to login");
                    Navigation.NavigateTo("/login");
                    return;
                }

                // Check if user is admin
                if (!authState.User.IsInRole("Admin"))
                {
                    Console.WriteLine("AdminDashboard - Not admin, showing error");
                    isAuthorized = false;
                    isLoading = false;
                    return;
                }

                Console.WriteLine("AdminDashboard - User is authorized admin");
                isAuthorized = true;

                // Load dashboard data
                await LoadDashboard();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminDashboard - Error during initialization: {ex.Message}");
                isAuthorized = false;
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadDashboard()
        {
            stats = await AdminService.GetDashboardStatsAsync();
            users = await AdminService.GetAllUsersAsync();
            pendingReports = await AdminService.GetPendingReportsAsync();
            recentComments = await AdminService.GetRecentCommentsAsync();
        }

        private async Task ToggleUserStatus(int userId)
        {
            await AdminService.ToggleUserStatusAsync(userId);
            await LoadDashboard();
        }

        private async Task ResolveReport(int reportId, string resolution)
        {
            await AdminService.ResolveReportAsync(reportId, resolution);
            await LoadDashboard();
        }

        private async Task DeleteComment(int commentId)
        {
            await AdminService.DeleteCommentAsync(commentId);
            await LoadDashboard();
        }
    }
}