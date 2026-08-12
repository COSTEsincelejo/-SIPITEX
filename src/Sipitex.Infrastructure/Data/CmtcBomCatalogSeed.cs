using Microsoft.EntityFrameworkCore;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Data;

/// <summary>
/// Seed idempotente de materiales y fichas técnicas CMTC (consumos oficiales).
/// Materiales nuevos: Stock=0 y MinStock=0 — ajustar después con cantidades reales de bodega.
/// No modifica Camisa/Pantalón ni asigna instructores.
/// </summary>
internal static class CmtcBomCatalogSeed
{
    // Prefijo de código para materiales creados por este seed (fácil de filtrar en inventario).
    private const string MaterialCodePrefix = "cmtc-pendiente-";

    private const string BomNotes =
        "Ficha técnica CMTC (Ficha Técnica Insumos - Producción). " +
        "Materiales nuevos del catálogo con Stock=0 y MinStock=0 pendientes de ajuste en bodega.";

    // Catálogo de insumos: nombre exacto + unidad. Stock/min siempre 0 al crear.
    private static readonly (string Name, MaterialUnit Unit)[] Materials =
    [
        ("Tela Orión 100% poliéster", MaterialUnit.Metros),
        ("Forro Brioni poliéster", MaterialUnit.Metros),
        ("Guata #200 100% poliéster", MaterialUnit.Metros),
        ("Entretela tejida 100% algodón (ribetes)", MaterialUnit.Metros),
        ("Marquilla talla", MaterialUnit.Unidades),
        ("Cremallera diente nylon separable #5", MaterialUnit.Metros),
        ("Cremallera diente nylon fija 15cm", MaterialUnit.Unidades),
        ("Hilo pespuntes", MaterialUnit.Metros),
        ("Tela Piqué-Lacoste 30% algodón / 70% poliéster", MaterialUnit.Metros),
        ("Entretela 32% termoadhesiva / 68% poliamida-viscosa (pechera)", MaterialUnit.Metros),
        ("Hiladillo poliéster ancho 10mm", MaterialUnit.Metros),
        ("Botón pasta 4 orificios 18 líneas", MaterialUnit.Unidades),
        ("Hilaza amarre y filete", MaterialUnit.Metros),
        ("Paño 100% lana australiana", MaterialUnit.Metros),
        ("Tela Brioni 100% poliéster", MaterialUnit.Metros),
        ("Cremallera diente metálico", MaterialUnit.Unidades),
        ("Botón 4 huecos 24L", MaterialUnit.Unidades),
        ("Hilo pespuntes y presillas", MaterialUnit.Metros),
        ("Drill Vulcano 65% algodón / 35% poliéster", MaterialUnit.Metros),
        ("Cremallera diente nylon separable", MaterialUnit.Metros),
        ("Malla 100% poliéster", MaterialUnit.Metros),
        ("Elástico crochet 1cm", MaterialUnit.Metros),
        ("No tejido Briony 100% polipropileno", MaterialUnit.Metros),
        ("Hilo pespuntes/amarre", MaterialUnit.Metros),
        ("Lona 90% algodón / 10% poliéster", MaterialUnit.Metros),
    ];

    // Fichas: ProductName, Referencia, líneas (nombre material, qty por unidad).
    private static readonly (string ProductName, string Referencia, (string MaterialName, decimal Qty)[] Lines)[] Products =
    [
        (
            "Chaleco COPASST Femenino",
            "C-03-004-334",
            [
                ("Tela Orión 100% poliéster", 1.65m),
                ("Forro Brioni poliéster", 1.5m),
                ("Guata #200 100% poliéster", 1.5m),
                ("Entretela tejida 100% algodón (ribetes)", 0.02m),
                ("Marquilla talla", 1m),
                ("Cremallera diente nylon separable #5", 0.65m),
                ("Cremallera diente nylon fija 15cm", 1m),
                ("Hilo pespuntes", 45.30m),
            ]
        ),
        (
            "Camiseta Polo Infantil",
            "C-05-004-329",
            [
                ("Tela Piqué-Lacoste 30% algodón / 70% poliéster", 0.52m),
                ("Entretela 32% termoadhesiva / 68% poliamida-viscosa (pechera)", 0.12m),
                ("Marquilla talla", 1m),
                ("Hiladillo poliéster ancho 10mm", 0.36m),
                ("Botón pasta 4 orificios 18 líneas", 2m),
                ("Hilo pespuntes", 21.78m),
                ("Hilaza amarre y filete", 34.07m),
            ]
        ),
        (
            "Pantalón Colegial Paño",
            "C-05-004-325",
            [
                ("Paño 100% lana australiana", 0.9m),
                ("Tela Brioni 100% poliéster", 0.22m),
                ("Marquilla talla", 1m),
                ("Cremallera diente metálico", 1m),
                ("Botón 4 huecos 24L", 1m),
                ("Hilo pespuntes y presillas", 27.62m),
                ("Hilaza amarre y filete", 35.71m),
            ]
        ),
        (
            "Chaleco Aprendiz",
            "C-02-004-303",
            [
                ("Drill Vulcano 65% algodón / 35% poliéster", 0.78m),
                ("Marquilla talla", 1m),
                ("Cremallera diente nylon separable", 0.4m),
                ("Hilo pespuntes", 62.78m),
                ("Hilaza amarre y filete", 45.72m),
            ]
        ),
        (
            "Cofia",
            "C-01-001-021",
            [
                ("Malla 100% poliéster", 0.14m),
                ("Elástico crochet 1cm", 0.46m),
                ("Hilo pespuntes", 2.00m),
                ("Hilaza amarre y filete", 0.46m),
            ]
        ),
        (
            "Bolso Ecológico Briony",
            "C-01-001-026",
            [
                ("No tejido Briony 100% polipropileno", 0.35m),
                ("Hilo pespuntes/amarre", 5.33m),
            ]
        ),
        (
            "Tula",
            "C-01-001-020",
            [
                ("Lona 90% algodón / 10% poliéster", 0.42m),
                ("Hilo pespuntes", 12.85m),
                ("Hilaza amarre y filete", 17.14m),
            ]
        ),
    ];

    public static async Task EnsureAsync(SipitexDbContext context)
    {
        var materialsByName = await EnsureMaterialsAsync(context);
        await EnsureBomProductsAsync(context, materialsByName);
    }

    private static async Task<Dictionary<string, Material>> EnsureMaterialsAsync(SipitexDbContext context)
    {
        var existing = await context.Materials.ToListAsync();
        var byName = existing.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

        var nextSeq = existing
            .Where(m => m.Code.StartsWith(MaterialCodePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(m =>
            {
                var suffix = m.Code[MaterialCodePrefix.Length..];
                return int.TryParse(suffix, out var n) ? n : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        var created = false;
        foreach (var (name, unit) in Materials)
        {
            if (byName.ContainsKey(name))
                continue;

            nextSeq++;
            var material = new Material
            {
                // Código con prefijo claro: stock/min en 0 — completar en bodega.
                Code = $"{MaterialCodePrefix}{nextSeq:D3}",
                Name = name,
                Unit = unit,
                Stock = 0m,
                MinStock = 0m,
                Status = MaterialStatus.Bueno,
                LastEntryDate = DateOnly.FromDateTime(DateTime.Today)
            };
            context.Materials.Add(material);
            byName[name] = material;
            created = true;
        }

        if (created)
            await context.SaveChangesAsync();

        return byName;
    }

    private static async Task EnsureBomProductsAsync(
        SipitexDbContext context,
        IReadOnlyDictionary<string, Material> materialsByName)
    {
        foreach (var (productName, referencia, lines) in Products)
        {
            // Idempotente: no tocar si ya existe por referencia o por nombre.
            var exists = await context.BomProducts.AnyAsync(p =>
                p.Referencia == referencia
                || p.ProductName == productName);
            if (exists)
                continue;

            var product = new BomProduct
            {
                ProductName = productName,
                Referencia = referencia,
                IsReference = false,
                HabilitadoParaOrdenes = true,
                Notes = BomNotes
            };
            context.BomProducts.Add(product);
            await context.SaveChangesAsync();

            foreach (var (materialName, qty) in lines)
            {
                if (!materialsByName.TryGetValue(materialName, out var material))
                    throw new InvalidOperationException(
                        $"Material CMTC no encontrado para BOM '{productName}': '{materialName}'.");

                context.BomItems.Add(new BomItem
                {
                    BomProductId = product.Id,
                    ProductName = productName,
                    MaterialId = material.Id,
                    QuantityPerUnit = qty,
                    Unit = material.Unit
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
