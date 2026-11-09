using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Arma el resumen de actividad con lo que el sistema ya guarda.
// No hay marcación de ingreso/salida ni padrón de aprendices: esos campos salen como "No registrado".
public class ActividadInstructorService : IActividadInstructorService
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-CO");

    private readonly IUserRepository _users;
    private readonly IProductionSessionRepository _sessions;
    private readonly IProductionOrderBomSnapshotRepository _snapshots;
    private readonly IGrupoConfeccionRepository _grupos;
    private readonly IQualityRepository _quality;
    private readonly IConsumoMaterialRepository _consumos;
    private readonly IFichaRepository _fichas;

    public ActividadInstructorService(
        IUserRepository users,
        IProductionSessionRepository sessions,
        IProductionOrderBomSnapshotRepository snapshots,
        IGrupoConfeccionRepository grupos,
        IQualityRepository quality,
        IConsumoMaterialRepository consumos,
        IFichaRepository fichas)
    {
        _users = users;
        _sessions = sessions;
        _snapshots = snapshots;
        _grupos = grupos;
        _quality = quality;
        _consumos = consumos;
        _fichas = fichas;
    }

    public async Task<ActividadInstructorReporteDto> ConsultarAsync(
        ActividadInstructorConsulta consulta,
        CancellationToken cancellationToken = default)
    {
        var desde = consulta.Desde <= consulta.Hasta ? consulta.Desde : consulta.Hasta;
        var hasta = consulta.Desde <= consulta.Hasta ? consulta.Hasta : consulta.Desde;
        var tipo = NormalizarTipo(consulta.Tipo);
        var instructor = await _users.GetByIdAsync(consulta.InstructorId, cancellationToken);
        var nombre = instructor?.Nombre ?? $"Instructor #{consulta.InstructorId}";
        var fichas = await _fichas.GetAllAsync(cancellationToken);
        var fichaFiltro = EtiquetaFicha(fichas, consulta.FichaId);

        if (instructor is null)
        {
            return Vacio(consulta.InstructorId, nombre, null, desde, hasta, fichaFiltro, consulta.Jornada, tipo, false);
        }

        var eventos = new List<Evento>();
        var horas = TimeSpan.Zero;
        var unidades = 0;
        var prendas = 0;

        if (Incluye(tipo, ActividadInstructorTexto.Produccion))
        {
            var sesiones = await _sessions.GetAllWithDetailsAsync(cancellationToken);
            var snapshots = new Dictionary<int, IReadOnlyList<ProductionOrderBomSnapshot>>();
            foreach (var sesion in sesiones.Where(s => SesionDelInstructor(s, consulta.InstructorId) && EnRango(s.SessionDate, desde, hasta)))
            {
                if (!SesionPasaFiltro(sesion, consulta))
                    continue;

                if (!snapshots.TryGetValue(sesion.ProductionOrderId, out var receta))
                {
                    receta = await _snapshots.GetByOrderIdAsync(sesion.ProductionOrderId, cancellationToken);
                    snapshots[sesion.ProductionOrderId] = receta;
                }

                unidades += sesion.Units;
                var orden = sesion.ProductionOrder?.OrderNumber ?? $"Orden #{sesion.ProductionOrderId}";
                var producto = sesion.ProductionOrder?.ProductName;
                var ficha = EtiquetaFicha(sesion.Ficha);
                var texto = new StringBuilder();
                texto.Append("Producción: ").Append(sesion.Units).Append(sesion.Units == 1 ? " unidad" : " unidades");
                if (!string.IsNullOrWhiteSpace(producto))
                    texto.Append(" de ").Append(producto);
                texto.Append(" (").Append(orden).Append(')');
                if (ficha is not null)
                    texto.Append(", ficha ").Append(ficha);
                texto.Append(". Hora de registro ").Append(sesion.SessionDate.ToString("HH:mm", Es));
                texto.Append(". El ingreso y la salida de este registro no se guardan.");
                var estimado = ConsumoEstimado(receta, sesion.Units);
                if (estimado is not null)
                    texto.Append(' ').Append(estimado);

                eventos.Add(new Evento(
                    DateOnly.FromDateTime(sesion.SessionDate),
                    ActividadInstructorTexto.Produccion,
                    texto.ToString(),
                    ficha,
                    string.IsNullOrWhiteSpace(sesion.Observations) ? null : sesion.Observations.Trim(),
                    null,
                    null,
                    sesion.FichaId,
                    null));
            }
        }

        if (Incluye(tipo, ActividadInstructorTexto.Grupo))
        {
            var grupos = await _grupos.GetAllAsync(cancellationToken);
            foreach (var grupo in grupos.Where(g => g.InstructorUserId == consulta.InstructorId && EnRango(g.FechaRealizacion, desde, hasta)))
            {
                if (!OrdenPasaFiltro(grupo.ProductionOrderId, fichas, consulta))
                    continue;

                prendas += grupo.CantidadPrendas;
                TimeOnly? salida = grupo.HoraFin;
                if (salida is TimeOnly fin && fin > grupo.HoraInicio)
                    horas += fin - grupo.HoraInicio;

                var orden = grupo.ProductionOrder?.OrderNumber ?? $"Orden #{grupo.ProductionOrderId}";
                var producto = grupo.ProductionOrder?.ProductName;
                var texto = new StringBuilder();
                texto.Append("Grupo de confección: ").Append(grupo.CantidadPrendas)
                    .Append(grupo.CantidadPrendas == 1 ? " prenda" : " prendas");
                if (!string.IsNullOrWhiteSpace(producto))
                    texto.Append(" de ").Append(producto);
                texto.Append(" (").Append(orden).Append(").");
                var conQuien = $"Grupo de confección · {orden}";

                eventos.Add(new Evento(
                    grupo.FechaRealizacion,
                    ActividadInstructorTexto.Grupo,
                    texto.ToString(),
                    conQuien,
                    null,
                    grupo.HoraInicio,
                    salida,
                    null,
                    grupo.Id));
            }
        }

        if (Incluye(tipo, ActividadInstructorTexto.Calidad))
        {
            var inspecciones = await _quality.GetAllAsync(cancellationToken);
            foreach (var inspeccion in inspecciones.Where(q =>
                         NombreIgual(q.Responsable, nombre) && EnRango(q.InspectionDate, desde, hasta)))
            {
                if (!OrdenPasaFiltro(inspeccion.ProductionOrderId, fichas, consulta))
                    continue;

                var orden = inspeccion.ProductionOrder?.OrderNumber ?? $"Orden #{inspeccion.ProductionOrderId}";
                var texto = $"Control de calidad: {inspeccion.UnitsInspected} unidades de {orden}, resultado {EtiquetaCalidad(inspeccion.Result)}.";
                eventos.Add(new Evento(
                    inspeccion.InspectionDate,
                    ActividadInstructorTexto.Calidad,
                    texto,
                    $"Orden {orden}",
                    string.IsNullOrWhiteSpace(inspeccion.MotivoReproceso) ? null : inspeccion.MotivoReproceso.Trim(),
                    null,
                    null,
                    null,
                    null));
            }
        }

        if (Incluye(tipo, ActividadInstructorTexto.Consumo))
        {
            var consumos = await _consumos.GetAllWithDetailsAsync(cancellationToken);
            foreach (var consumo in consumos.Where(c =>
                         c.ResponsableUserId == consulta.InstructorId && EnRango(c.FechaUtc, desde, hasta)))
            {
                if (!OrdenPasaFiltro(consumo.ProductionOrderId, fichas, consulta))
                    continue;

                var orden = consumo.ProductionOrder?.OrderNumber ?? $"Orden #{consumo.ProductionOrderId}";
                var material = consumo.Material?.Name ?? $"Material #{consumo.MaterialId}";
                var unidad = consumo.Material is { } mat ? UnitHelper.ToDisplay(mat.Unit) : "unidad";
                var texto = $"Consumo de materiales: {material}, {consumo.Cantidad.ToString("0.##", CultureInfo.InvariantCulture)} {unidad}, orden {orden}.";
                eventos.Add(new Evento(
                    DateOnly.FromDateTime(consumo.FechaUtc),
                    ActividadInstructorTexto.Consumo,
                    texto,
                    consumo.GrupoConfeccionId is int
                        ? $"Grupo de confección · {orden}"
                        : $"Orden {orden}",
                    null,
                    null,
                    null,
                    null,
                    consumo.GrupoConfeccionId));
            }
        }

        var dias = new List<ActividadInstructorDiaDto>();
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            var delDia = eventos.Where(e => e.Fecha == fecha).ToList();
            dias.Add(ArmarDia(fecha, delDia));
        }

        var realizadas = ActividadInstructorTexto.TiposConsultables
            .Where(t => Incluye(tipo, t) && eventos.Any(e => e.Tipo == t))
            .Select(ActividadInstructorTexto.EtiquetaTipo)
            .ToList();
        var noRealizadas = ActividadInstructorTexto.TiposConsultables
            .Where(t => Incluye(tipo, t) && eventos.All(e => e.Tipo != t))
            .Select(ActividadInstructorTexto.EtiquetaTipo)
            .ToList();

        var diasCon = dias.Count(d => d.ConActividad);
        var reporte = new ActividadInstructorReporteDto(
            true,
            consulta.InstructorId,
            nombre,
            instructor.PhotoPath,
            Iniciales(nombre),
            desde,
            hasta,
            Periodo(desde, hasta),
            fichaFiltro,
            string.IsNullOrWhiteSpace(consulta.Jornada) ? null : consulta.Jornada.Trim(),
            tipo is null ? null : ActividadInstructorTexto.EtiquetaTipo(tipo),
            diasCon > 0,
            dias.Count,
            diasCon,
            dias.Count - diasCon,
            FormatoHoras(horas),
            ActividadInstructorTexto.NoRegistrado,
            eventos.Select(e => e.FichaId).Where(id => id is > 0).Distinct().Count()
                + eventos.Select(e => e.GrupoId).Where(id => id is > 0).Distinct().Count(),
            unidades,
            prendas,
            realizadas,
            noRealizadas,
            dias,
            "");

        return reporte with { CodigoVerificacion = Codigo(reporte) };
    }

    private static ActividadInstructorDiaDto ArmarDia(DateOnly fecha, IReadOnlyList<Evento> eventos)
    {
        var ingresos = eventos.Where(e => e.Ingreso is not null).Select(e => e.Ingreso!.Value).ToList();
        var ultimoBloque = eventos
            .Where(e => e.Ingreso is not null)
            .OrderByDescending(e => e.Ingreso)
            .FirstOrDefault();
        var salidaDelUltimo = ultimoBloque?.Salida;

        return new ActividadInstructorDiaDto(
            fecha,
            Capitalizar(fecha.ToString("dddd d 'de' MMMM 'de' yyyy", Es)),
            eventos.Count > 0,
            ingresos.Count == 0 ? ActividadInstructorTexto.NoRegistrado : ingresos.Min().ToString("HH:mm", Es),
            salidaDelUltimo is TimeOnly fin
                ? fin.ToString("HH:mm", Es)
                : ActividadInstructorTexto.NoRegistrado,
            eventos.Select(e => e.Texto).Distinct(StringComparer.Ordinal).ToList(),
            eventos.Select(e => e.ConQuien).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToList(),
            ActividadInstructorTexto.NoRegistrado,
            eventos.Select(e => e.Observacion).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct(StringComparer.OrdinalIgnoreCase).Cast<string>().ToList());
    }

    private static ActividadInstructorReporteDto Vacio(
        int instructorId,
        string nombre,
        string? photo,
        DateOnly desde,
        DateOnly hasta,
        string? ficha,
        string? jornada,
        string? tipo,
        bool existe)
    {
        var dias = new List<ActividadInstructorDiaDto>();
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            dias.Add(ArmarDia(fecha, []));

        var noRealizadas = ActividadInstructorTexto.TiposConsultables
            .Where(t => Incluye(tipo, t))
            .Select(ActividadInstructorTexto.EtiquetaTipo)
            .ToList();

        var reporte = new ActividadInstructorReporteDto(
            existe,
            instructorId,
            nombre,
            photo,
            Iniciales(nombre),
            desde,
            hasta,
            Periodo(desde, hasta),
            ficha,
            string.IsNullOrWhiteSpace(jornada) ? null : jornada.Trim(),
            tipo is null ? null : ActividadInstructorTexto.EtiquetaTipo(tipo),
            false,
            dias.Count,
            0,
            dias.Count,
            ActividadInstructorTexto.NoRegistrado,
            ActividadInstructorTexto.NoRegistrado,
            0,
            0,
            0,
            [],
            noRealizadas,
            dias,
            "");
        return reporte with { CodigoVerificacion = Codigo(reporte) };
    }

    private static bool SesionDelInstructor(ProductionSession sesion, int instructorId) =>
        sesion.RegisteredByUserId == instructorId || FichaDelInstructor(sesion.Ficha, instructorId);

    private static bool SesionPasaFiltro(ProductionSession sesion, ActividadInstructorConsulta consulta)
    {
        if (consulta.FichaId is int fichaId && fichaId > 0 && sesion.FichaId != fichaId)
            return false;
        if (!string.IsNullOrWhiteSpace(consulta.Jornada)
            && !string.Equals(sesion.Ficha?.Turno, consulta.Jornada.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private static bool OrdenPasaFiltro(
        int orderId,
        IReadOnlyList<Ficha> fichas,
        ActividadInstructorConsulta consulta)
    {
        var pideFicha = consulta.FichaId is > 0;
        var pideJornada = !string.IsNullOrWhiteSpace(consulta.Jornada);
        if (!pideFicha && !pideJornada)
            return true;

        var candidatas = fichas.Where(f => f.ProductionOrderId == orderId);
        if (consulta.FichaId is int fichaId && fichaId > 0)
            candidatas = candidatas.Where(f => f.Id == fichaId);
        if (pideJornada)
            candidatas = candidatas.Where(f =>
                string.Equals(f.Turno, consulta.Jornada!.Trim(), StringComparison.OrdinalIgnoreCase));
        return candidatas.Any();
    }

    private static bool FichaDelInstructor(Ficha? ficha, int instructorId)
    {
        if (ficha is null)
            return false;
        if (ficha.InstructorUserId == instructorId)
            return true;
        return ficha.Instructors?.Any(i => i.UserId == instructorId) == true;
    }

    private static string? EtiquetaFicha(IReadOnlyList<Ficha> fichas, int? fichaId)
    {
        if (fichaId is not int id || id <= 0)
            return null;
        var ficha = fichas.FirstOrDefault(f => f.Id == id);
        return ficha is null ? $"Ficha #{id}" : EtiquetaFicha(ficha);
    }

    private static string? EtiquetaFicha(Ficha? ficha)
    {
        if (ficha is null || string.IsNullOrWhiteSpace(ficha.NumeroGrupo))
            return null;
        var texto = string.IsNullOrWhiteSpace(ficha.ProcessName)
            ? ficha.NumeroGrupo
            : $"{ficha.NumeroGrupo} · {ficha.ProcessName}";
        if (!string.IsNullOrWhiteSpace(ficha.Turno))
            texto += $" · {ficha.Turno}";
        return texto;
    }

    private static string? ConsumoEstimado(IReadOnlyList<ProductionOrderBomSnapshot> receta, int unidades)
    {
        if (receta.Count == 0 || unidades == 0)
            return null;
        var partes = receta
            .OrderBy(l => l.MaterialName, StringComparer.OrdinalIgnoreCase)
            .Select(l => $"{l.MaterialName} {(l.QuantityPerUnit * unidades).ToString("0.##", CultureInfo.InvariantCulture)} {UnitHelper.ToDisplay(l.Unit)}");
        return "Consumo estimado por la receta: " + string.Join(", ", partes) + ".";
    }

    private static bool Incluye(string? filtro, string tipo) =>
        filtro is null || string.Equals(filtro, tipo, StringComparison.Ordinal);

    private static string? NormalizarTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return null;
        var limpio = tipo.Trim().ToLowerInvariant();
        return ActividadInstructorTexto.TiposConsultables.Contains(limpio) ? limpio : null;
    }

    private static bool EnRango(DateTime fecha, DateOnly desde, DateOnly hasta) =>
        EnRango(DateOnly.FromDateTime(fecha), desde, hasta);

    private static bool EnRango(DateOnly fecha, DateOnly desde, DateOnly hasta) =>
        fecha >= desde && fecha <= hasta;

    private static bool NombreIgual(string? responsable, string nombre) =>
        !string.IsNullOrWhiteSpace(responsable)
        && string.Equals(responsable.Trim(), nombre.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string EtiquetaCalidad(QualityResult resultado) => resultado switch
    {
        QualityResult.Aprobada => "Aprobada",
        QualityResult.Reproceso => "Reproceso",
        QualityResult.Rechazada => "Rechazada",
        _ => resultado.ToString()
    };

    private static string FormatoHoras(TimeSpan total)
    {
        if (total <= TimeSpan.Zero)
            return ActividadInstructorTexto.NoRegistrado;
        var horas = (int)total.TotalHours;
        var minutos = total.Minutes;
        if (minutos == 0)
            return horas == 1 ? "1 h" : $"{horas} h";
        return $"{horas} h {minutos} min";
    }

    private static string Iniciales(string nombre)
    {
        var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (partes.Length == 0)
            return "?";
        if (partes.Length == 1)
            return partes[0][..1].ToUpper(Es);
        return string.Concat(char.ToUpper(partes[0][0], Es), char.ToUpper(partes[1][0], Es));
    }

    private static string Periodo(DateOnly desde, DateOnly hasta) =>
        desde == hasta
            ? Capitalizar(desde.ToString("d 'de' MMMM 'de' yyyy", Es))
            : $"{Capitalizar(desde.ToString("d 'de' MMMM 'de' yyyy", Es))} – {Capitalizar(hasta.ToString("d 'de' MMMM 'de' yyyy", Es))}";

    private static string Capitalizar(string texto) =>
        string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], Es) + texto[1..];

    private static string Codigo(ActividadInstructorReporteDto reporte)
    {
        var contenido = new StringBuilder();
        contenido.Append(reporte.InstructorId).Append('|')
            .Append(reporte.Desde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
            .Append(reporte.Hasta.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
            .Append(reporte.FichaFiltro ?? "").Append('|')
            .Append(reporte.JornadaFiltro ?? "").Append('|')
            .Append(reporte.TipoFiltro ?? "").Append('|')
            .Append(reporte.UnidadesEnSesiones).Append('|')
            .Append(reporte.PrendasEnGrupos).Append('|')
            .Append(reporte.HorasRegistradas).Append('|');
        foreach (var dia in reporte.Dias)
        {
            contenido.Append(dia.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
                .Append(dia.Ingreso).Append(';')
                .Append(dia.Salida).Append(';')
                .Append(string.Join(",", dia.Actividades)).Append('|');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(contenido.ToString()));
        return Convert.ToHexString(hash)[..12];
    }

    private sealed record Evento(
        DateOnly Fecha,
        string Tipo,
        string Texto,
        string? ConQuien,
        string? Observacion,
        TimeOnly? Ingreso,
        TimeOnly? Salida,
        int? FichaId,
        int? GrupoId);
}
