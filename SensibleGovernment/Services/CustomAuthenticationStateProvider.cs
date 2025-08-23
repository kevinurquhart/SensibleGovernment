using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using SensibleGovernment.Models;

namespace SensibleGovernment.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _localStorage;
    private readonly AuthService _authService;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(
        ProtectedLocalStorage localStorage,
        AuthService authService,
        ILogger<CustomAuthenticationStateProvider> logger)
    {
        _localStorage = localStorage;
        _authService = authService;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Check if we have a stored session
            var userSessionResult = await _localStorage.GetAsync<UserSession>("userSession");

            if (!userSessionResult.Success || userSessionResult.Value == null)
            {
                return new AuthenticationState(_anonymous);
            }

            var userSession = userSessionResult.Value;

            // Check if session is expired
            if (userSession.ExpiresAt < DateTime.UtcNow)
            {
                await _localStorage.DeleteAsync("userSession");
                return new AuthenticationState(_anonymous);
            }

            // Validate the session is still valid (user not suspended, etc.)
            var user = await _authService.GetUserByIdAsync(userSession.UserId);
            if (user == null || !user.IsActive)
            {
                await _localStorage.DeleteAsync("userSession");
                return new AuthenticationState(_anonymous);
            }

            // Create claims principal
            var claimsPrincipal = CreateClaimsPrincipal(user);
            return new AuthenticationState(claimsPrincipal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var (success, message, requiresCaptcha) = await _authService.LoginAsync(email, password);

            if (!success)
            {
                return false;
            }

            var user = _authService.CurrentUser;
            if (user == null) return false;

            // Create and store session
            var userSession = new UserSession
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                IsAdmin = user.IsAdmin,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30) // 30 minute session
            };

            await _localStorage.SetAsync("userSession", userSession);

            // Update authentication state
            var claimsPrincipal = CreateClaimsPrincipal(user);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _authService.Logout();
            await _localStorage.DeleteAsync("userSession");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<bool> RegisterAsync(string userName, string email, string password)
    {
        try
        {
            var (success, message, _) = await _authService.RegisterAsync(userName, email, password);

            if (!success)
            {
                return false;
            }

            // Auto-login after registration
            return await LoginAsync(email, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return false;
        }
    }

    private ClaimsPrincipal CreateClaimsPrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("IsActive", user.IsActive.ToString()),
            new Claim("Created", user.Created.ToString())
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        claims.Add(new Claim(ClaimTypes.Role, "User"));

        var identity = new ClaimsIdentity(claims, "Custom");
        return new ClaimsPrincipal(identity);
    }

    // Helper class for session storage
    public class UserSession
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}