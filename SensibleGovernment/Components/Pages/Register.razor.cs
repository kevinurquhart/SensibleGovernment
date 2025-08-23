using Microsoft.AspNetCore.Components;
using SensibleGovernment.Services;
using System.ComponentModel.DataAnnotations;

namespace SensibleGovernment.Components.Pages
{
    public partial class Register
    {
        private RegisterModel registerModel = new();
        private bool isLoading = false;
        private string? errorMessage;
        private string? successMessage;
        private bool showPassword = false;

        // Password strength
        private string passwordStrengthText = "";
        private string passwordStrengthClass = "";
        private string passwordStrengthTextClass = "";
        private int passwordStrengthPercent = 0;
        private PasswordChecks passwordChecks = new();

        // CAPTCHA
        private int captchaA;
        private int captchaB;
        private int captchaAnswer;

        protected override void OnInitialized()
        {
            if (AuthService.IsAuthenticated)
            {
                Navigation.NavigateTo("/");
            }
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

        private void CheckUsernameStrength(ChangeEventArgs e)
        {
            registerModel.UserName = e.Value?.ToString() ?? "";
        }

        private void CheckPasswordStrength(ChangeEventArgs e)
        {
            var password = e.Value?.ToString() ?? "";
            registerModel.Password = password;

            // Reset checks
            passwordChecks = new PasswordChecks
            {
                MinLength = password.Length >= 8,
                HasUpper = password.Any(char.IsUpper),
                HasLower = password.Any(char.IsLower),
                HasNumber = password.Any(char.IsDigit),
                HasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch))
            };

            // Calculate strength
            var strength = 0;
            if (passwordChecks.MinLength) strength++;
            if (passwordChecks.HasUpper) strength++;
            if (passwordChecks.HasLower) strength++;
            if (passwordChecks.HasNumber) strength++;
            if (passwordChecks.HasSpecial) strength++;
            if (password.Length >= 12) strength++;

            // Set UI indicators
            if (strength <= 2)
            {
                passwordStrengthText = "Weak password";
                passwordStrengthClass = "bg-danger";
                passwordStrengthTextClass = "text-danger";
                passwordStrengthPercent = 33;
            }
            else if (strength <= 4)
            {
                passwordStrengthText = "Moderate password";
                passwordStrengthClass = "bg-warning";
                passwordStrengthTextClass = "text-warning";
                passwordStrengthPercent = 66;
            }
            else
            {
                passwordStrengthText = "Strong password";
                passwordStrengthClass = "bg-success";
                passwordStrengthTextClass = "text-success";
                passwordStrengthPercent = 100;
            }
        }

        private bool IsFormValid()
        {
            return !string.IsNullOrEmpty(registerModel.UserName) &&
                   !string.IsNullOrEmpty(registerModel.Email) &&
                   !string.IsNullOrEmpty(registerModel.Password) &&
                   registerModel.Password == registerModel.ConfirmPassword &&
                   registerModel.AgreeToTerms &&
                   passwordChecks.MinLength &&
                   passwordChecks.HasUpper &&
                   passwordChecks.HasLower &&
                   passwordChecks.HasNumber;
        }

        private async Task HandleRegister()
        {
            isLoading = true;
            errorMessage = null;
            successMessage = null;

            try
            {
                // Validate CAPTCHA
                if (captchaAnswer != (captchaA + captchaB))
                {
                    errorMessage = "Incorrect security check answer. Please try again.";
                    GenerateCaptcha();
                    isLoading = false;
                    return;
                }

                // Additional validation
                if (!IsFormValid())
                {
                    errorMessage = "Please complete all required fields correctly.";
                    isLoading = false;
                    return;
                }

                var (success, message, _) = await AuthService.RegisterAsync(
                    registerModel.UserName,
                    registerModel.Email,
                    registerModel.Password,
                    registerModel.ConfirmPassword);

                if (success)
                {
                    successMessage = message + " Redirecting...";
                    await Task.Delay(1000);
                    Navigation.NavigateTo("/");
                }
                else
                {
                    errorMessage = message;
                    GenerateCaptcha(); // Regenerate CAPTCHA on error
                }
            }
            catch (Exception ex)
            {
                errorMessage = "An error occurred during registration. Please try again.";
                Console.WriteLine($"Registration error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        public class RegisterModel
        {
            [Required(ErrorMessage = "Username is required")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
            [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your password")]
            [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "You must agree to the terms")]
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms")]
            public bool AgreeToTerms { get; set; } = false;

            public bool SubscribeNewsletter { get; set; } = false;
        }

        private class PasswordChecks
        {
            public bool MinLength { get; set; }
            public bool HasUpper { get; set; }
            public bool HasLower { get; set; }
            public bool HasNumber { get; set; }
            public bool HasSpecial { get; set; }
        }
    }
}