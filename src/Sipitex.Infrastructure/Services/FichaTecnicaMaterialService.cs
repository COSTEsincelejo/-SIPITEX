using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Services;

public sealed class FichaTecnicaMaterialService : IFichaTecnicaMaterialService
{
    private readonly SipitexDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IActivityLogService _activityLog;

    public FichaTecnicaMaterialService(SipitexDbContext db, IUnitOfWork uow, IActivityLogService activityLog)
    {
        _db = db;
        _uow = uow;
        _activityLog = activityLog;
    }

    public async Task<FichaTecnicaMaterialDetalle?> ObtenerAsync(
        int materialId,
        CancellationToken cancellationToken = default)
    {
        var material = await _db.Materials.AsNoTracking()
            .Include(m => m.PlantaInventario)
            .Include(m => m.FichaTecnica)
            .FirstOrDefaultAsync(m => m.Id == materialId, cancellationToken);
        if (material is null)
            return null;

        var ficha = material.FichaTecnica;
        return new FichaTecnicaMaterialDetalle
        {
            MaterialId = material.Id,
            PlantaInventarioId = material.PlantaInventarioId,
            Planta = material.PlantaInventario?.Nombre ?? string.Empty,
            Codigo = material.Code,
            CodigoAlmacen = material.CodigoAlmacen,
            Nombre = material.Name,
            Referencia = material.Referencia,
            Unidad = UnitHelper.ToDisplay(material.Unit),
            TieneFicha = ficha is not null,
            Ficha = new FichaTecnicaMaterialDatos
            {
                DescripcionTecnica = ficha?.DescripcionTecnica,
                TipoMaterial = ficha?.TipoMaterial,
                Composicion = ficha?.Composicion,
                GramajeGm2 = DecimalTexto.Formato(ficha?.GramajeGm2),
                AnchoCm = DecimalTexto.Formato(ficha?.AnchoCm),
                Colores = ficha?.Colores,
                Acabados = ficha?.Acabados,
                EspecificacionesUso = ficha?.EspecificacionesUso,
                ProcesoFabricacion = ficha?.ProcesoFabricacion,
                NormasCertificaciones = ficha?.NormasCertificaciones,
                EnlaceFichaOriginal = ficha?.EnlaceFichaOriginal,
                Observaciones = ficha?.Observaciones
            }
        };
    }

    public async Task<FichaTecnicaMaterialResult> GuardarAsync(
        GuardarFichaTecnicaMaterialDto dto,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var material = await _db.Materials
            .FirstOrDefaultAsync(m => m.Id == dto.MaterialId, cancellationToken);
        if (material is null)
            return FichaTecnicaMaterialResult.Fail("No se encontró el material.");

        if (!FichaTecnicaTexto.TryLeer(dto, out var datos, out var errores))
            return FichaTecnicaMaterialResult.Fail("Revise los datos de la ficha técnica.", errores);

        var ficha = await _db.FichasTecnicasMaterial
            .FirstOrDefaultAsync(f => f.MaterialId == material.Id, cancellationToken);
        var crear = ficha is null;
        if (ficha is null)
        {
            ficha = new FichaTecnicaMaterial { MaterialId = material.Id };
            _db.FichasTecnicasMaterial.Add(ficha);
        }

        Copiar(ficha, datos);
        await _uow.SaveChangesAsync(cancellationToken);

        await _activityLog.LogAsync(
            actorUserId,
            crear ? ActivityLogActions.CreateFichaTecnicaMaterial : ActivityLogActions.UpdateFichaTecnicaMaterial,
            ActivityLogEntities.FichaTecnicaMaterial,
            ficha.Id.ToString(),
            $"Ficha técnica de {material.Name}.",
            cancellationToken);

        return FichaTecnicaMaterialResult.Ok(crear
            ? "Se creó la ficha técnica."
            : "Se actualizó la ficha técnica.");
    }

    private static void Copiar(FichaTecnicaMaterial ficha, FichaTecnicaLeida datos)
    {
        ficha.DescripcionTecnica = datos.DescripcionTecnica;
        ficha.TipoMaterial = datos.TipoMaterial;
        ficha.Composicion = datos.Composicion;
        ficha.GramajeGm2 = datos.GramajeGm2;
        ficha.AnchoCm = datos.AnchoCm;
        ficha.Colores = datos.Colores;
        ficha.Acabados = datos.Acabados;
        ficha.EspecificacionesUso = datos.EspecificacionesUso;
        ficha.ProcesoFabricacion = datos.ProcesoFabricacion;
        ficha.NormasCertificaciones = datos.NormasCertificaciones;
        ficha.EnlaceFichaOriginal = datos.EnlaceFichaOriginal;
        ficha.Observaciones = datos.Observaciones;
    }
}
