using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Helpers;

public sealed class FichaTecnicaLeida
{
    public string? DescripcionTecnica { get; init; }
    public string? TipoMaterial { get; init; }
    public string? Composicion { get; init; }
    public decimal? GramajeGm2 { get; init; }
    public decimal? AnchoCm { get; init; }
    public string? Colores { get; init; }
    public string? Acabados { get; init; }
    public string? EspecificacionesUso { get; init; }
    public string? ProcesoFabricacion { get; init; }
    public string? NormasCertificaciones { get; init; }
    public string? EnlaceFichaOriginal { get; init; }
    public string? Observaciones { get; init; }
}

// Validación compartida por la pantalla y el importador.
public static class FichaTecnicaTexto
{
    public static bool TryLeer(GuardarFichaTecnicaMaterialDto dto, out FichaTecnicaLeida datos, out IReadOnlyList<string> errores)
    {
        var lista = new List<string>();
        var descripcion = Texto(dto.DescripcionTecnica, FichaTecnicaMaterial.DescripcionTecnicaMaxLength, "La descripción técnica", lista);
        var tipo = Texto(dto.TipoMaterial, FichaTecnicaMaterial.TipoMaterialMaxLength, "El tipo de tela / material", lista);
        var composicion = Texto(dto.Composicion, FichaTecnicaMaterial.ComposicionMaxLength, "La composición", lista);
        var colores = Texto(dto.Colores, FichaTecnicaMaterial.ColoresMaxLength, "Los colores", lista);
        var acabados = Texto(dto.Acabados, FichaTecnicaMaterial.AcabadosMaxLength, "Los acabados", lista);
        var uso = Texto(dto.EspecificacionesUso, FichaTecnicaMaterial.EspecificacionesUsoMaxLength, "Las especificaciones de uso", lista);
        var proceso = Texto(dto.ProcesoFabricacion, FichaTecnicaMaterial.ProcesoFabricacionMaxLength, "El proceso de fabricación", lista);
        var normas = Texto(dto.NormasCertificaciones, FichaTecnicaMaterial.NormasCertificacionesMaxLength, "Las normas / certificaciones", lista);
        var enlace = Texto(dto.EnlaceFichaOriginal, FichaTecnicaMaterial.EnlaceFichaOriginalMaxLength, "El enlace de la ficha", lista);
        var observaciones = Texto(dto.Observaciones, FichaTecnicaMaterial.ObservacionesMaxLength, "Las observaciones", lista);

        decimal? gramaje = null;
        decimal? ancho = null;
        if (!DecimalTexto.TryParseOpcional(dto.GramajeGm2, "El gramaje", out gramaje, out var errorGramaje))
            lista.Add(errorGramaje!);
        if (!DecimalTexto.TryParseOpcional(dto.AnchoCm, "El ancho", out ancho, out var errorAncho))
            lista.Add(errorAncho!);

        datos = new FichaTecnicaLeida
        {
            DescripcionTecnica = descripcion,
            TipoMaterial = tipo,
            Composicion = composicion,
            GramajeGm2 = gramaje,
            AnchoCm = ancho,
            Colores = colores,
            Acabados = acabados,
            EspecificacionesUso = uso,
            ProcesoFabricacion = proceso,
            NormasCertificaciones = normas,
            EnlaceFichaOriginal = enlace,
            Observaciones = observaciones
        };
        errores = lista;
        return lista.Count == 0;
    }

    private static string? Texto(string? value, int max, string etiqueta, List<string> errores)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var limpio = value.Trim();
        if (limpio.Length > max)
            errores.Add($"{etiqueta} no puede superar {max} caracteres.");
        return limpio;
    }
}
