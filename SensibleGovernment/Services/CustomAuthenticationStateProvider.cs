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
            _logger.LogInformation("GetAuthenticationStateAsync called");

            // Always check localStorage - don't cache across different calls
            ProtectedBrowserStorageResult<UserSession> userSessionResult;
            try
            {
                userSessionResult = await _localStorage.GetAsync<UserSession>("userSession");
                _logger.LogInformation($"LocalStorage read. Success: {userSessionResult.Success}");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
            {
                // This happens during prerendering/SSR - it's expected
                _logger.LogInformation("JavaScript interop not available (SSR/prerendering), returning anonymous");
                return new AuthenticationState(_anonymous);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from localStorage");
                return new AuthenticationState(_anonymous);
            }

            if (!userSessionResult.Success || userSessionResult.Value == null)
            {
                _logger.LogInformation("No user session found in localStorage");
                return new AuthenticationState(_anonymous);
            }

            var userSession = userSessionResult.Value;
            _logger.LogInformation($"Session found for user: {userSession.UserName}, IsAdmin: {userSession.IsAdmin}");

            // Check if session is expired
            if (userSession.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning($"Session expired for user {userSession.UserName}");
                await _localStorage.DeleteAsync("userSession");
                return new AuthenticationState(_anonymous);
            }

            // For performance, we trust the session data instead of hitting the DB every time
            // Only validate against DB on login or when explicitly needed

            // Create claims principal from session data
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString()),
                new Claim(ClaimTypes.Name, userSession.UserName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim("IsActive", "True"),
                new Claim(ClaimTypes.Role, "User")
            };

            if (userSession.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                _logger.LogInformation($"Admin role added for user {userSession.UserName}");
            }

            var identity = new ClaimsIdentity(claims, "Custom");
            var principal = new ClaimsPrincipal(identity);

            _logger.LogInformation($"Returning auth state for {userSession.UserName}, Roles: {string.Join(", ", claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");

            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetAuthenticationStateAsync");
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
                _logger.LogWarning($"Login failed for {email}: {message}");
                return false;
            }

            var user = _authService.CurrentUser;
            if (user == null)
            {
                _logger.LogError("Login succeeded but CurrentUser is null");
                return false;
            }

            // Create and store session with extended expiry
            var userSession = new UserSession
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                IsAdmin = user.IsAdmin,
                ExpiresAt = DateTime.UtcNow.AddHours(8) // 8 hour session
            };

            await _localStorage.SetAsync("userSession", userSession);

            _logger.LogInformation($"Session stored for user: {user.UserName}, IsAdmin: {user.IsAdmin}, Expires: {userSession.ExpiresAt}");

            // Notify that auth state has changed
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));

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

            var authState = new AuthenticationState(_anonymous);
            NotifyAuthenticationStateChanged(Task.FromResult(authState));

            _logger.LogInformation("User logged out and session cleared");
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