using Microsoft.AspNetCore.Components.Authorization;
using SensibleGovernment.Models;
using SensibleGovernment.Services;

namespace SensibleGovernment.Components.Pages
{
    public partial class AdminDashboard
    {
        private string activeTab = "users";
        private AdminDashboardStats? stats;
        private List<User>? users;
        private List<UserReport>? pendingReports;
        private List<Comment>? recentComments;
        private AuthenticationState? authState;

        protected override async Task OnInitializedAsync()
        {
            authState = await AuthProvider.GetAuthenticationStateAsync();

            // The [Authorize] attribute ensures we're authenticated, but double-check for admin
            if (authState?.User.IsInRole("Admin") == true)
            {
                await LoadDashboard();
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