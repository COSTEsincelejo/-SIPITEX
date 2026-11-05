using ClosedXML.Excel;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;

namespace Sipitex.Infrastructure.Import;

internal enum FichaColumna
{
    Codigo,
    Nombre,
    Referencia,
    DescripcionTecnica,
    TipoMaterial,
    Composicion,
    Gramaje,
    Ancho,
    Colores,
    Acabados,
    Especificaciones,
    Proceso,
    Normas,
    Enlace,
    Observaciones
}

internal sealed class FichaTecnicaExcelFila
{
    public int Fila { get; init; }
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Referencia { get; set; }
    public string? DescripcionTecnica { get; set; }
    public string? TipoMaterial { get; set; }
    public string? Composicion { get; set; }
    public bool GramajeVacio { get; set; } = true;
    public double? GramajeNumero { get; set; }
    public string? GramajeTexto { get; set; }
    public bool AnchoVacio { get; set; } = true;
    public double? AnchoNumero { get; set; }
    public string? AnchoTexto { get; set; }
    public string? Colores { get; set; }
    public string? Acabados { get; set; }
    public string? EspecificacionesUso { get; set; }
    public string? ProcesoFabricacion { get; set; }
    public string? NormasCertificaciones { get; set; }
    public string? EnlaceFichaOriginal { get; set; }
    public string? Observaciones { get; set; }
}

internal sealed class FichaTecnicaExcelLectura
{
    public string? Aviso { get; init; }
    public IReadOnlyList<FichaTecnicaExcelFila> Filas { get; init; } = [];
}

// La hoja es opcional. Si no está, la importación de materiales sigue igual.
internal static class FichaTecnicaExcelReader
{
    private readonly record struct AliasEncabezado(string Alias, FichaColumna Columna);

    private static readonly AliasEncabezado[] Aliases =
    [
        new("codigo de almacen", FichaColumna.Codigo),
        new("codigo almacen", FichaColumna.Codigo),
        new("codigo", FichaColumna.Codigo),
        new("nombre del material", FichaColumna.Nombre),
        new("nombre de material", FichaColumna.Nombre),
        new("nombre material", FichaColumna.Nombre),
        new("nombre", FichaColumna.Nombre),
        new("referencia", FichaColumna.Referencia),
        new("descripcion tecnica", FichaColumna.DescripcionTecnica),
        new("descripcion", FichaColumna.DescripcionTecnica),
        new("tipo de tela material", FichaColumna.TipoMaterial),
        new("tipo de tela", FichaColumna.TipoMaterial),
        new("tipo de material", FichaColumna.TipoMaterial),
        new("tipo material", FichaColumna.TipoMaterial),
        new("composicion", FichaColumna.Composicion),
        new("gramaje", FichaColumna.Gramaje),
        new("ancho", FichaColumna.Ancho),
        new("colores disponibles", FichaColumna.Colores),
        new("colores", FichaColumna.Colores),
        new("acabados", FichaColumna.Acabados),
        new("especificaciones de confeccion uso", FichaColumna.Especificaciones),
        new("especificaciones de confeccion", FichaColumna.Especificaciones),
        new("especificaciones de uso", FichaColumna.Especificaciones),
        new("especificaciones", FichaColumna.Especificaciones),
        new("proceso de fabricacion", FichaColumna.Proceso),
        new("proceso fabricacion", FichaColumna.Proceso),
        new("normas certificaciones", FichaColumna.Normas),
        new("normas", FichaColumna.Normas),
        new("certificaciones", FichaColumna.Normas),
        new("imagen o enlace a ficha original", FichaColumna.Enlace),
        new("enlace a ficha original", FichaColumna.Enlace),
        new("enlace", FichaColumna.Enlace),
        new("imagen", FichaColumna.Enlace),
        new("observaciones", FichaColumna.Observaciones)
    ];

    public static FichaTecnicaExcelLectura Leer(XLWorkbook workbook)
    {
        var sheet = workbook.Worksheets
            .FirstOrDefault(w => string.Equals(w.Name, InventarioImportTextos.HojaFichas, StringComparison.OrdinalIgnoreCase));
        if (sheet is null)
            return new FichaTecnicaExcelLectura();

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (lastRow == 0 || lastCol == 0)
            return new FichaTecnicaExcelLectura();

        var headerRow = BuscarEncabezado(sheet, lastRow, lastCol);
        if (headerRow is null)
        {
            return new FichaTecnicaExcelLectura
            {
                Aviso = "No se reconocieron los encabezados de la hoja Fichas_Tecnicas. " +
                        $"Encabezados encontrados: {Listar(sheet, 1, lastCol)}."
            };
        }

        var columnas = Mapear(sheet, headerRow.Value, lastCol);
        var filas = new List<FichaTecnicaExcelFila>();
        for (var row = headerRow.Value + 1; row <= lastRow; row++)
        {
            var fila = new FichaTecnicaExcelFila { Fila = row };
            foreach (var (columna, index) in columnas)
                Asignar(fila, columna, InventarioExcelReader.LeerCelda(sheet.Cell(row, index)));

            if (string.IsNullOrWhiteSpace(fila.Nombre)
                && string.IsNullOrWhiteSpace(fila.Codigo)
                && string.IsNullOrWhiteSpace(fila.Referencia))
                continue;

            filas.Add(fila);
        }

        return new FichaTecnicaExcelLectura { Filas = filas };
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
                    && columna is FichaColumna.Codigo or FichaColumna.Nombre or FichaColumna.Referencia)
                    return row;
            }
        }

        return null;
    }

    private static Dictionary<FichaColumna, int> Mapear(IXLWorksheet sheet, int headerRow, int lastCol)
    {
        var map = new Dictionary<FichaColumna, int>();
        var puntaje = new Dictionary<FichaColumna, int>();
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

    private static bool TryMatch(string key, out FichaColumna columna, out int score)
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

    private static string Listar(IXLWorksheet sheet, int row, int lastCol)
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

        return textos.Count == 0 ? "(ninguno)" : string.Join(" | ", textos);
    }

    private static void Asignar(FichaTecnicaExcelFila fila, FichaColumna columna, CeldaLeida celda)
    {
        switch (columna)
        {
            case FichaColumna.Codigo:
                fila.Codigo = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Nombre:
                fila.Nombre = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Referencia:
                fila.Referencia = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.DescripcionTecnica:
                fila.DescripcionTecnica = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.TipoMaterial:
                fila.TipoMaterial = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Composicion:
                fila.Composicion = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Gramaje:
                fila.GramajeVacio = celda.Vacia;
                fila.GramajeNumero = celda.Numero;
                fila.GramajeTexto = celda.Texto;
                break;
            case FichaColumna.Ancho:
                fila.AnchoVacio = celda.Vacia;
                fila.AnchoNumero = celda.Numero;
                fila.AnchoTexto = celda.Texto;
                break;
            case FichaColumna.Colores:
                fila.Colores = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Acabados:
                fila.Acabados = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Especificaciones:
                fila.EspecificacionesUso = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Proceso:
                fila.ProcesoFabricacion = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Normas:
                fila.NormasCertificaciones = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Enlace:
                fila.EnlaceFichaOriginal = InventarioExcelReader.Texto(celda);
                break;
            case FichaColumna.Observaciones:
                fila.Observaciones = InventarioExcelReader.Texto(celda);
                break;
        }
    }
}
