using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Import;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;
using Sipitex.Infrastructure.Services;

namespace Sipitex.Tests;

public class FichaTecnicaMaterialTests
{
    [Fact]
    public async Task CrearYEditar_DejaUnaSolaFicha_YRegistraActividad()
    {
        await using var db = await CreateDbAsync();
        var user = await UsuarioAsync(db);
        var material = await MaterialAsync(db, "ALM-1", "Jersey");
        var service = Servicio(db);

        var creado = await service.GuardarAsync(new GuardarFichaTecnicaMaterialDto
        {
            MaterialId = material.Id,
            TipoMaterial = "Jersey",
            GramajeGm2 = "180,5",
            AnchoCm = "150",
            DescripcionTecnica = "Tela de punto"
        }, user.Id);

        Assert.True(creado.Success, creado.Message);
        var ficha = await db.FichasTecnicasMaterial.SingleAsync();
        Assert.Equal(material.Id, ficha.MaterialId);
        Assert.Equal(180.5m, ficha.GramajeGm2);
        Assert.Equal(150m, ficha.AnchoCm);
        Assert.Equal("Tela de punto", ficha.DescripcionTecnica);

        var editado = await service.GuardarAsync(new GuardarFichaTecnicaMaterialDto
        {
            MaterialId = material.Id,
            TipoMaterial = "Jersey peinado",
            GramajeGm2 = "200",
            DescripcionTecnica = "Tela de punto actualizada"
        }, user.Id);

        Assert.True(editado.Success, editado.Message);
        Assert.Equal(1, await db.FichasTecnicasMaterial.CountAsync());
        var actual = await db.FichasTecnicasMaterial.SingleAsync();
        Assert.Equal(ficha.Id, actual.Id);
        Assert.Equal("Jersey peinado", actual.TipoMaterial);
        Assert.Equal(200m, actual.GramajeGm2);
        Assert.Null(actual.AnchoCm);
        Assert.Equal("Tela de punto actualizada", actual.DescripcionTecnica);

        var logs = await db.ActivityLogs.OrderBy(a => a.Id).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Equal(ActivityLogActions.CreateFichaTecnicaMaterial, logs[0].Action);
        Assert.Equal(ActivityLogActions.UpdateFichaTecnicaMaterial, logs[1].Action);
        Assert.All(logs, log =>
        {
            Assert.Equal(ActivityLogEntities.FichaTecnicaMaterial, log.Entity);
            Assert.Equal(actual.Id.ToString(), log.EntityId);
            Assert.Equal(user.Id, log.UserId);
        });
    }

    [Fact]
    public async Task UnMaterial_SoloPuedeTenerUnaFicha()
    {
        await using var db = await CreateDbAsync();
        var material = await MaterialAsync(db, "ALM-1", "Jersey");
        var indice = db.Model.FindEntityType(typeof(FichaTecnicaMaterial))!
            .GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(FichaTecnicaMaterial.MaterialId));
        Assert.True(indice.IsUnique);

        db.FichasTecnicasMaterial.Add(new FichaTecnicaMaterial { MaterialId = material.Id, TipoMaterial = "A" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        db.FichasTecnicasMaterial.Add(new FichaTecnicaMaterial { MaterialId = material.Id, TipoMaterial = "B" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(1, await db.FichasTecnicasMaterial.CountAsync());
    }

    [Fact]
    public async Task HojaAusente_NoFallaLaImportacion()
    {
        await using var db = await CreateDbAsync();
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = Libro((inv, _) =>
        {
            EncabezadosInventario(inv);
            inv.Cell(2, 1).Value = "Hilo";
            inv.Cell(2, 2).Value = 2;
            inv.Cell(2, 3).Value = "libra";
            inv.Cell(2, 4).Value = "Planta de Inventario 1";
        }, conFichas: false);

        var resultado = await service.SimularAsync(archivo);

        Assert.True(resultado.ArchivoValido, resultado.ErrorArchivo);
        Assert.Equal(0, resultado.FichasAVincular);
        Assert.Empty(resultado.FichasAdvertencias);
        Assert.Single(resultado.Validas);
        Assert.Equal(0, await db.FichasTecnicasMaterial.CountAsync());
    }

    [Fact]
    public async Task UnMaterialCoincidente_VinculaLaFicha_YConservaElDecimal()
    {
        await using var db = await CreateDbAsync();
        var user = await UsuarioAsync(db);
        await MaterialAsync(db, "ALM-1", "Jersey");
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = LibroFichas(
            Fila(2, codigo: "ALM-1", nombre: "Jersey", gramaje: "180,5", ancho: "150,5", tipo: "Jersey algodón", descripcion: "Tela de punto"),
            FilaVacia(3));

        var simulacion = await service.SimularAsync(archivo);
        Assert.True(simulacion.ArchivoValido, simulacion.ErrorArchivo);
        Assert.Equal(1, simulacion.FichasAVincular);
        Assert.Empty(simulacion.FichasAdvertencias);
        Assert.Equal(0, await db.FichasTecnicasMaterial.CountAsync());
        Assert.False(db.ChangeTracker.HasChanges());

        archivo.Position = 0;
        var confirmado = await service.ConfirmarAsync(archivo, user.Id);
        Assert.True(confirmado.ArchivoValido, confirmado.ErrorArchivo);
        Assert.True(confirmado.Persistido);
        Assert.Equal(1, confirmado.FichasVinculadas);

        var ficha = await db.FichasTecnicasMaterial.Include(f => f.Material).SingleAsync();
        Assert.Equal("ALM-1", ficha.Material.CodigoAlmacen);
        Assert.Equal(180.5m, ficha.GramajeGm2);
        Assert.Equal(150.5m, ficha.AnchoCm);
        Assert.Equal("Jersey algodón", ficha.TipoMaterial);
        Assert.Equal("Tela de punto", ficha.DescripcionTecnica);
        Assert.Null(ficha.EnlaceFichaOriginal);
        Assert.Contains(await db.ActivityLogs.ToListAsync(), a => a.Action == ActivityLogActions.CreateFichaTecnicaMaterial);
    }

    [Fact]
    public async Task CeroMaterialesCoincidentes_QuedaComoAdvertencia()
    {
        await using var db = await CreateDbAsync();
        var user = await UsuarioAsync(db);
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = LibroFichas(Fila(2, codigo: "ALM-NO", nombre: "Sin dueño"));

        var simulacion = await service.SimularAsync(archivo);

        Assert.True(simulacion.ArchivoValido, simulacion.ErrorArchivo);
        Assert.Equal(0, simulacion.FichasAVincular);
        var aviso = Assert.Single(simulacion.FichasAdvertencias);
        Assert.Equal(2, aviso.Fila);
        Assert.Contains("ficha sin material único: completar a mano", aviso.Mensaje, StringComparison.Ordinal);

        archivo.Position = 0;
        var confirmado = await service.ConfirmarAsync(archivo, user.Id);
        Assert.False(confirmado.Persistido);
        Assert.Equal(0, confirmado.FichasVinculadas);
        Assert.Equal(0, await db.FichasTecnicasMaterial.CountAsync());
    }

    [Fact]
    public async Task DosMaterialesCoincidentes_NoSeAdivina()
    {
        await using var db = await CreateDbAsync();
        var user = await UsuarioAsync(db);
        await MaterialAsync(db, "ALM-DUP", "Jersey planta 1", plantaId: 1);
        await MaterialAsync(db, "ALM-DUP", "Jersey planta 2", plantaId: 2);
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = LibroFichas(Fila(2, referencia: "ALM-DUP", nombre: "Jersey"));

        var simulacion = await service.SimularAsync(archivo);

        Assert.True(simulacion.ArchivoValido, simulacion.ErrorArchivo);
        Assert.Equal(0, simulacion.FichasAVincular);
        var aviso = Assert.Single(simulacion.FichasAdvertencias);
        Assert.Contains("ficha sin material único: completar a mano", aviso.Mensaje, StringComparison.Ordinal);

        archivo.Position = 0;
        await service.ConfirmarAsync(archivo, user.Id);
        Assert.Equal(0, await db.FichasTecnicasMaterial.CountAsync());
    }

    [Fact]
    public async Task MaterialCreadoEnElMismoArchivo_TambienSeVincula()
    {
        await using var db = await CreateDbAsync();
        var user = await UsuarioAsync(db);
        var service = new InventarioImportService(db, new UnitOfWork(db));
        await using var archivo = Libro((inv, fichas) =>
        {
            EncabezadosInventario(inv);
            inv.Cell(2, 1).Value = "Dril";
            inv.Cell(2, 2).Value = "4";
            inv.Cell(2, 3).Value = "metro";
            inv.Cell(2, 4).Value = "Planta de Inventario 1";
            inv.Cell(2, 5).Value = "ALM-NUEVO";
            Assert.NotNull(fichas);
            EncabezadosFicha(fichas);
            Fila(2, codigo: "ALM-NUEVO", nombre: "Dril", gramaje: "65,4")(fichas);
        });

        var simulacion = await service.SimularAsync(archivo);
        Assert.True(simulacion.ArchivoValido, simulacion.ErrorArchivo);
        Assert.Equal(1, simulacion.FichasAVincular);
        Assert.Empty(simulacion.FichasAdvertencias);
        Assert.Equal(0, await db.Materials.CountAsync(m => m.CodigoAlmacen == "ALM-NUEVO"));

        archivo.Position = 0;
        var confirmado = await service.ConfirmarAsync(archivo, user.Id);
        Assert.Equal(1, confirmado.Importados);
        Assert.Equal(1, confirmado.FichasVinculadas);
        var ficha = await db.FichasTecnicasMaterial.Include(f => f.Material).SingleAsync();
        Assert.Equal("Dril", ficha.Material.Name);
        Assert.Equal(65.4m, ficha.GramajeGm2);
    }

    private static FichaTecnicaMaterialService Servicio(SipitexDbContext db) =>
        new(db, new UnitOfWork(db), new ActivityLogService(db, new UserRepository(db)));

    private static async Task<User> UsuarioAsync(SipitexDbContext db)
    {
        var user = new User
        {
            Nombre = "Admin Ficha",
            Email = $"admin-{Guid.NewGuid():N}@test.local",
            PasswordHash = "x",
            Rol = UserRoles.Administrador,
            EmailConfirmed = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Material> MaterialAsync(
        SipitexDbContext db,
        string codigo,
        string nombre,
        int plantaId = 1)
    {
        var material = new Material
        {
            Code = "mat" + Guid.NewGuid().ToString("N")[..12],
            CodigoAlmacen = codigo,
            Name = nombre,
            Unit = MaterialUnit.Metros,
            Stock = 1,
            PlantaInventarioId = plantaId
        };
        db.Materials.Add(material);
        await db.SaveChangesAsync();
        return material;
    }

    private static void EncabezadosInventario(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "nombre del material";
        ws.Cell(1, 2).Value = "cantidad";
        ws.Cell(1, 3).Value = "unidad de medida";
        ws.Cell(1, 4).Value = "ubicación / tipo de bodega";
        ws.Cell(1, 5).Value = "código ( numero de referencia del almacen principal)";
    }

    private static void EncabezadosFicha(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "código ( numero de referencia del almacen principal)";
        ws.Cell(1, 2).Value = "nombre del material";
        ws.Cell(1, 3).Value = "referencia";
        ws.Cell(1, 4).Value = "descripción técnica";
        ws.Cell(1, 5).Value = "tipo de tela / material";
        ws.Cell(1, 6).Value = "composición";
        ws.Cell(1, 7).Value = "gramaje (g/m²)";
        ws.Cell(1, 8).Value = "ancho (cm)";
        ws.Cell(1, 9).Value = "colores disponibles";
        ws.Cell(1, 10).Value = "acabados";
        ws.Cell(1, 11).Value = "materiales e insumos";
        ws.Cell(1, 12).Value = "especificaciones de confección / uso";
        ws.Cell(1, 13).Value = "proceso de fabricación";
        ws.Cell(1, 14).Value = "normas / certificaciones";
        ws.Cell(1, 15).Value = "imagen o enlace a ficha original";
        ws.Cell(1, 16).Value = "observaciones";
        ws.Cell(1, 17).Value = "id";
        ws.Cell(1, 18).Value = "stock";
    }

    private static Action<IXLWorksheet> Fila(
        int fila,
        string? codigo = null,
        string? referencia = null,
        string? nombre = null,
        string? gramaje = null,
        string? ancho = null,
        string? tipo = null,
        string? descripcion = null) => ws =>
    {
        if (codigo is not null) ws.Cell(fila, 1).Value = codigo;
        if (nombre is not null) ws.Cell(fila, 2).Value = nombre;
        if (referencia is not null) ws.Cell(fila, 3).Value = referencia;
        if (descripcion is not null) ws.Cell(fila, 4).Value = descripcion;
        if (tipo is not null) ws.Cell(fila, 5).Value = tipo;
        if (gramaje is not null) ws.Cell(fila, 7).Value = gramaje;
        if (ancho is not null) ws.Cell(fila, 8).Value = ancho;
        ws.Cell(fila, 15).FormulaA1 = "=#REF!";
    };

    private static Action<IXLWorksheet> FilaVacia(int fila) => ws =>
    {
        ws.Cell(fila, 17).Value = fila;
        ws.Cell(fila, 18).FormulaA1 = "=#REF!";
    };

    private static MemoryStream LibroFichas(params Action<IXLWorksheet>[] filas) =>
        Libro((_, fichas) =>
        {
            EncabezadosFicha(fichas!);
            foreach (var fila in filas)
                fila(fichas!);
        });

    private static MemoryStream Libro(Action<IXLWorksheet, IXLWorksheet?> fill, bool conFichas = true)
    {
        using var wb = new XLWorkbook();
        var inventario = wb.AddWorksheet(InventarioImportTextos.Hoja);
        EncabezadosInventario(inventario);
        IXLWorksheet? fichas = conFichas ? wb.AddWorksheet(InventarioImportTextos.HojaFichas) : null;
        fill(inventario, fichas);
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static async Task<SipitexDbContext> CreateDbAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sipitex-ficha-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        var db = new SipitexDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
