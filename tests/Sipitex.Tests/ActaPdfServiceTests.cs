using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Reporting;

namespace Sipitex.Tests;

public class ActaPdfServiceTests
{
    [Fact]
    public void Render_GeneraPdfConCabeceraYFirmas()
    {
        var acta = new ActaMovimientoDto(
            1,
            "ACT-0001",
            ActaTipo.Egreso,
            ActaOrigen.Consumo,
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            "Salida a confección",
            4,
            "OP-4",
            null,
            null,
            "Laura Gómez",
            "Instructor",
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            TestPng.Bytes,
            "Pedro Encargado",
            "Encargado de bodega",
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            TestPng.Bytes,
            7,
            "Ana",
            [
                new ActaDetalleDto(1, ActaItemTipo.Material, "Hilo", 5, "metro", 3, 11, null, 4)
            ]);

        var file = new ActaPdfService().Render(acta);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.StartsWith("SIPITEX_ACT-0001_", file.FileName);
        Assert.True(file.Content.Length > 100);
        Assert.Equal((byte)'%', file.Content[0]);
        Assert.Equal((byte)'P', file.Content[1]);
        Assert.Equal((byte)'D', file.Content[2]);
        Assert.Equal((byte)'F', file.Content[3]);
    }

    [Fact]
    public void Render_IncluyeCodigoDeMaterialYCodigoDeVerificacion()
    {
        var acta = new ActaMovimientoDto(
            1,
            "ACT-0009",
            ActaTipo.Egreso,
            ActaOrigen.Consumo,
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            "Salida a confección",
            4,
            "OP-4",
            null,
            null,
            "Laura Gómez",
            "Instructor",
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            TestPng.Bytes,
            "Pedro Encargado",
            "Encargado de bodega",
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            TestPng.Bytes,
            7,
            "Ana",
            [
                new ActaDetalleDto(
                    1, ActaItemTipo.Material, "Hilo", 5, "metro", 3, 11, null, 4,
                    CodigoMaterial: "MAT-3",
                    PlantaInventario: "Planta de Inventario 1")
            ],
            ProductoOrden: "Camisa");

        var file = new ActaPdfService().Render(acta);
        var texto = ExtraerTextoPdf(file.Content);
        var codigo = ActaPdfService.CodigoVerificacion(acta);

        Assert.Equal(12, codigo.Length);
        Assert.Contains("MAT-3", texto, StringComparison.Ordinal);
        Assert.Contains(codigo, texto, StringComparison.Ordinal);
        Assert.Contains("Camisa", texto, StringComparison.Ordinal);
        Assert.Contains("Planta de Inventario 1", texto, StringComparison.Ordinal);
        Assert.Contains("Código de veri", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ActaAnulada_MuestraMarcaYMotivo()
    {
        var acta = new ActaMovimientoDto(
            1,
            "ACT-0002",
            ActaTipo.Egreso,
            ActaOrigen.Manual,
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            null,
            null,
            null,
            null,
            null,
            "Laura",
            "Instructor",
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            TestPng.Bytes,
            "Pedro",
            "Encargado",
            null,
            null,
            7,
            "Ana",
            [new ActaDetalleDto(1, ActaItemTipo.Material, "Tela", 1, "metro", null, null, null, null)],
            Estado: ActaEstado.Anulada,
            PlantaOrigenNombre: "Planta de Inventario 1",
            PlantaDestinoNombre: "Planta de Inventario 2",
            MotivoAnulacion: "Error de destinatario");

        var texto = ExtraerTextoPdf(new ActaPdfService().Render(acta).Content);
        var normalizado = texto.Replace("\0", "");

        Assert.Contains("ANULADA", texto, StringComparison.Ordinal);
        Assert.Contains("Error de des", normalizado, StringComparison.Ordinal);
        Assert.Contains("natario", normalizado, StringComparison.Ordinal);
        Assert.Contains("Planta de Inventario 1", texto, StringComparison.Ordinal);
        Assert.Contains("Planta de Inventario 2", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_PendienteFirma_IndicaFirmaDeQuienRecibe()
    {
        var acta = new ActaMovimientoDto(
            1,
            "ACT-0003",
            ActaTipo.Ingreso,
            ActaOrigen.Manual,
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            null,
            null,
            null,
            null,
            null,
            "Laura",
            "Instructor",
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            TestPng.Bytes,
            "Pedro",
            "Encargado",
            null,
            null,
            7,
            "Ana",
            [new ActaDetalleDto(1, ActaItemTipo.Material, "Tela", 1, "metro", null, null, null, null)],
            Estado: ActaEstado.PendienteFirma);

        var texto = ExtraerTextoPdf(new ActaPdfService().Render(acta).Content);
        Assert.Contains("Firma de quien recibe pendiente", texto, StringComparison.Ordinal);
    }

    private static string ExtraerTextoPdf(byte[] pdf)
    {
        var latin = Encoding.Latin1.GetString(pdf);
        var objetos = new Dictionary<int, string>();
        var cursor = 0;
        while (true)
        {
            var marca = latin.IndexOf(" 0 obj\n", cursor, StringComparison.Ordinal);
            if (marca < 0)
                break;
            var idInicio = marca;
            while (idInicio > 0 && char.IsDigit(latin[idInicio - 1]))
                idInicio--;
            if (!int.TryParse(latin.AsSpan(idInicio, marca - idInicio), out var id))
            {
                cursor = marca + 7;
                continue;
            }

            var cuerpoInicio = marca + " 0 obj\n".Length;
            var fin = latin.IndexOf("endobj", cuerpoInicio, StringComparison.Ordinal);
            if (fin < 0)
                break;
            var cuerpo = latin[cuerpoInicio..fin];
            objetos[id] = cuerpo.Contains("stream", StringComparison.Ordinal)
                ? InflarObjeto(pdf, latin, cuerpoInicio, fin) ?? cuerpo
                : cuerpo;
            cursor = fin + 6;
        }

        var mapas = new Dictionary<int, Dictionary<int, string>>();
        foreach (var (id, cuerpo) in objetos)
        {
            var unicode = Regex.Match(cuerpo, @"/ToUnicode\s+(\d+)\s+0 R");
            if (!unicode.Success || !cuerpo.Contains("/Subtype /Type0", StringComparison.Ordinal))
                continue;
            if (int.TryParse(unicode.Groups[1].Value, out var mapaId) && objetos.TryGetValue(mapaId, out var cmap))
                mapas[id] = LeerCmap(cmap);
        }

        var texto = new StringBuilder();
        foreach (var cuerpo in objetos.Values.Where(c => c.Contains("/Type /Page", StringComparison.Ordinal)))
        {
            var fuentes = new Dictionary<string, Dictionary<int, string>>(StringComparer.Ordinal);
            foreach (Match recurso in Regex.Matches(cuerpo, @"/(F\d+)\s+(\d+)\s+0 R"))
            {
                if (int.TryParse(recurso.Groups[2].Value, out var fuenteId) && mapas.TryGetValue(fuenteId, out var mapa))
                    fuentes["/" + recurso.Groups[1].Value] = mapa;
            }

            var contenidos = Regex.Match(cuerpo, @"/Contents\s+(\d+)\s+0 R");
            if (!contenidos.Success
                || !int.TryParse(contenidos.Groups[1].Value, out var contenidoId)
                || !objetos.TryGetValue(contenidoId, out var contenido))
                continue;

            var fuente = new Dictionary<int, string>();
            var tokens = Regex.Matches(contenido, @"/F\d+|<([0-9A-Fa-f]+)>|Tj");
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i].Value;
                if (token.StartsWith("/F", StringComparison.Ordinal))
                {
                    fuentes.TryGetValue(token, out fuente!);
                    fuente ??= new Dictionary<int, string>();
                    continue;
                }

                if (!token.StartsWith('<') || i + 1 >= tokens.Count || tokens[i + 1].Value != "Tj")
                    continue;
                var hex = token[1..^1];
                for (var g = 0; g + 4 <= hex.Length; g += 4)
                {
                    if (int.TryParse(hex.AsSpan(g, 4), System.Globalization.NumberStyles.HexNumber, null, out var glyph)
                        && fuente.TryGetValue(glyph, out var caracter))
                        texto.Append(caracter);
                }

                i++;
            }
        }

        return texto.ToString();
    }

    private static Dictionary<int, string> LeerCmap(string cmap)
    {
        var mapa = new Dictionary<int, string>();
        foreach (Match bloque in Regex.Matches(cmap, @"beginbfchar(.*?)endbfchar", RegexOptions.Singleline))
        {
            foreach (Match par in Regex.Matches(bloque.Groups[1].Value, @"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>"))
                mapa[Convert.ToInt32(par.Groups[1].Value, 16)] = DecodificarUtf16(par.Groups[2].Value);
        }

        foreach (Match bloque in Regex.Matches(cmap, @"beginbfrange(.*?)endbfrange", RegexOptions.Singleline))
        {
            foreach (Match rango in Regex.Matches(bloque.Groups[1].Value, @"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>"))
            {
                var desde = Convert.ToInt32(rango.Groups[1].Value, 16);
                var hasta = Convert.ToInt32(rango.Groups[2].Value, 16);
                var destino = Convert.ToInt32(rango.Groups[3].Value, 16);
                for (var i = 0; desde + i <= hasta; i++)
                    mapa[desde + i] = char.ConvertFromUtf32(destino + i);
            }
        }

        return mapa;
    }

    private static string DecodificarUtf16(string hex)
    {
        var texto = new StringBuilder();
        for (var i = 0; i + 4 <= hex.Length; i += 4)
            texto.Append(char.ConvertFromUtf32(Convert.ToInt32(hex.Substring(i, 4), 16)));
        return texto.ToString();
    }

    private static string? InflarObjeto(byte[] pdf, string latin, int cuerpoInicio, int fin)
    {
        var stream = latin.IndexOf("stream", cuerpoInicio, StringComparison.Ordinal);
        if (stream < 0 || stream > fin)
            return null;
        var datos = stream + "stream".Length;
        if (datos < latin.Length && latin[datos] == '\r')
            datos++;
        if (datos < latin.Length && latin[datos] == '\n')
            datos++;
        var endstream = latin.IndexOf("endstream", datos, StringComparison.Ordinal);
        if (endstream < 0 || endstream > fin)
            return null;
        var largo = endstream - datos;
        if (largo > 0 && pdf[endstream - 1] == (byte)'\n')
            largo--;
        if (largo > 0 && pdf[datos + largo - 1] == (byte)'\r')
            largo--;
        if (largo <= 0)
            return null;

        try
        {
            using var entrada = new MemoryStream(pdf, datos, largo);
            using var zlib = new ZLibStream(entrada, CompressionMode.Decompress);
            using var salida = new MemoryStream();
            zlib.CopyTo(salida);
            return Encoding.Latin1.GetString(salida.ToArray());
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }
}
