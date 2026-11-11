using ClosedXML.Excel;
using Sipitex.Application.DTOs;

namespace Sipitex.Infrastructure.Import;

/// <summary>
/// Genera la plantilla de inventario CMTC (hoja Inventario_Materiales) con encabezados
/// que InventarioExcelReader acepta y filas de ejemplo de productos Cofia/Tula.
/// </summary>
public static class InventarioPlantillaExcel
{
    public const string FileName = "Plantilla_Inventario_SIPITEX_Corregida_v2.xlsx";
    public const string PlantaEjemplo = "Planta de Inventario 1";

    private static readonly string[] Encabezados =
    [
        "Código",
        "Categoría",
        "Subcategoría",
        "Nombre del material",
        "Referencia",
        "Cantidad",
        "Unidad de medida",
        "Descripción",
        "Fecha de ingreso",
        "Proveedor",
        "Ubicación",
        "Stock mínimo",
        "Tipo de elemento",
        "Costo unitario"
    ];

    // Nombres exactos de CmtcBomCatalogSeed (Cofia + Tula). Stock > 0 y costo para carga inicial.
    private static readonly (string Nombre, string Unidad, double Cantidad, double Minimo, double Costo, string Categoria)[] FilasEjemplo =
    [
        ("Malla 100% poliéster", "metros", 50, 5, 8500, "Telas"),
        ("Elástico crochet 1cm", "metros", 40, 5, 1200, "Avíos"),
        ("Hilo pespuntes", "metros", 200, 20, 350, "Hilos"),
        ("Hilaza amarre y filete", "metros", 180, 20, 280, "Hilos"),
        ("Lona 90% algodón / 10% poliéster", "metros", 60, 5, 9200, "Telas"),
    ];

    public static byte[] Crear()
    {
        using var stream = new MemoryStream();
        Escribir(stream);
        return stream.ToArray();
    }

    public static void Escribir(Stream destino)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet(InventarioImportTextos.Hoja);

        for (var i = 0; i < Encabezados.Length; i++)
            ws.Cell(1, i + 1).Value = Encabezados[i];

        ws.Row(1).Style.Font.Bold = true;

        var hoy = DateTime.Today;
        for (var i = 0; i < FilasEjemplo.Length; i++)
        {
            var (nombre, unidad, cantidad, minimo, costo, categoria) = FilasEjemplo[i];
            var fila = i + 2;
            ws.Cell(fila, 2).Value = categoria;
            ws.Cell(fila, 4).Value = nombre;
            ws.Cell(fila, 6).Value = cantidad;
            ws.Cell(fila, 7).Value = unidad;
            ws.Cell(fila, 8).Value = "Ejemplo CMTC — ajustar cantidades reales por planta";
            ws.Cell(fila, 9).Value = hoy;
            ws.Cell(fila, 9).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(fila, 11).Value = PlantaEjemplo;
            ws.Cell(fila, 12).Value = minimo;
            ws.Cell(fila, 13).Value = "Consumible";
            ws.Cell(fila, 14).Value = costo;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(destino);
        if (destino.CanSeek)
            destino.Position = 0;
    }

    public static void GuardarEn(string rutaArchivo)
    {
        var dir = Path.GetDirectoryName(rutaArchivo);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var fs = File.Create(rutaArchivo);
        Escribir(fs);
    }
}
