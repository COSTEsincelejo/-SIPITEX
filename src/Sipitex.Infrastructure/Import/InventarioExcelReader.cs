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

    public static InventarioExcelLectura Fallo(string error) => new() { Error = error };
}

internal readonly record struct CeldaLeida(bool Vacia, string? Texto, double? Numero, DateTime? Fecha)
{
    public static CeldaLeida EnBlanco { get; } = new(true, null, null, null);
    public static CeldaLeida DeTexto(string texto) => new(false, texto, null, null);
    public static CeldaLeida DeNumero(double numero) => new(false, null, numero, null);
    public static CeldaLeida DeFecha(DateTime fecha) => new(false, null, null, fecha);
}

// Lee Inventario_Materiales por nombre de encabezado. Ignora la columna stock y las de fórmula.
internal static class InventarioExcelReader
{
    private static readonly Dictionary<string, InventarioColumna> Encabezados = new(StringComparer.Ordinal)
    {
        ["codigo"] = InventarioColumna.Codigo,
        ["codigo almacen"] = InventarioColumna.Codigo,
        ["codigo de almacen"] = InventarioColumna.Codigo,
        ["categoria"] = InventarioColumna.Categoria,
        ["subcategoria"] = InventarioColumna.Subcategoria,
        ["sub categoria"] = InventarioColumna.Subcategoria,
        ["nombre"] = InventarioColumna.Nombre,
        ["no material"] = InventarioColumna.Referencia,
        ["no de material"] = InventarioColumna.Referencia,
        ["n material"] = InventarioColumna.Referencia,
        ["n de material"] = InventarioColumna.Referencia,
        ["numero material"] = InventarioColumna.Referencia,
        ["numero de material"] = InventarioColumna.Referencia,
        ["nro material"] = InventarioColumna.Referencia,
        ["nro de material"] = InventarioColumna.Referencia,
        ["referencia"] = InventarioColumna.Referencia,
        ["no material referencia"] = InventarioColumna.Referencia,
        ["material referencia"] = InventarioColumna.Referencia,
        ["cantidad"] = InventarioColumna.Cantidad,
        ["unidad"] = InventarioColumna.Unidad,
        ["descripcion"] = InventarioColumna.Descripcion,
        ["fecha de ingreso"] = InventarioColumna.Fecha,
        ["fecha ingreso"] = InventarioColumna.Fecha,
        ["proveedor"] = InventarioColumna.Proveedor,
        ["ubicacion"] = InventarioColumna.Ubicacion,
        ["stock minimo"] = InventarioColumna.MinStock,
        ["stock min"] = InventarioColumna.MinStock,
        ["minimo"] = InventarioColumna.MinStock,
        ["tipo de elemento"] = InventarioColumna.Tipo,
        ["tipo elemento"] = InventarioColumna.Tipo
    };

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
                    $"La hoja {InventarioImportTextos.Hoja} no tiene la columna Nombre.");

            var columnas = MapearColumnas(sheet, headerRow.Value, lastCol);
            if (!columnas.ContainsKey(InventarioColumna.Nombre))
                return InventarioExcelLectura.Fallo(
                    $"La hoja {InventarioImportTextos.Hoja} no tiene la columna Nombre.");

            var filas = new List<InventarioExcelFila>();
            for (var row = headerRow.Value + 1; row <= lastRow; row++)
            {
                var fila = new InventarioExcelFila { Fila = row };
                var alguna = false;
                foreach (var (columna, index) in columnas)
                {
                    var celda = LeerCelda(sheet.Cell(row, index));
                    if (!celda.Vacia)
                        alguna = true;
                    Asignar(fila, columna, celda);
                }

                if (alguna)
                    filas.Add(fila);
            }

            return new InventarioExcelLectura { Filas = filas };
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
                if (TextoNormalizado.Clave(cell.GetString()) == "nombre")
                    return row;
            }
        }

        return null;
    }

    private static Dictionary<InventarioColumna, int> MapearColumnas(IXLWorksheet sheet, int headerRow, int lastCol)
    {
        var map = new Dictionary<InventarioColumna, int>();
        for (var col = 1; col <= lastCol; col++)
        {
            var cell = sheet.Cell(headerRow, col);
            if (cell.HasFormula)
                continue;

            var key = TextoNormalizado.Clave(cell.GetString());
            if (key.Length == 0 || key == "stock" || key == "estado")
                continue;

            if (!TryColumna(key, out var columna))
                continue;

            map.TryAdd(columna, col);
        }

        return map;
    }

    private static bool TryColumna(string key, out InventarioColumna columna)
    {
        if (key.Contains("stock", StringComparison.Ordinal) && key.Contains("min", StringComparison.Ordinal))
        {
            columna = InventarioColumna.MinStock;
            return true;
        }

        return Encabezados.TryGetValue(key, out columna);
    }

    private static CeldaLeida LeerCelda(IXLCell cell)
    {
        if (cell.IsEmpty())
            return CeldaLeida.EnBlanco;

        var value = cell.HasFormula ? cell.CachedValue : cell.Value;
        if (value.IsBlank)
            return CeldaLeida.EnBlanco;
        if (value.IsText)
        {
            var texto = value.GetText().Trim();
            return texto.Length == 0 ? CeldaLeida.EnBlanco : CeldaLeida.DeTexto(texto);
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

    private static string? Texto(CeldaLeida celda)
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
