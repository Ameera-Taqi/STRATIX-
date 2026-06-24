using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stratix.Application.Interfaces;

namespace Stratix.Infrastructure.Mail;

public class SmtpPasswordResetMailService : IPasswordResetMailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpPasswordResetMailService> _logger;

    public SmtpPasswordResetMailService(IConfiguration config, ILogger<SmtpPasswordResetMailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        var host = _config["Stratix:Mail:Host"] ?? "localhost";
        var port = int.TryParse(_config["Stratix:Mail:Port"], out var p) ? p : 1025;
        var from = _config["Stratix:Mail:From"] ?? "noreply@stratix.local";
        var useAuth = bool.TryParse(_config["Stratix:Mail:SmtpAuth"], out var auth) && auth;

        using var client = new SmtpClient(host, port) { EnableSsl = false };
        if (useAuth)
        {
            client.Credentials = new NetworkCredential(
                _config["Stratix:Mail:Username"],
                _config["Stratix:Mail:Password"]);
        }

        var message = new MailMessage(from, email, "Stratix password reset", $"Reset your password: {resetLink}");
        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}", email);
        }
    }
}
