using System.Reflection;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Import;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

public class InventarioImportTests
{
    [Fact]
    public async Task Simulacion_NoEscribeEnBaseDeDatos()
    {
        await using var db = await CreateDbAsync();
        var antesMateriales = await db.Materials.CountAsync();
        var antesPlantas = await db.PlantasInventario.CountAsync();
        var service = new InventarioImportService(db, new UnitOfWork(db));

        await using var archivo = LibroSimulacion();
        var resultado = await service.SimularAsync(archivo);

        Assert.True(resultado.ArchivoValido, resultado.ErrorArchivo);
        Assert.False(resultado.Persistido);
        Assert.Equal(0, resultado.Importados);
        Assert.Equal(antesMateriales, await db.Materials.CountAsync());
        Assert.Equal(antesPlantas, await db.PlantasInventario.CountAsync());
        Assert.Equal(0, await db.StockMovements.CountAsync());
        Assert.Equal(0, await db.ActivityLogs.CountAsync());
        Assert.False(db.ChangeTracker.HasChanges());

        Assert.Equal(8, resultado.Validas.Count);
        Assert.Empty(resultado.Omitidas);

        var jersey = Unica(resultado, "Jersey");
        Assert.Equal("ALM-1", jersey.CodigoAlmacen);
        Assert.Equal("Telas", jersey.Categoria);
        Assert.Equal("Punto", jersey.Subcategoria);
        Assert.Equal("REF-1", jersey.Referencia);
        Assert.Equal("Rollo azul", jersey.Descripcion);
        Assert.Equal("Textiles", jersey.Proveedor);
        Assert.Equal(10m, jersey.Stock);
        Assert.Equal(2m, jersey.MinStock);
        Assert.Equal(MaterialUnit.Metros, jersey.Unidad);
        Assert.Equal(new DateOnly(2026, 11, 5), jersey.FechaIngreso);
        Assert.Equal("Planta de Inventario 1", jersey.Planta);
        Assert.False(jersey.PlantaNueva);
        Assert.Equal(TipoElemento.Consumible, jersey.TipoElemento);
        Assert.Contains(InventarioImportTextos.CodigoRepetido, jersey.Advertencias);

        var hilo = Unica(resultado, "Hilo suelto");
        Assert.Null(hilo.CodigoAlmacen);
        Assert.Equal(3.25m, hilo.Stock);
        Assert.Equal(0m, hilo.MinStock);
        Assert.Equal(MaterialUnit.Kg, hilo.Unidad);
        Assert.Equal(TipoElemento.Consumible, hilo.TipoElemento);
        Assert.Contains(InventarioImportTextos.SinCodigo, hilo.Advertencias);

        var botones = Unica(resultado, "Botones");
        Assert.Equal(MaterialUnit.Unidades, botones.Unidad);
        Assert.NotEqual(MaterialUnit.Und, botones.Unidad);
        Assert.Contains(InventarioImportTextos.CodigoRepetido, botones.Advertencias);

        var pano = Unica(resultado, "Paño demo");
        Assert.Equal(MaterialUnit.Pano, pano.Unidad);
        Assert.Equal(DateOnly.FromDateTime(DateTime.FromOADate(30000)), pano.FechaIngreso);
        Assert.Equal(1.5m, pano.MinStock);
        Assert.Equal("Bodega piloto", pano.Planta);
        Assert.True(pano.PlantaNueva);
        Assert.Equal(TipoElemento.Devolutivo, pano.TipoElemento);

        var gruesa = Unica(resultado, "Gruesa demo");
        Assert.Equal(MaterialUnit.Gruesa, gruesa.Unidad);
        Assert.Equal(new DateOnly(2024, 3, 15), gruesa.FechaIngreso);
        Assert.Equal("Bodega piloto", gruesa.Planta);

        var dm = Unica(resultado, "Decimetro demo");
        Assert.Equal(MaterialUnit.Decimetro, dm.Unidad);
        Assert.Equal(new DateOnly(2024, 1, 5), dm.FechaIngreso);
        Assert.Equal("Planta de Inventario 1", dm.Planta);
        Assert.False(dm.PlantaNueva);

        Assert.Equal(MaterialUnit.Paquete, Unica(resultado, "Paquete demo").Unidad);
        Assert.Equal(MaterialUnit.Metros, Unica(resultado, "Metro corto").Unidad);

        Assert.Contains(resultado.Advertencias, a => a.Mensaje == InventarioImportTextos.SinUnidad);
        Assert.DoesNotContain(resultado.Validas, f => f.Nombre == "Cinta");
        Assert.DoesNotContain(resultado.Errores, f => f.Nombre == "Cinta");
        Assert.Equal(3, resultado.Errores.Count);
        Assert.Contains(resultado.Errores, e => e.Nombre == "Cierre" && e.Mensaje.Contains("Unidad desconocida", StringComparison.Ordinal));
        Assert.Contains(resultado.Errores, e => e.Nombre == "Negativo" && e.Mensaje.Contains("no puede ser negativa", StringComparison.Ordinal));
        Assert.Contains(resultado.Errores, e => e.Nombre == "Fecha mala" && e.Mensaje.Contains("fecha de ingreso", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UnidadInvalida_EsErrorDeFila_YNoEscribe()
    {
        await using var db = await CreateDbAsync();
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = Libro(ws =>
        {
            Encabezados(ws);
            Fila(ws, 2, nombre: "Cierre", unidad: "cajas", codigo: "ALM-3", cantidad: 1, ubicacion: "Planta de Inventario 1");
        });

        var resultado = await service.SimularAsync(archivo);

        Assert.True(resultado.ArchivoValido, resultado.ErrorArchivo);
        Assert.Empty(resultado.Validas);
        var error = Assert.Single(resultado.Errores);
        Assert.Equal(2, error.Fila);
        Assert.Contains("Unidad desconocida", error.Mensaje, StringComparison.Ordinal);
        Assert.Contains("cajas", error.Mensaje, StringComparison.Ordinal);
        Assert.Equal(0, await db.Materials.CountAsync());
        Assert.False(db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task Confirmar_OmiteDuplicadoPorCodigoNombreYPlanta()
    {
        await using var db = await CreateDbAsync();
        var user = new User
        {
            Nombre = "Admin Import",
            Email = $"admin-{Guid.NewGuid():N}@test.local",
            PasswordHash = "x",
            Rol = UserRoles.Administrador,
            EmailConfirmed = true
        };
        db.Users.Add(user);
        db.Materials.Add(new Material
        {
            Code = "mat-existente",
            CodigoAlmacen = "ALM-9",
            Name = "Tela cruda",
            Unit = MaterialUnit.Metros,
            Stock = 3,
            MinStock = 1,
            PlantaInventarioId = 1
        });
        await db.SaveChangesAsync();
        var service = new InventarioImportService(db, new UnitOfWork(db));

        await using var primero = LibroConfirmacion();
        var resultado = await service.ConfirmarAsync(primero, user.Id);

        Assert.True(resultado.ArchivoValido, resultado.ErrorArchivo);
        Assert.True(resultado.Persistido);
        Assert.Equal(1, resultado.Importados);
        var omitida = Assert.Single(resultado.Omitidas);
        Assert.Equal("Tela cruda", omitida.Nombre);
        Assert.Equal("ALM-9", omitida.CodigoAlmacen);
        Assert.Contains(InventarioImportTextos.CodigoRepetido, resultado.Advertencias.Select(a => a.Mensaje));

        var materiales = await db.Materials.OrderBy(m => m.Id).ToListAsync();
        Assert.Equal(2, materiales.Count);
        var existente = materiales[0];
        Assert.Equal(3m, existente.Stock);
        Assert.Equal("mat-existente", existente.Code);

        var nuevo = materiales[1];
        Assert.Equal("Botón nácar", nuevo.Name);
        Assert.Equal("ALM-9", nuevo.CodigoAlmacen);
        Assert.Equal("REF-B", nuevo.Referencia);
        Assert.Equal("Avíos", nuevo.Categoria);
        Assert.Equal("Botones", nuevo.Subcategoria);
        Assert.Equal("Bolsa", nuevo.Descripcion);
        Assert.Equal("Hilos SA", nuevo.Proveedor);
        Assert.Equal(4m, nuevo.Stock);
        Assert.Equal(0m, nuevo.MinStock);
        Assert.Equal(MaterialUnit.Paquete, nuevo.Unit);
        Assert.Equal(TipoElemento.Devolutivo, nuevo.TipoElemento);
        Assert.Equal(MaterialStatus.Bueno, nuevo.Status);
        Assert.Equal(new DateOnly(2024, 3, 15), nuevo.LastEntryDate);
        Assert.Equal(0m, nuevo.CostoAdquisicion);
        Assert.Equal(0m, nuevo.CostoPromedioPonderado);
        Assert.Equal(4m, nuevo.StockSinCosto);
        Assert.Matches(@"^mat\d+$", nuevo.Code);
        Assert.NotEqual(existente.Code, nuevo.Code);

        var plantaNueva = await db.PlantasInventario.SingleAsync(p => p.Nombre == "Bodega piloto");
        Assert.Equal(plantaNueva.Id, nuevo.PlantaInventarioId);
        Assert.Equal(3, await db.PlantasInventario.CountAsync());

        var movimiento = Assert.Single(await db.StockMovements.ToListAsync());
        Assert.Equal(nuevo.Id, movimiento.MaterialId);
        Assert.Equal(user.Id, movimiento.UsuarioId);
        Assert.Equal(StockMovementType.Entrada, movimiento.TipoMovimiento);
        Assert.Equal(StockEntryOrigin.OtraFuenteAutorizada, movimiento.Origen);
        Assert.Equal(4m, movimiento.Cantidad);
        Assert.Equal(4m, movimiento.StockResultante);
        Assert.Equal(0m, movimiento.CostoUnitario);
        Assert.True(movimiento.SinCosto);
        Assert.Equal(InventarioImportTextos.ReferenciaMovimiento, movimiento.Referencia);

        var log = Assert.Single(await db.ActivityLogs.ToListAsync());
        Assert.Equal(ActivityLogActions.ImportInventario, log.Action);
        Assert.Equal(ActivityLogEntities.Material, log.Entity);
        Assert.Equal(nuevo.Id.ToString(), log.EntityId);
        Assert.Equal(user.Id, log.UserId);
        Assert.Contains("Importación inicial", log.Details, StringComparison.Ordinal);

        await using var segundo = LibroConfirmacion();
        var repetido = await service.ConfirmarAsync(segundo, user.Id);
        Assert.Equal(0, repetido.Importados);
        Assert.Equal(2, repetido.Omitidas.Count);
        Assert.Equal(2, await db.Materials.CountAsync());
        Assert.Equal(1, await db.StockMovements.CountAsync());
        Assert.Equal(3m, (await db.Materials.SingleAsync(m => m.Code == "mat-existente")).Stock);
    }

    [Fact]
    public void Importar_SoloAdministrador_YPostConAntiforgery()
    {
        var metodos = typeof(InventarioController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.Name is nameof(InventarioController.Importar) or nameof(InventarioController.Confirmar))
            .ToList();

        Assert.Equal(3, metodos.Count);
        Assert.All(metodos, metodo =>
        {
            var auth = metodo.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(auth);
            Assert.Equal(UserRoles.Administrador, auth!.Roles);
        });

        var posts = metodos.Where(m => m.GetCustomAttribute<HttpPostAttribute>() is not null).ToList();
        Assert.Equal(2, posts.Count);
        Assert.All(posts, metodo => Assert.NotNull(metodo.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
    }

    [Fact]
    public async Task PlantillaCmtc_EncabezadosLargos_FilasVacias_Decimal_YDuplicado()
    {
        Assert.Equal(5, (int)MaterialUnit.Caja);
        Assert.Equal(11, (int)MaterialUnit.Libra);

        await using var db = await CreateDbAsync();
        db.PlantasInventario.Add(new PlantaInventario { Nombre = "Centro logístico CMTC", Activo = true });
        var user = new User
        {
            Nombre = "Admin CMTC",
            Email = $"cmtc-{Guid.NewGuid():N}@test.local",
            PasswordHash = "x",
            Rol = UserRoles.Administrador,
            EmailConfirmed = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var plantasAntes = await db.PlantasInventario.CountAsync();
        var service = new InventarioImportService(db, new UnitOfWork(db));

        await using var archivo = LibroPlantillaReal();
        var sim = await service.SimularAsync(archivo);

        Assert.True(sim.ArchivoValido, sim.ErrorArchivo);
        Assert.False(sim.Persistido);
        Assert.Equal(2, sim.Validas.Count);
        Assert.Equal(3, sim.Omitidas.Count);
        Assert.Equal(4, sim.Advertencias.Count);
        Assert.Empty(sim.Errores);
        Assert.Equal(0, await db.Materials.CountAsync());
        Assert.Equal(plantasAntes, await db.PlantasInventario.CountAsync());

        var tela = Unica(sim, "Tela decimal");
        Assert.Equal(65.4m, tela.Stock);
        Assert.Equal(0m, tela.MinStock);
        Assert.Equal(MaterialUnit.Caja, tela.Unidad);
        Assert.Equal(TipoElemento.Consumible, tela.TipoElemento);
        Assert.Equal("ALM-65", tela.CodigoAlmacen);
        Assert.Equal("REF-65", tela.Referencia);
        Assert.Equal("Centro logístico CMTC", tela.Planta);
        Assert.False(tela.PlantaNueva);
        Assert.Contains(InventarioImportTextos.CodigoRepetido, tela.Advertencias);

        var hilo = Unica(sim, "Hilo libra");
        Assert.Equal(MaterialUnit.Libra, hilo.Unidad);
        Assert.Equal(4m, hilo.MinStock);
        Assert.Equal(TipoElemento.Devolutivo, hilo.TipoElemento);
        Assert.Equal("Centro logístico CMTC", hilo.Planta);

        var duplicado = Assert.Single(sim.Advertencias, a => a.Mensaje.Contains("repite", StringComparison.Ordinal));
        Assert.Contains("fila 4", duplicado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fila 2", duplicado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, sim.Advertencias.Count(a => a.Mensaje == InventarioImportTextos.PorFlujoOrdenes));
        Assert.Contains(sim.Omitidas, o => o.Nombre == "Pieza en curso");
        Assert.Contains(sim.Omitidas, o => o.Nombre == "Camisa lista");
        Assert.DoesNotContain(sim.Validas, f => f.Planta == "Bodega de proceso");

        archivo.Position = 0;
        var confirmado = await service.ConfirmarAsync(archivo, user.Id);
        Assert.Equal(2, confirmado.Importados);
        Assert.Equal(3, confirmado.Omitidas.Count);

        var guardada = Assert.Single(await db.Materials.ToListAsync(), m => m.Name == "Tela decimal");
        Assert.Equal(65.4m, guardada.Stock);
        Assert.Equal(0m, guardada.MinStock);
        Assert.Equal(MaterialUnit.Caja, guardada.Unit);
        var movimiento = Assert.Single(await db.StockMovements.Where(m => m.MaterialId == guardada.Id).ToListAsync());
        Assert.Equal(65.4m, movimiento.Cantidad);
        Assert.Equal(65.4m, movimiento.StockResultante);

        var nombres = await db.PlantasInventario.Select(p => p.Nombre).ToListAsync();
        Assert.Equal(plantasAntes, nombres.Count);
        Assert.Contains("Centro logístico CMTC", nombres);
        Assert.DoesNotContain(nombres, n => n.Contains("proceso", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, n => n.Contains("terminado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FaltanEncabezadosObligatorios_LaSimulacionSeDetiene()
    {
        await using var db = await CreateDbAsync();
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = Libro(ws =>
        {
            ws.Cell(1, 1).Value = "id";
            ws.Cell(1, 2).Value = "nombre del material";
            ws.Cell(1, 3).Value = "stock";
            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = "Tela suelta";
            ws.Cell(2, 3).FormulaA1 = "=#REF!";
        });

        var resultado = await service.SimularAsync(archivo);

        Assert.False(resultado.ArchivoValido);
        Assert.Contains("cantidad", resultado.ErrorArchivo, StringComparison.Ordinal);
        Assert.Contains("unidad", resultado.ErrorArchivo, StringComparison.Ordinal);
        Assert.Contains("ubicación", resultado.ErrorArchivo, StringComparison.Ordinal);
        Assert.Contains("nombre del material", resultado.ErrorArchivo, StringComparison.Ordinal);
        Assert.Contains("id", resultado.ErrorArchivo, StringComparison.Ordinal);
        Assert.Empty(resultado.Validas);
        Assert.Empty(resultado.Errores);
        Assert.Equal(0, await db.Materials.CountAsync());
    }

    private static InventarioImportFilaValida Unica(InventarioImportResult resultado, string nombre) =>
        Assert.Single(resultado.Validas, f => f.Nombre == nombre);

    [Fact]
    public async Task Confirmar_CostoUnitarioOpcional_CompraOEntradaSinCosto_YLaPrimeraCompraNoSeDiluye()
    {
        await using var db = await CreateDbAsync();
        var user = new User
        {
            Nombre = "Admin Costo",
            Email = $"costo-{Guid.NewGuid():N}@test.local",
            PasswordHash = "x",
            Rol = UserRoles.Administrador,
            EmailConfirmed = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var importacion = new InventarioImportService(db, new UnitOfWork(db));

        await using var archivo = Libro(ws =>
        {
            ws.Cell(1, 1).Value = "Nombre del material";
            ws.Cell(1, 2).Value = "Cantidad";
            ws.Cell(1, 3).Value = "Unidad de medida";
            ws.Cell(1, 4).Value = "Ubicación";
            ws.Cell(1, 5).Value = "Costo unitario";
            ws.Cell(2, 1).Value = "Tela sin costo";
            ws.Cell(2, 2).Value = 100;
            ws.Cell(2, 3).Value = "metros";
            ws.Cell(2, 4).Value = "Planta de Inventario 1";
            ws.Cell(3, 1).Value = "Hilo con costo";
            ws.Cell(3, 2).Value = 10;
            ws.Cell(3, 3).Value = "metros";
            ws.Cell(3, 4).Value = "Planta de Inventario 1";
            ws.Cell(3, 5).Value = 10000;
            ws.Cell(4, 1).Value = "Botón a cero";
            ws.Cell(4, 2).Value = 5;
            ws.Cell(4, 3).Value = "unidad";
            ws.Cell(4, 4).Value = "Planta de Inventario 1";
            ws.Cell(4, 5).Value = 0;
        });

        var simulado = await importacion.SimularAsync(archivo);
        Assert.True(simulado.ArchivoValido, simulado.ErrorArchivo);
        Assert.Null(Unica(simulado, "Tela sin costo").CostoUnitario);
        Assert.Equal(10000m, Unica(simulado, "Hilo con costo").CostoUnitario);
        Assert.Null(Unica(simulado, "Botón a cero").CostoUnitario);

        archivo.Position = 0;
        var confirmado = await importacion.ConfirmarAsync(archivo, user.Id);
        Assert.True(confirmado.Persistido, confirmado.ErrorArchivo);
        Assert.Equal(3, confirmado.Importados);

        var tela = await db.Materials.SingleAsync(m => m.Name == "Tela sin costo");
        var hilo = await db.Materials.SingleAsync(m => m.Name == "Hilo con costo");
        var boton = await db.Materials.SingleAsync(m => m.Name == "Botón a cero");
        Assert.Equal(0m, tela.CostoPromedioPonderado);
        Assert.Equal(100m, tela.StockSinCosto);
        Assert.Equal(10000m, hilo.CostoPromedioPonderado);
        Assert.Equal(10000m, hilo.CostoAdquisicion);
        Assert.Equal(0m, hilo.StockSinCosto);
        Assert.Equal(0m, boton.CostoPromedioPonderado);
        Assert.Equal(5m, boton.StockSinCosto);

        var movimientos = await db.StockMovements.ToListAsync();
        var movTela = Assert.Single(movimientos, m => m.MaterialId == tela.Id);
        var movHilo = Assert.Single(movimientos, m => m.MaterialId == hilo.Id);
        Assert.Equal(StockEntryOrigin.OtraFuenteAutorizada, movTela.Origen);
        Assert.Equal(0m, movTela.CostoUnitario);
        Assert.True(movTela.SinCosto);
        Assert.Equal(InventarioImportTextos.ReferenciaMovimiento, movTela.Referencia);
        Assert.Equal(StockEntryOrigin.Compra, movHilo.Origen);
        Assert.Equal(10000m, movHilo.CostoUnitario);
        Assert.False(movHilo.SinCosto);
        Assert.Equal(InventarioImportTextos.ReferenciaMovimiento, movHilo.Referencia);

        var inventario = new InventoryService(
            new MaterialRepository(db),
            new MaterialRequestRepository(db),
            new ProductionOrderRepository(db),
            new BomRepository(db),
            new StockMovementRepository(db),
            new UnitOfWork(db));
        var compra = await inventario.AdjustStockAsync(
            new AdjustStockDto(tela.Id, 110m, StockEntryOrigin.Compra, 10_000m),
            user.Id);

        Assert.True(compra.Success, compra.Message);
        var actualizada = await db.Materials.SingleAsync(m => m.Id == tela.Id);
        Assert.Equal(110m, actualizada.Stock);
        Assert.Equal(100m, actualizada.StockSinCosto);
        Assert.Equal(10_000m, actualizada.CostoPromedioPonderado);
        Assert.NotEqual(909.0909m, actualizada.CostoPromedioPonderado);
    }

    [Theory]
    [InlineData("Precio", 10000d, 10000)]
    [InlineData("Valor unitario", 2500d, 2500)]
    [InlineData("Costo unitario", 10.5d, 10.5)]
    public async Task Simular_AliasDeCostoUnitario(string encabezado, double valor, decimal esperado)
    {
        await using var db = await CreateDbAsync();
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = Libro(ws =>
        {
            ws.Cell(1, 1).Value = "Nombre del material";
            ws.Cell(1, 2).Value = "Cantidad";
            ws.Cell(1, 3).Value = "Unidad";
            ws.Cell(1, 4).Value = "Ubicación";
            ws.Cell(1, 5).Value = encabezado;
            ws.Cell(2, 1).Value = "Avío";
            ws.Cell(2, 2).Value = 2;
            ws.Cell(2, 3).Value = "unidad";
            ws.Cell(2, 4).Value = "Planta de Inventario 1";
            ws.Cell(2, 5).Value = valor;
        });

        var resultado = await service.SimularAsync(archivo);

        Assert.True(resultado.ArchivoValido, resultado.ErrorArchivo);
        Assert.Equal(esperado, Assert.Single(resultado.Validas).CostoUnitario);
    }

    [Fact]
    public async Task Simular_CostoConPuntoDeMiles_YCostoInvalido()
    {
        await using var db = await CreateDbAsync();
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = Libro(ws =>
        {
            ws.Cell(1, 1).Value = "Nombre del material";
            ws.Cell(1, 2).Value = "Cantidad";
            ws.Cell(1, 3).Value = "Unidad";
            ws.Cell(1, 4).Value = "Ubicación";
            ws.Cell(1, 5).Value = "Precio";
            ws.Cell(2, 1).Value = "Tela miles";
            ws.Cell(2, 2).Value = "100";
            ws.Cell(2, 3).Value = "metros";
            ws.Cell(2, 4).Value = "Planta de Inventario 1";
            ws.Cell(2, 5).Value = "10.000";
            ws.Cell(3, 1).Value = "Tela mala";
            ws.Cell(3, 2).Value = "1";
            ws.Cell(3, 3).Value = "metros";
            ws.Cell(3, 4).Value = "Planta de Inventario 1";
            ws.Cell(3, 5).Value = "no-es-costo";
        });

        var resultado = await service.SimularAsync(archivo);

        Assert.True(resultado.ArchivoValido, resultado.ErrorArchivo);
        Assert.Equal(10000m, Unica(resultado, "Tela miles").CostoUnitario);
        var error = Assert.Single(resultado.Errores);
        Assert.Equal("Tela mala", error.Nombre);
        Assert.Contains("costo unitario", error.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    private static MemoryStream LibroSimulacion()
    {
        return Libro(ws =>
        {
            Encabezados(ws, filaEncabezado: 3);
            ws.Cell(1, 1).Value = "Plantilla CMTC";

            Fila(ws, 4, "Jersey", "metro", "ALM-1", 10, "Planta de Inventario 1",
                categoria: "Telas", subcategoria: "Punto", referencia: "REF-1",
                descripcion: "Rollo azul", proveedor: "Textiles", minimo: 2,
                fecha: new DateTime(2026, 11, 5), tipo: "Consumible", estado: "Malo",
                nombreComercial: "NO", formulaStock: "=100");

            Fila(ws, 6, "Hilo suelto", "kilo", codigo: null, cantidadTexto: "3,25",
                ubicacion: "Planta de Inventario 1");

            Fila(ws, 7, "Botones", "und", "ALM-1", 4, "Planta de Inventario 1");

            Fila(ws, 8, "Cinta", unidad: null, codigo: "ALM-2", cantidad: 1, ubicacion: "Planta de Inventario 1");

            Fila(ws, 9, "Cierre", "cajas", "ALM-3", 1, "Planta de Inventario 1");

            ws.Cell(10, Col("Estado")).Value = "Bueno";
            ws.Cell(10, Col("Stock")).FormulaA1 = "=100";

            Fila(ws, 11, "Paño demo", "paño", "ALM-P", 1, "Bodega piloto",
                minimoTexto: "1,5", fechaSerial: 30000, tipo: "Devolutivo");

            Fila(ws, 12, "Gruesa demo", "gruesa", "ALM-G", 1, "Bodega piloto", fechaTexto: "15/03/2024");

            Fila(ws, 13, "Decimetro demo", "dm", "ALM-D", 1, "planta de inventario 1", fechaTexto: "5/1/2024");

            Fila(ws, 14, "Paquete demo", "paquet", "ALM-Q", 1, "Planta de Inventario 1");

            Fila(ws, 15, "Metro corto", "m", "ALM-M", 1, "Planta de Inventario 1");

            Fila(ws, 16, "Negativo", "unidad", "ALM-N", -1, "Planta de Inventario 1");

            Fila(ws, 17, "Fecha mala", "unidad", "ALM-F", 1, "Planta de Inventario 1", fechaTexto: "32/13/2024");
        });
    }

    private static MemoryStream LibroConfirmacion()
    {
        return Libro(ws =>
        {
            Encabezados(ws);
            Fila(ws, 2, "Botón nácar", "paquete", "ALM-9", 4, "Bodega piloto",
                categoria: "Avíos", subcategoria: "Botones", referencia: "REF-B",
                descripcion: "Bolsa", proveedor: "Hilos SA", tipo: "Devolutivo",
                fechaTexto: "15/03/2024", estado: "Malo");
            Fila(ws, 3, "Tela cruda", "m", "ALM-9", 99, "planta de inventario 1", minimo: 5);
        });
    }

    private static readonly string[] Columnas =
    [
        "Nombre comercial",
        "Ubicación",
        "Estado",
        "Stock",
        "Nombre",
        "Unidad",
        "Código",
        "Cantidad",
        "Stock mínimo",
        "Categoría",
        "Subcategoría",
        "No. material/referencia",
        "Descripción",
        "Fecha de ingreso",
        "Proveedor",
        "Tipo de elemento"
    ];

    private static int Col(string nombre) => Array.IndexOf(Columnas, nombre) + 1;

    private static void Encabezados(IXLWorksheet ws, int filaEncabezado = 1)
    {
        for (var i = 0; i < Columnas.Length; i++)
            ws.Cell(filaEncabezado, i + 1).Value = Columnas[i];
    }

    private static void Fila(
        IXLWorksheet ws,
        int fila,
        string nombre,
        string? unidad = "unidad",
        string? codigo = null,
        double? cantidad = null,
        string? ubicacion = null,
        string? categoria = null,
        string? subcategoria = null,
        string? referencia = null,
        string? descripcion = null,
        string? proveedor = null,
        double? minimo = null,
        string? minimoTexto = null,
        DateTime? fecha = null,
        string? fechaTexto = null,
        double? fechaSerial = null,
        string? tipo = null,
        string? estado = null,
        string? nombreComercial = null,
        string? formulaStock = null,
        string? cantidadTexto = null)
    {
        if (nombreComercial is not null)
            ws.Cell(fila, Col("Nombre comercial")).Value = nombreComercial;
        if (ubicacion is not null)
            ws.Cell(fila, Col("Ubicación")).Value = ubicacion;
        if (estado is not null)
            ws.Cell(fila, Col("Estado")).Value = estado;
        if (formulaStock is not null)
            ws.Cell(fila, Col("Stock")).FormulaA1 = formulaStock;
        ws.Cell(fila, Col("Nombre")).Value = nombre;
        if (unidad is not null)
            ws.Cell(fila, Col("Unidad")).Value = unidad;
        if (codigo is not null)
            ws.Cell(fila, Col("Código")).Value = codigo;
        if (cantidadTexto is not null)
            ws.Cell(fila, Col("Cantidad")).Value = cantidadTexto;
        else if (cantidad is not null)
            ws.Cell(fila, Col("Cantidad")).Value = cantidad.Value;
        if (minimoTexto is not null)
            ws.Cell(fila, Col("Stock mínimo")).Value = minimoTexto;
        else if (minimo is not null)
            ws.Cell(fila, Col("Stock mínimo")).Value = minimo.Value;
        if (categoria is not null)
            ws.Cell(fila, Col("Categoría")).Value = categoria;
        if (subcategoria is not null)
            ws.Cell(fila, Col("Subcategoría")).Value = subcategoria;
        if (referencia is not null)
            ws.Cell(fila, Col("No. material/referencia")).Value = referencia;
        if (descripcion is not null)
            ws.Cell(fila, Col("Descripción")).Value = descripcion;
        if (fecha is not null)
            ws.Cell(fila, Col("Fecha de ingreso")).Value = fecha.Value;
        else if (fechaTexto is not null)
            ws.Cell(fila, Col("Fecha de ingreso")).Value = fechaTexto;
        else if (fechaSerial is not null)
            ws.Cell(fila, Col("Fecha de ingreso")).Value = fechaSerial.Value;
        if (proveedor is not null)
            ws.Cell(fila, Col("Proveedor")).Value = proveedor;
        if (tipo is not null)
            ws.Cell(fila, Col("Tipo de elemento")).Value = tipo;
    }

    private static MemoryStream LibroPlantillaReal()
    {
        return Libro(ws =>
        {
            string[] headers =
            [
                "id",
                "código ( numero de referencia del almacen principal)",
                "no. material / referencia ( se asigna en produccion)",
                "nombre del material",
                "unidad de medida",
                "cantidad",
                "ubicación / tipo de bodega",
                "stock",
                "stock mínimo",
                "tipo de elemento",
                "categoría",
                "fecha de ingreso"
            ];
            for (var i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            void Dato(int fila, int id, string? codigo, string? referencia, string? nombre, string? unidad,
                string? cantidad, string? ubicacion, string? minimo, string? tipo, string? categoria)
            {
                ws.Cell(fila, 1).Value = id;
                if (codigo is not null) ws.Cell(fila, 2).Value = codigo;
                if (referencia is not null) ws.Cell(fila, 3).Value = referencia;
                if (nombre is not null) ws.Cell(fila, 4).Value = nombre;
                if (unidad is not null) ws.Cell(fila, 5).Value = unidad;
                if (cantidad is not null) ws.Cell(fila, 6).Value = cantidad;
                if (ubicacion is not null) ws.Cell(fila, 7).Value = ubicacion;
                ws.Cell(fila, 8).FormulaA1 = "=#REF!";
                if (minimo is not null) ws.Cell(fila, 9).Value = minimo;
                if (tipo is not null) ws.Cell(fila, 10).Value = tipo;
                if (categoria is not null) ws.Cell(fila, 11).Value = categoria;
                ws.Cell(fila, 12).Value = "05/11/2026";
            }

            Dato(2, 1, "ALM-65", "REF-65", "Tela decimal", "caja", "65,4", "centro logistico CMTC", null, "consumibles", "Telas");
            Dato(3, 2, "ALM-LIB", "REF-L", "Hilo libra", "libra", "2", "Centro logístico CMTC", "4", "devolutivos", "Hilos");
            Dato(4, 3, "ALM-65", "REF-65", "Tela decimal", "caja", "10", "centro logistico CMTC", null, "consumibles", "Telas");
            Dato(5, 4, "ALM-PROC", "REF-P", "Pieza en curso", "caja", "1", "Bodega de proceso", null, "consumible", "material en proceso");
            Dato(6, 5, "ALM-TERM", "REF-T", "Camisa lista", "unidad", "1", "Bodega terminado", null, "devolutivo", "producto terminado");
            for (var i = 0; i < 300; i++)
            {
                var fila = 7 + i;
                ws.Cell(fila, 1).Value = 1000 + i;
                ws.Cell(fila, 8).FormulaA1 = "=#REF!";
            }
        });
    }

    private static MemoryStream Libro(Action<IXLWorksheet> fill)
    {
        using var wb = new XLWorkbook();
        wb.AddWorksheet("Portada");
        var ws = wb.AddWorksheet(InventarioImportTextos.Hoja);
        fill(ws);
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static async Task<SipitexDbContext> CreateDbAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sipitex-import-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        var db = new SipitexDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
