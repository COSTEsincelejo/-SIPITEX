using System.Globalization;
using ClosedXML.Excel;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;

namespace Sipitex.Infrastructure.Import;

internal enum InventarioColumna
{
    Codigo,
    Categoria,
    Subcategoria,
    Nombre,
    Referencia,
    Cantidad,
    Unidad,
    Descripcion,
    Fecha,
    Proveedor,
    Ubicacion,
    MinStock,
    Tipo
}

internal sealed class InventarioExcelFila
{
    public int Fila { get; init; }
    public string? Codigo { get; set; }
    public string? Categoria { get; set; }
    public string? Subcategoria { get; set; }
    public string? Nombre { get; set; }
    public string? Referencia { get; set; }
    public bool CantidadVacia { get; set; } = true;
    public double? CantidadNumero { get; set; }
    public string? CantidadTexto { get; set; }
    public string? Unidad { get; set; }
    public string? Descripcion { get; set; }
    public bool FechaVacia { get; set; } = true;
    public DateTime? FechaValor { get; set; }
    public double? FechaSerial { get; set; }
    public string? FechaTexto { get; set; }
    public string? Proveedor { get; set; }
    public string? Ubicacion { get; set; }
    public bool MinStockVacio { get; set; } = true;
    public double? MinStockNumero { get; set; }
    public string? MinStockTexto { get; set; }
    public string? TipoElemento { get; set; }
}

internal sealed class InventarioExcelLectura
{
    public string? Error { get; init; }
    public IReadOnlyList<InventarioExcelFila> Filas { get; init; } = [];
    public IReadOnlyList<FichaTecnicaExcelFila> Fichas { get; init; } = [];
    public string? AvisoFichas { get; init; }

    public static InventarioExcelLectura Fallo(string error) => new() { Error = error };
}

internal readonly record struct CeldaLeida(bool Vacia, string? Texto, double? Numero, DateTime? Fecha)
{
    public static CeldaLeida EnBlanco { get; } = new(true, null, null, null);
    public static CeldaLeida DeTexto(string texto) => new(false, texto, null, null);
    public static CeldaLeida DeNumero(double numero) => new(false, null, numero, null);
    public static CeldaLeida DeFecha(DateTime fecha) => new(false, null, null, fecha);
}

// Lee Inventario_Materiales por nombre de encabezado. La columna stock no se lee nunca.
internal static class InventarioExcelReader
{
    private readonly record struct AliasEncabezado(string Alias, InventarioColumna Columna);

    // El match es igualdad o "empieza con" el alias. Los más largos ganan.
    private static readonly AliasEncabezado[] Aliases =
    [
        new("codigo de almacen", InventarioColumna.Codigo),
        new("codigo almacen", InventarioColumna.Codigo),
        new("codigo", InventarioColumna.Codigo),
        new("subcategoria", InventarioColumna.Subcategoria),
        new("sub categoria", InventarioColumna.Subcategoria),
        new("categoria", InventarioColumna.Categoria),
        new("nombre del material", InventarioColumna.Nombre),
        new("nombre de material", InventarioColumna.Nombre),
        new("nombre material", InventarioColumna.Nombre),
        new("nombre", InventarioColumna.Nombre),
        new("no material referencia", InventarioColumna.Referencia),
        new("numero de material", InventarioColumna.Referencia),
        new("numero material", InventarioColumna.Referencia),
        new("no de material", InventarioColumna.Referencia),
        new("no material", InventarioColumna.Referencia),
        new("nro de material", InventarioColumna.Referencia),
        new("nro material", InventarioColumna.Referencia),
        new("n de material", InventarioColumna.Referencia),
        new("n material", InventarioColumna.Referencia),
        new("referencia", InventarioColumna.Referencia),
        new("unidad de medida", InventarioColumna.Unidad),
        new("unidad", InventarioColumna.Unidad),
        new("cantidad", InventarioColumna.Cantidad),
        new("descripcion", InventarioColumna.Descripcion),
        new("fecha de ingreso", InventarioColumna.Fecha),
        new("fecha ingreso", InventarioColumna.Fecha),
        new("proveedor", InventarioColumna.Proveedor),
        new("ubicacion tipo de bodega", InventarioColumna.Ubicacion),
        new("ubicacion", InventarioColumna.Ubicacion),
        new("stock minimo", InventarioColumna.MinStock),
        new("stock min", InventarioColumna.MinStock),
        new("tipo de elemento", InventarioColumna.Tipo),
        new("tipo elemento", InventarioColumna.Tipo)
    ];

    public static InventarioExcelLectura Leer(Stream contenido)
    {
        try
        {
            using var buffer = new MemoryStream();
            contenido.CopyTo(buffer);
            if (buffer.Length == 0)
                return InventarioExcelLectura.Fallo("El archivo está vacío.");
            buffer.Position = 0;

            using var workbook = new XLWorkbook(buffer);
            var sheet = workbook.Worksheets
                .FirstOrDefault(w => string.Equals(w.Name, InventarioImportTextos.Hoja, StringComparison.OrdinalIgnoreCase));
            if (sheet is null)
                return InventarioExcelLectura.Fallo($"No se encontró la hoja {InventarioImportTextos.Hoja}.");

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastRow == 0 || lastCol == 0)
                return InventarioExcelLectura.Fallo($"La hoja {InventarioImportTextos.Hoja} está vacía.");

            var headerRow = BuscarEncabezado(sheet, lastRow, lastCol);
            if (headerRow is null)
                return InventarioExcelLectura.Fallo(
                    "No se encontró la fila de encabezados (se espera la columna de nombre del material). " +
                    $"Encabezados encontrados: {ListarCeldas(sheet, 1, lastCol)}.");

            var encontrados = TextosDeFila(sheet, headerRow.Value, lastCol);
            var columnas = MapearColumnas(sheet, headerRow.Value, lastCol);
            var faltan = Faltantes(columnas);
            if (faltan.Count > 0)
                return InventarioExcelLectura.Fallo(
                    $"Faltan encabezados obligatorios ({string.Join(", ", faltan)}). " +
                    $"Encabezados encontrados: {string.Join(" | ", encontrados)}.");

            var filas = new List<InventarioExcelFila>();
            for (var row = headerRow.Value + 1; row <= lastRow; row++)
            {
                var nombre = LeerCelda(sheet.Cell(row, columnas[InventarioColumna.Nombre]));
                if (string.IsNullOrWhiteSpace(Texto(nombre)))
                    continue;

                var fila = new InventarioExcelFila { Fila = row };
                foreach (var (columna, index) in columnas)
                    Asignar(fila, columna, LeerCelda(sheet.Cell(row, index)));

                filas.Add(fila);
            }

            var fichas = FichaTecnicaExcelReader.Leer(workbook);
            return new InventarioExcelLectura
            {
                Filas = filas,
                Fichas = fichas.Filas,
                AvisoFichas = fichas.Aviso
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return InventarioExcelLectura.Fallo("No se pudo leer el archivo Excel. Verifique que sea un .xlsx válido.");
        }
    }

    private static int? BuscarEncabezado(IXLWorksheet sheet, int lastRow, int lastCol)
    {
        var limite = Math.Min(lastRow, 30);
        for (var row = 1; row <= limite; row++)
        {
            for (var col = 1; col <= lastCol; col++)
            {
                var cell = sheet.Cell(row, col);
                if (cell.HasFormula)
                    continue;
                if (TryMatch(TextoNormalizado.Encabezado(cell.GetString()), out var columna, out _)
                    && columna == InventarioColumna.Nombre)
                    return row;
            }
        }

        return null;
    }

    private static Dictionary<InventarioColumna, int> MapearColumnas(IXLWorksheet sheet, int headerRow, int lastCol)
    {
        var map = new Dictionary<InventarioColumna, int>();
        var puntaje = new Dictionary<InventarioColumna, int>();
        for (var col = 1; col <= lastCol; col++)
        {
            var cell = sheet.Cell(headerRow, col);
            if (cell.HasFormula)
                continue;

            var key = TextoNormalizado.Encabezado(cell.GetString());
            if (!TryMatch(key, out var columna, out var score))
                continue;
            if (puntaje.TryGetValue(columna, out var previo) && previo >= score)
                continue;

            map[columna] = col;
            puntaje[columna] = score;
        }

        return map;
    }

    private static bool TryMatch(string key, out InventarioColumna columna, out int score)
    {
        columna = default;
        score = -1;
        if (key.Length == 0 || key is "stock" or "estado" or "id")
            return false;

        foreach (var alias in Aliases)
        {
            var exacto = key == alias.Alias;
            var prefijo = key.StartsWith(alias.Alias + " ", StringComparison.Ordinal);
            if (!exacto && !prefijo)
                continue;

            var candidato = alias.Alias.Length + (exacto ? 1000 : 0);
            if (candidato <= score)
                continue;

            score = candidato;
            columna = alias.Columna;
        }

        return score >= 0;
    }

    private static List<string> Faltantes(Dictionary<InventarioColumna, int> columnas)
    {
        var faltan = new List<string>();
        if (!columnas.ContainsKey(InventarioColumna.Nombre))
            faltan.Add("nombre");
        if (!columnas.ContainsKey(InventarioColumna.Cantidad))
            faltan.Add("cantidad");
        if (!columnas.ContainsKey(InventarioColumna.Unidad))
            faltan.Add("unidad");
        if (!columnas.ContainsKey(InventarioColumna.Ubicacion))
            faltan.Add("ubicación");
        return faltan;
    }

    private static string ListarCeldas(IXLWorksheet sheet, int row, int lastCol) =>
        string.Join(" | ", TextosDeFila(sheet, row, lastCol));

    private static List<string> TextosDeFila(IXLWorksheet sheet, int row, int lastCol)
    {
        var textos = new List<string>();
        for (var col = 1; col <= lastCol; col++)
        {
            var cell = sheet.Cell(row, col);
            if (cell.HasFormula)
                continue;
            var texto = cell.GetString().Trim();
            if (texto.Length > 0)
                textos.Add(texto);
        }

        return textos.Count == 0 ? ["(ninguno)"] : textos;
    }

    internal static CeldaLeida LeerCelda(IXLCell cell)
    {
        if (cell.IsEmpty() && !cell.HasFormula)
            return CeldaLeida.EnBlanco;

        XLCellValue value;
        try
        {
            value = cell.HasFormula ? cell.CachedValue : cell.Value;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return CeldaLeida.EnBlanco;
        }

        if (value.IsBlank || value.IsError)
            return CeldaLeida.EnBlanco;
        if (value.IsText)
        {
            var texto = value.GetText().Trim();
            if (texto.Length == 0 || texto.Equals("#REF!", StringComparison.OrdinalIgnoreCase))
                return CeldaLeida.EnBlanco;
            return CeldaLeida.DeTexto(texto);
        }

        if (value.IsDateTime)
            return CeldaLeida.DeFecha(value.GetDateTime());
        if (value.IsNumber)
            return CeldaLeida.DeNumero(value.GetNumber());
        if (value.IsBoolean)
            return CeldaLeida.DeTexto(value.GetBoolean() ? "true" : "false");

        var fallback = cell.GetString().Trim();
        return fallback.Length == 0 ? CeldaLeida.EnBlanco : CeldaLeida.DeTexto(fallback);
    }

    private static void Asignar(InventarioExcelFila fila, InventarioColumna columna, CeldaLeida celda)
    {
        switch (columna)
        {
            case InventarioColumna.Codigo:
                fila.Codigo = Texto(celda);
                break;
            case InventarioColumna.Categoria:
                fila.Categoria = Texto(celda);
                break;
            case InventarioColumna.Subcategoria:
                fila.Subcategoria = Texto(celda);
                break;
            case InventarioColumna.Nombre:
                fila.Nombre = Texto(celda);
                break;
            case InventarioColumna.Referencia:
                fila.Referencia = Texto(celda);
                break;
            case InventarioColumna.Cantidad:
                fila.CantidadVacia = celda.Vacia;
                fila.CantidadNumero = celda.Numero;
                fila.CantidadTexto = celda.Texto;
                break;
            case InventarioColumna.Unidad:
                fila.Unidad = Texto(celda);
                break;
            case InventarioColumna.Descripcion:
                fila.Descripcion = Texto(celda);
                break;
            case InventarioColumna.Fecha:
                fila.FechaVacia = celda.Vacia;
                fila.FechaValor = celda.Fecha;
                fila.FechaSerial = celda.Fecha is null ? celda.Numero : null;
                fila.FechaTexto = celda.Texto;
                break;
            case InventarioColumna.Proveedor:
                fila.Proveedor = Texto(celda);
                break;
            case InventarioColumna.Ubicacion:
                fila.Ubicacion = Texto(celda);
                break;
            case InventarioColumna.MinStock:
                fila.MinStockVacio = celda.Vacia;
                fila.MinStockNumero = celda.Numero;
                fila.MinStockTexto = celda.Texto;
                break;
            case InventarioColumna.Tipo:
                fila.TipoElemento = Texto(celda);
                break;
        }
    }

    internal static string? Texto(CeldaLeida celda)
    {
        if (celda.Vacia)
            return null;
        if (!string.IsNullOrWhiteSpace(celda.Texto))
            return celda.Texto.Trim();
        if (celda.Fecha is DateTime fecha)
            return fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        if (celda.Numero is double numero)
            return NumeroComoTexto(numero);
        return null;
    }

    private static string NumeroComoTexto(double numero)
    {
        if (double.IsFinite(numero)
            && numero == Math.Truncate(numero)
            && numero >= long.MinValue
            && numero <= long.MaxValue)
            return ((long)numero).ToString(CultureInfo.InvariantCulture);

        return numero.ToString(CultureInfo.InvariantCulture);
    }
}
