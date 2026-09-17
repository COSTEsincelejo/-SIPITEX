using Moq;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

internal static class AuthCodeTestSupport
{
    internal sealed class NoopGuard : IAuthCodeRequestGuard
    {
        public bool IsLimited(string? ipAddress, string purpose) => false;
        public void Record(string? ipAddress, string purpose) { }
    }

    internal static void BindTokens(Mock<IPasswordResetTokenRepository> tokens, List<PasswordResetToken> store)
    {
        tokens.Setup(t => t.AddAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .Callback<PasswordResetToken, CancellationToken>((token, _) =>
            {
                token.Id = store.Count + 1;
                store.Add(token);
            })
            .Returns(Task.CompletedTask);

        tokens.Setup(t => t.GetUnusedByUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string purpose, CancellationToken _) =>
                store.Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAtUtc is null).ToList());

        tokens.Setup(t => t.CountCreatedSinceAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string purpose, DateTime since, CancellationToken _) =>
                store.Count(t => t.UserId == userId && t.Purpose == purpose && t.CreatedAtUtc >= since));

        tokens.Setup(t => t.FindLatestUnusedAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string purpose, CancellationToken _) =>
                store
                    .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAtUtc is null)
                    .OrderByDescending(t => t.CreatedAtUtc)
                    .FirstOrDefault());

        tokens.Setup(t => t.FindValidAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int userId, string purpose, string hash, DateTime now, CancellationToken _) =>
                store.FirstOrDefault(t =>
                    t.UserId == userId
                    && t.Purpose == purpose
                    && t.TokenHash == hash
                    && t.UsedAtUtc is null
                    && t.ExpiresAtUtc > now));

        tokens.Setup(t => t.Update(It.IsAny<PasswordResetToken>()));
    }

    internal static string ExtractCode(string? body)
    {
        Assert.False(string.IsNullOrWhiteSpace(body));
        var match = System.Text.RegularExpressions.Regex.Match(body!, @"Código:\s*(\d{6})");
        Assert.True(match.Success, "El correo debe incluir un código de 6 dígitos, no un enlace.");
        return match.Groups[1].Value;
    }
}
