using System.Net;
using System.Net.Mail;

namespace SensibleGovernment.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _isEnabled;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _isEnabled = _configuration.GetValue<bool>("Email:Enabled");
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation($"Email service disabled. Would send to {to}: {subject}");
            return true; // Return success in dev mode
        }

        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["Email:SmtpPass"];
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
            var fromName = _configuration["Email:FromName"] ?? "The Sensible Citizen";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(new MailAddress(to));

            await client.SendMailAsync(message);
            _logger.LogInformation($"Email sent successfully to {to}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {to}");
            return false;
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string userName)
    {
        var subject = "Welcome to The Sensible Citizen";
        var body = $@"
            <h2>Welcome {userName}!</h2>
            <p>Thank you for joining The Sensible Citizen community.</p>
            <p>You can now:</p>
            <ul>
                <li>Comment on articles</li>
                <li>Like posts</li>
                <li>Engage in discussions</li>
            </ul>
            <p>Stay informed, stay engaged!</p>
            <hr>
            <p><small>The Sensible Citizen - Accountability, Analysis, Action</small></p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendCommentNotificationAsync(string postAuthorEmail, string postTitle, string commenterName, string commentContent)
    {
        var subject = $"New comment on your article: {postTitle}";
        var body = $@"
            <h3>New Comment on Your Article</h3>
            <p><strong>{commenterName}</strong> commented on your article <em>{postTitle}</em>:</p>
            <blockquote style='border-left: 3px solid #ccc; padding-left: 10px; margin-left: 0;'>
                {System.Web.HttpUtility.HtmlEncode(commentContent)}
            </blockquote>
            <p><a href='#'>View and respond to this comment</a></p>
            <hr>
            <p><small>The Sensible Citizen - Accountability, Analysis, Action</small></p>
        ";

        await SendEmailAsync(postAuthorEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        var subject = "Password Reset Request";
        var resetUrl = $"https://yourdomain.com/reset-password?token={resetToken}";
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>We received a request to reset your password.</p>
            <p>Click the link below to reset your password:</p>
            <p><a href='{resetUrl}' style='background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a></p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't request this, please ignore this email.</p>
            <hr>
            <p><small>The Sensible Citizen - Accountability, Analysis, Action</small></p>
        ";

        await SendEmailAsync(email, subject, body);
    }
}