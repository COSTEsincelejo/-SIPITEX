using System.Net;

namespace Sipitex.Application.Services;

// Correo HTML de un código de 6 dígitos. El código queda solo en su línea
// para que las pruebas puedan leerlo; el cuerpo no lleva enlaces.
public static class VerificationEmailTemplate
{
    public const string ConfirmationSubject = "Código de confirmación SIPITEX";
    public const string PasswordResetSubject = "Código para restablecer contraseña SIPITEX";

    public static string Build(string nombre, string plainCode, string intro, string closing)
    {
        var safeName = WebUtility.HtmlEncode(nombre);
        var safeIntro = WebUtility.HtmlEncode(intro);
        var safeClosing = WebUtility.HtmlEncode(closing);
        var safeCode = WebUtility.HtmlEncode(plainCode);

        return $"""
            <div style="margin:0;padding:24px;background:#DCE6F1;font-family:Arial,Helvetica,sans-serif;">
              <div style="max-width:520px;margin:0 auto;background:#ffffff;border:1px solid #1F3864;">
                <div style="background:#1F3864;color:#ffffff;padding:16px 24px;font-size:20px;letter-spacing:1px;">SIPITEX</div>
                <div style="padding:24px;color:#1F3864;line-height:1.5;">
                  <p>Hola {safeName},</p>
                  <p>{safeIntro}</p>
                  <p style="font-size:32px;font-weight:700;letter-spacing:8px;text-align:center;margin:24px 0;color:#1F3864;">
            {safeCode}
                  </p>
                  <p>Este código vence en 15 minutos y solo puede usarse una vez. Si pide otro, el anterior deja de servir.</p>
                  <p>{safeClosing}</p>
                </div>
              </div>
            </div>
            """;
    }
}
