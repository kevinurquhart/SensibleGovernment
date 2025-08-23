using SensibleGovernment.Services;
using System.ComponentModel.DataAnnotations;

namespace SensibleGovernment.Components.Pages
{
    public partial class Login
    {
        private LoginModel loginModel = new();
        private bool isLoading = false;
        private string? errorMessage;
        private string? successMessage;
        private bool showPassword = false;
        private bool requiresCaptcha = false;
        private bool showTwoFactorInput = false;
        private bool showTimeoutMessage = false;
        private bool showSecurityTips = true;
        private string twoFactorCode = "";

        // Simple CAPTCHA
        private int captchaA;
        private int captchaB;
        private int captchaAnswer;

        protected override void OnInitialized()
        {
            if (AuthService.IsAuthenticated)
            {
                Navigation.NavigateTo("/");
            }

            // Check for timeout parameter
            var uri = new Uri(Navigation.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            showTimeoutMessage = query["timeout"] == "true";

            GenerateCaptcha();
        }

        private void GenerateCaptcha()
        {
            var random = new Random();
            captchaA = random.Next(1, 10);
            captchaB = random.Next(1, 10);
        }

        private void TogglePassword()
        {
            showPassword = !showPassword;
        }

        private async Task HandleLogin()
        {
            isLoading = true;
            errorMessage = null;
            successMessage = null;

            try
            {
                // Validate CAPTCHA if required
                if (requiresCaptcha && captchaAnswer != (captchaA + captchaB))
                {
                    errorMessage = "Incorrect security check answer. Please try again.";
                    GenerateCaptcha();
                    isLoading = false;
                    return;
                }

                var success = await AuthProvider.LoginAsync(
                    loginModel.Email,
                    loginModel.Password);

                if (success)
                {
                    successMessage = "Login successful! Redirecting...";
                    await Task.Delay(500);
                    Navigation.NavigateTo("/", true); // Force reload to update auth state
                }
                else
                {
                    errorMessage = "Invalid email or password";
                    if (requiresCaptcha)
                    {
                        GenerateCaptcha();
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = "An error occurred during login. Please try again.";
                Console.WriteLine($"Login error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task VerifyTwoFactor()
        {
            // In production, verify the TOTP code properly
            // For now, we'll accept any 6-digit code
            if (twoFactorCode.Length == 6)
            {
                successMessage = "Two-factor authentication successful!";
                await Task.Delay(500);
                Navigation.NavigateTo("/");
            }
            else
            {
                errorMessage = "Invalid code. Please enter a 6-digit code.";
            }
        }

        public class LoginModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; } = false;
        }
    }
}