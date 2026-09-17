namespace Sipitex.Domain.Entities;

// Destino del código de 6 dígitos enviado por correo (verificación o reset).
public static class AuthCodePurposes
{
    public const string EmailVerification = "EmailVerification";
    public const string PasswordReset = "PasswordReset";
}
