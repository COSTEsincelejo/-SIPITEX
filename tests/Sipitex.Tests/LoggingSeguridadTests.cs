using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Data;
using Sipitex.Web.Logging;
using Sipitex.Web.Security;

namespace Sipitex.Tests;

public class LoggingSeguridadTests
{
    private const string Password = "ClaveProhibida-Log-7K2M";
    private const string Correo = "correo-prohibido-log@ejemplo.test";
    private const string MarcaFirma = "FIRMA-SECRETA-NOLOG-8F3A21";

    [Fact]
    public async Task ActaYLogin_NoEscribenFirmaNiContrasena()
    {
        var actaLog = new ListaLogger<ActaMovimientoService>();
        var loginLog = new ListaLogger<UserAccountService>();
        var bloqueoLog = new ListaLogger<MemoryLoginAttemptGuard>();
        var firma = FirmaConMarca();
        var dataUrl = "data:image/png;base64," + Convert.ToBase64String(firma);

        var acta = await CrearActa(actaLog, firma);
        Assert.True(acta.Success, acta.Message);

        var users = new Mock<IUserRepository>();
        var user = new User
        {
            Id = 42,
            Nombre = "Persona",
            Email = Correo,
            PasswordHash = PasswordHasher.Hash("otra-clave-distinta"),
            Rol = UserRoles.Instructor,
            IsActive = true,
            EmailConfirmed = true
        };
        users.Setup(r => r.GetByEmailAsync(Correo, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var cuentas = new UserAccountService(
            users.Object,
            Mock.Of<IFichaRepository>(),
            Mock.Of<IPlantaInventarioRepository>(),
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IPasswordResetService>(),
            loginLog);
        var autenticado = await cuentas.AuthenticateAsync(Correo, Password);
        Assert.Null(autenticado);

        var guard = new MemoryLoginAttemptGuard(new MemoryCache(new MemoryCacheOptions()), bloqueoLog);
        for (var i = 0; i < MemoryLoginAttemptGuard.MaxFailedAttempts; i++)
            guard.RecordFailure(Correo, "203.0.113.10");
        Assert.True(guard.IsLockedOut(Correo, "203.0.113.10"));

        var texto = string.Join('\n', actaLog.Lineas.Concat(loginLog.Lineas).Concat(bloqueoLog.Lineas));
        Assert.Contains("Acta creada", texto, StringComparison.Ordinal);
        Assert.Contains("ACT-0001", texto, StringComparison.Ordinal);
        Assert.Contains("Inicio de sesión fallido", texto, StringComparison.Ordinal);
        Assert.Contains("UsuarioId", texto, StringComparison.Ordinal);
        Assert.Contains("42", texto, StringComparison.Ordinal);
        Assert.Contains("Cuenta bloqueada", texto, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, texto, StringComparison.Ordinal);
        Assert.DoesNotContain(MarcaFirma, texto, StringComparison.Ordinal);
        Assert.DoesNotContain(dataUrl, texto, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToBase64String(firma), texto, StringComparison.Ordinal);
        Assert.DoesNotContain(Correo, texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PasoDeArranque_ConservaElNombreCuandoFalla()
    {
        var ex = await Assert.ThrowsAsync<ArranqueException>(() =>
            PasosArranque.Ejecutar(PasosArranque.Migraciones, () => throw new InvalidOperationException("sin-reintento")));

        Assert.Equal(PasosArranque.Migraciones, ex.Paso);
        Assert.Contains("sin-reintento", ex.InnerException?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FallaDeArranque_NoDejaLaCadenaDeConexion()
    {
        var log = new ListaLogger<LoggingSeguridadTests>();
        var secreta = "Password=super-secreta-db";
        var ex = new InvalidOperationException(
            "No se pudo conectar. Host=db.interno;Database=sipitex;Username=sipitex;" + secreta);

        RegistroSeguro.Fatal(log, ex, PasosArranque.Migraciones);

        var texto = string.Join('\n', log.Lineas);
        Assert.Contains(PasosArranque.Migraciones, texto, StringComparison.Ordinal);
        Assert.Contains("No se reintenta", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secreta-db", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("db.interno", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Niveles_SeSobrescribenDesdeLaConfiguracion()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
            ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning",
            ["Logging:LogLevel:Sipitex"] = "Information",
            ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"] = "Error"
        }).Build();

        var sink = new ListaSink();
        var loggerConfig = new LoggerConfiguration();
        SipitexLogging.AplicarNiveles(loggerConfig, config);
        using var logger = loggerConfig.WriteTo.Sink(sink).CreateLogger();

        logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting.Diagnostics")
            .Information("asp-info");
        logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting.Diagnostics")
            .Warning("asp-warn");
        logger.ForContext("SourceContext", "Sipitex.Web.Algo")
            .Information("sipitex-info");
        logger.ForContext("SourceContext", "Microsoft.EntityFrameworkCore.Database.Command")
            .Warning("ef-warn");
        logger.ForContext("SourceContext", "Microsoft.EntityFrameworkCore.Database.Command")
            .Error("ef-error");

        var textos = sink.Eventos.Select(e => e.RenderMessage()).ToList();
        Assert.DoesNotContain("asp-info", textos);
        Assert.Contains("asp-warn", textos);
        Assert.Contains("sipitex-info", textos);
        Assert.DoesNotContain("ef-warn", textos);
        Assert.Contains("ef-error", textos);
    }

    [Fact]
    public void CodigoDeCorrelacion_TieneSeisHexadecimales()
    {
        var codigo = CodigoCorrelacion.Crear();
        Assert.Matches("^ERR-[0-9A-F]{6}$", codigo);
    }

    private static byte[] FirmaConMarca()
    {
        var marca = Encoding.UTF8.GetBytes(MarcaFirma);
        var firma = new byte[TestPng.Bytes.Length + marca.Length];
        TestPng.Bytes.CopyTo(firma, 0);
        marca.CopyTo(firma, TestPng.Bytes.Length);
        return firma;
    }

    private static async Task<ServiceResult<ActaMovimientoDto>> CrearActa(
        ILogger<ActaMovimientoService> logger,
        byte[] firma)
    {
        var actas = new Mock<IActaMovimientoRepository>();
        var consumos = new Mock<IConsumoMaterialRepository>();
        var orders = new Mock<IProductionOrderRepository>();
        var users = new Mock<IUserRepository>();
        var uow = new Mock<IUnitOfWork>();
        ActaMovimiento? saved = null;

        users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        actas.Setup(r => r.GetLastNumeroAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        actas.Setup(r => r.AddAsync(It.IsAny<ActaMovimiento>(), It.IsAny<CancellationToken>()))
            .Callback<ActaMovimiento, CancellationToken>((a, _) =>
            {
                a.Id = 1;
                saved = a;
            })
            .Returns(Task.CompletedTask);
        actas.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);
        actas.Setup(r => r.GetNumerosPorStockMovementAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
        actas.Setup(r => r.GetNumerosPorConsumoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
        orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 4, OrderNumber = "OP-4" });
        consumos.Setup(r => r.GetByOrderIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial
                {
                    Id = 11,
                    ProductionOrderId = 4,
                    MaterialId = 3,
                    Material = new Material { Id = 3, Name = "Hilo", Unit = MaterialUnit.Metros },
                    Cantidad = 5
                }
            ]);

        var service = new ActaMovimientoService(
            actas.Object,
            Mock.Of<IStockMovementRepository>(),
            consumos.Object,
            orders.Object,
            users.Object,
            Mock.Of<IActaPdfService>(),
            uow.Object,
            logger);

        return await service.CreateAsync(new CreateActaDto(
            ActaTipo.Egreso,
            ActaOrigen.Consumo,
            "Laura",
            "Instructor",
            true,
            "Pedro",
            "Encargado de bodega",
            true,
            7,
            ProductionOrderId: 4,
            EntregaFirmaPng: firma,
            RecibeFirmaPng: firma));
    }

    private sealed class ListaSink : ILogEventSink
    {
        public List<LogEvent> Eventos { get; } = new();

        public void Emit(LogEvent logEvent) => Eventos.Add(logEvent);
    }

    private sealed class ListaLogger<T> : ILogger<T>
    {
        public List<string> Lineas { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var texto = formatter(state, exception);
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    texto += pair.Value switch
                    {
                        byte[] bytes => " " + Convert.ToBase64String(bytes),
                        null => "",
                        _ => " " + pair.Value
                    };
                }
            }

            if (exception is not null)
                texto += Environment.NewLine + exception;
            Lineas.Add(texto);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
