using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sipitex.Infrastructure.Email;

namespace Sipitex.Tests;

public class EmailSenderTests
{
    [Fact]
    public void IsSmtpConfigured_RequiereEnabledHostFromYUser()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = true,
            Host = "smtp.gmail.com",
            From = "sipitex@local",
            User = "user@smtp"
        }), NullLogger<EmailSender>.Instance);

        Assert.True(sender.IsSmtpConfigured);
    }

    [Fact]
    public void IsSmtpConfigured_SinUser_UsaOutbox()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = true,
            Host = "smtp.gmail.com",
            From = "sipitex@local",
            User = ""
        }), NullLogger<EmailSender>.Instance);

        Assert.False(sender.IsSmtpConfigured);
    }

    [Fact]
    public void IsSmtpConfigured_Disabled_FalsoAunqueHayaUser()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = false,
            Host = "smtp.gmail.com",
            From = "sipitex@local",
            User = "user@smtp"
        }), NullLogger<EmailSender>.Instance);

        Assert.False(sender.IsSmtpConfigured);
    }
}
