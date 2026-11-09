using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Reporting;

namespace Sipitex.Tests;

public class ActividadInstructorServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProductionSessionRepository> _sessions = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snapshots = new();
    private readonly Mock<IGrupoConfeccionRepository> _grupos = new();
    private readonly Mock<IQualityRepository> _quality = new();
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IFichaRepository> _fichas = new();

    public ActividadInstructorServiceTests()
    {
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _sessions.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _grupos.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _consumos.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _snapshots.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private ActividadInstructorService CreateSut() => new(
        _users.Object, _sessions.Object, _snapshots.Object, _grupos.Object,
        _quality.Object, _consumos.Object, _fichas.Object);

    [Fact]
    public async Task Consultar_SinRegistros_NoInventaHorasNiAprendices()
    {
        _users.Setup(u => u.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, Nombre = "Laura Gómez" });

        var reporte = await CreateSut().ConsultarAsync(new ActividadInstructorConsulta(
            10, new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 5)));

        Assert.False(reporte.TieneActividad);
        Assert.Equal(3, reporte.DiasSinActividad);
        Assert.Equal(0, reporte.DiasConActividad);
        Assert.Equal(ActividadInstructorTexto.NoRegistrado, reporte.HorasRegistradas);
        Assert.Equal(ActividadInstructorTexto.NoRegistrado, reporte.Aprendices);
        Assert.All(reporte.Dias, dia =>
        {
            Assert.False(dia.ConActividad);
            Assert.Equal(ActividadInstructorTexto.NoRegistrado, dia.Ingreso);
            Assert.Equal(ActividadInstructorTexto.NoRegistrado, dia.Salida);
            Assert.Equal(ActividadInstructorTexto.NoRegistrado, dia.Aprendices);
        });
        Assert.Equal(12, reporte.CodigoVerificacion.Length);
    }

    [Fact]
    public async Task Consultar_UsaSesionGrupoCalidadYConsumo_SinTomarLaHoraDeLaSesionComoIngreso()
    {
        var ficha = new Ficha
        {
            Id = 1,
            NumeroGrupo = "FICHA-T1",
            ProcessName = "Trazo",
            Turno = "Mañana",
            InstructorUserId = 10,
            ProductionOrderId = 5
        };
        var orden = new ProductionOrder { Id = 5, OrderNumber = "OP-001", ProductName = "Camisa" };
        _users.Setup(u => u.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, Nombre = "Laura Gómez", PhotoPath = "/Account/Photo/10" });
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([ficha]);
        _sessions.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProductionSession
                {
                    Id = 1,
                    FichaId = 1,
                    Ficha = ficha,
                    ProductionOrderId = 5,
                    ProductionOrder = orden,
                    Units = 12,
                    Observations = "Faltó hilo azul",
                    SessionDate = new DateTime(2026, 11, 3, 9, 15, 0),
                    RegisteredByUserId = 10
                }
            ]);
        _snapshots.Setup(r => r.GetByOrderIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProductionOrderBomSnapshot
                {
                    MaterialName = "Tela Jersey",
                    QuantityPerUnit = 1.6m,
                    Unit = MaterialUnit.Metros
                }
            ]);
        _grupos.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new GrupoConfeccion
                {
                    Id = 7,
                    InstructorUserId = 10,
                    ProductionOrderId = 5,
                    ProductionOrder = orden,
                    FechaRealizacion = new DateOnly(2026, 11, 6),
                    HoraInicio = new TimeOnly(7, 0),
                    HoraFin = new TimeOnly(11, 30),
                    CantidadPrendas = 8
                }
            ]);
        _quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new QualityRecord
                {
                    ProductionOrderId = 5,
                    ProductionOrder = orden,
                    UnitsInspected = 10,
                    Result = QualityResult.Aprobada,
                    Responsable = "laura gómez",
                    InspectionDate = new DateOnly(2026, 11, 3)
                }
            ]);
        _consumos.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ConsumoMaterial
                {
                    ProductionOrderId = 5,
                    ProductionOrder = orden,
                    MaterialId = 1,
                    Material = new Material { Id = 1, Name = "Hilo Poliéster", Unit = MaterialUnit.Metros },
                    Cantidad = 4.5m,
                    FechaUtc = new DateTime(2026, 11, 6, 15, 0, 0),
                    ResponsableUserId = 10
                }
            ]);

        var reporte = await CreateSut().ConsultarAsync(new ActividadInstructorConsulta(
            10, new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 6)));

        Assert.True(reporte.TieneActividad);
        Assert.Equal("LG", reporte.Iniciales);
        Assert.Equal(2, reporte.DiasConActividad);
        Assert.Equal(2, reporte.DiasSinActividad);
        Assert.Equal(12, reporte.UnidadesEnSesiones);
        Assert.Equal(8, reporte.PrendasEnGrupos);
        Assert.Equal("4 h 30 min", reporte.HorasRegistradas);
        Assert.Equal(ActividadInstructorTexto.NoRegistrado, reporte.Aprendices);
        Assert.Contains("Producción", reporte.ActividadesRealizadas);
        Assert.Contains("Grupo de confección", reporte.ActividadesRealizadas);
        Assert.Contains("Control de calidad", reporte.ActividadesRealizadas);
        Assert.Contains("Consumo de materiales", reporte.ActividadesRealizadas);

        var tres = reporte.Dias.Single(d => d.Fecha == new DateOnly(2026, 11, 3));
        Assert.Equal(ActividadInstructorTexto.NoRegistrado, tres.Ingreso);
        Assert.Equal(ActividadInstructorTexto.NoRegistrado, tres.Salida);
        Assert.Contains(tres.Actividades, a => a.Contains("12 unidades", StringComparison.Ordinal) && a.Contains("19.2 metro", StringComparison.Ordinal));
        Assert.Contains(tres.Actividades, a => a.Contains("Aprobada", StringComparison.Ordinal));
        Assert.Contains("Faltó hilo azul", tres.Observaciones);
        Assert.Contains(tres.ConQuien, q => q.Contains("FICHA-T1", StringComparison.Ordinal));

        var seis = reporte.Dias.Single(d => d.Fecha == new DateOnly(2026, 11, 6));
        Assert.Equal("07:00", seis.Ingreso);
        Assert.Equal("11:30", seis.Salida);
        Assert.Contains(seis.Actividades, a => a.Contains("Hilo Poliéster", StringComparison.Ordinal) && a.Contains("4.5 metro", StringComparison.Ordinal));

        var vacio = reporte.Dias.Single(d => d.Fecha == new DateOnly(2026, 11, 4));
        Assert.False(vacio.ConActividad);
    }

    [Fact]
    public async Task Consultar_FiltroDeFicha_DejaFueraLaOtraJornada()
    {
        var manana = new Ficha { Id = 1, NumeroGrupo = "FICHA-T1", ProcessName = "Trazo", Turno = "Mañana", ProductionOrderId = 5, InstructorUserId = 10 };
        var tarde = new Ficha { Id = 2, NumeroGrupo = "FICHA-E3", ProcessName = "Confección", Turno = "Tarde", ProductionOrderId = 8, InstructorUserId = 10 };
        _users.Setup(u => u.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, Nombre = "Laura Gómez" });
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([manana, tarde]);
        _sessions.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProductionSession { FichaId = 1, Ficha = manana, ProductionOrderId = 5, ProductionOrder = new ProductionOrder { OrderNumber = "OP-001" }, Units = 3, SessionDate = new DateTime(2026, 11, 3, 8, 0, 0), RegisteredByUserId = 10 },
                new ProductionSession { FichaId = 2, Ficha = tarde, ProductionOrderId = 8, ProductionOrder = new ProductionOrder { OrderNumber = "OP-002" }, Units = 9, SessionDate = new DateTime(2026, 11, 3, 14, 0, 0), RegisteredByUserId = 10 }
            ]);

        var reporte = await CreateSut().ConsultarAsync(new ActividadInstructorConsulta(
            10, new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 3), FichaId: 1, Jornada: "Mañana"));

        Assert.Equal(3, reporte.UnidadesEnSesiones);
        var dia = Assert.Single(reporte.Dias);
        Assert.Contains(dia.Actividades, a => a.Contains("OP-001", StringComparison.Ordinal));
        Assert.DoesNotContain(dia.Actividades, a => a.Contains("OP-002", StringComparison.Ordinal));
    }

    [Fact]
    public void Pdf_IncluyeEncabezadoResumenYVerificacion()
    {
        var reporte = new ActividadInstructorReporteDto(
            true, 10, "Laura Gómez", null, "LG",
            new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 3),
            "3 de noviembre de 2026", "FICHA-T1 · Trazo · Mañana", null, null,
            true, 1, 1, 0, "No registrado", "No registrado", 1, 12, 0,
            ["Producción"], ["Control de calidad", "Consumo de materiales", "Grupo de confección"],
            [
                new ActividadInstructorDiaDto(
                    new DateOnly(2026, 11, 3),
                    "Martes 3 de noviembre de 2026",
                    true,
                    "No registrado",
                    "No registrado",
                    ["Producción: 12 unidades de Camisa (OP-001)."],
                    ["FICHA-T1 · Trazo · Mañana"],
                    "No registrado",
                    ["Faltó hilo azul"])
            ],
            "AB12CD34EF56");

        var file = new ActividadInstructorPdf().Render(reporte);
        var texto = ExtraerTextoPdf(file.Content);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(file.Content.AsSpan(0, 4)));
        Assert.EndsWith(".pdf", file.FileName);
        var legible = texto.Replace("\uFB01", "fi").Replace("\uFB02", "fl");
        Assert.Contains("Reporte de Ac", legible, StringComparison.Ordinal);
        Assert.Contains("vidad del Instructor", legible, StringComparison.Ordinal);
        Assert.Contains("Laura Gómez", legible, StringComparison.Ordinal);
        Assert.Contains("No registrado", legible, StringComparison.Ordinal);
        Assert.Contains("Faltó hilo azul", legible, StringComparison.Ordinal);
        Assert.Contains("AB12CD34EF56", legible, StringComparison.Ordinal);
        Assert.Contains("Código de ver", legible, StringComparison.Ordinal);
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
            foreach (System.Text.RegularExpressions.Match recurso in Regex.Matches(cuerpo, @"/(F\d+)\s+(\d+)\s+0 R"))
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
        foreach (System.Text.RegularExpressions.Match bloque in Regex.Matches(cmap, @"beginbfchar(.*?)endbfchar", RegexOptions.Singleline))
        {
            foreach (System.Text.RegularExpressions.Match par in Regex.Matches(bloque.Groups[1].Value, @"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>"))
                mapa[Convert.ToInt32(par.Groups[1].Value, 16)] = DecodificarUtf16(par.Groups[2].Value);
        }

        foreach (System.Text.RegularExpressions.Match bloque in Regex.Matches(cmap, @"beginbfrange(.*?)endbfrange", RegexOptions.Singleline))
        {
            foreach (System.Text.RegularExpressions.Match rango in Regex.Matches(bloque.Groups[1].Value, @"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>"))
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
