using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Gestión de fichas técnicas (CRUD de BomProduct + líneas) y asignación a instructores
public class BomCatalogService : IBomCatalogService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BomCatalogService(
        IBomRepository bomRepository,
        IMaterialRepository materialRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BomProductListItemDto>> GetProductsAsync(
        int? assignedInstructorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var products = await _bomRepository.GetProductsAsync(cancellationToken);
        IEnumerable<BomProduct> query = products;
        if (assignedInstructorUserId is int instructorId)
            query = query.Where(p => p.Instructors.Any(i => i.UserId == instructorId));

        return query.Select(MapListItem).ToList();
    }

    public async Task<BomProductDetailDto?> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _bomRepository.GetProductByIdAsync(id, cancellationToken);
        if (product is null) return null;

        return new BomProductDetailDto(
            product.Id,
            product.ProductName,
            product.IsReference,
            product.Notes,
            product.HabilitadoParaOrdenes,
            product.Items.Select(i => new BomRecipeLineDetailDto(
                i.Id,
                i.MaterialId,
                i.Material.Name,
                i.QuantityPerUnit,
                i.Unit,
                UnitHelper.ToDisplay(i.Unit))).ToList(),
            product.Referencia,
            product.Linea,
            product.TallaInicial,
            product.TipoEmpaque,
            product.DescripcionPrenda,
            product.FechaSolicitud,
            product.FechaElaboracion,
            product.AnioMuestrario,
            product.EsDisenoNuevo,
            product.EsReplica,
            product.EsBancoDeMuestras,
            product.Disenador,
            product.Patronista,
            product.Digitacion,
            product.Tallas
                .OrderBy(t => t.Orden)
                .ThenBy(t => t.Id)
                .Select(t => new BomProductTallaDto(t.Id, t.Nombre, t.Orden))
                .ToList(),
            product.Piezas
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Id)
                .Select(p => new BomProductPiezaDto(p.Id, p.Nombre, p.Cantidad, p.Tela, p.Orden))
                .ToList(),
            product.Medidas
                .OrderBy(m => m.Tipo)
                .ThenBy(m => m.Orden)
                .ThenBy(m => m.Id)
                .Select(MapMedida)
                .ToList());
    }

    private static BomProductMedidaDto MapMedida(BomProductMedida m) => new(
        m.Id,
        m.Tipo,
        m.Codigo,
        m.Descripcion,
        m.Tolerancia,
        m.ComoMedir,
        m.Orden,
        m.Valores
            .OrderBy(v => v.Talla?.Orden ?? 0)
            .Select(v => new BomProductMedidaValorDto(
                v.BomProductTallaId,
                v.Talla?.Orden ?? 0,
                v.Talla?.Nombre,
                v.Valor))
            .ToList());

    public async Task<IReadOnlyList<string>> GetOrderEligibleProductNamesAsync(CancellationToken cancellationToken = default)
    {
        var products = await _bomRepository.GetProductsAsync(cancellationToken);
        return products
            .Where(p => p.HabilitadoParaOrdenes && p.Items.Count > 0)
            .Select(p => p.ProductName)
            .OrderBy(n => n)
            .ToList();
    }

    public async Task<ServiceResult> CreateAsync(UpsertBomProductDto dto, CancellationToken cancellationToken = default)
    {
        var validation = ValidateDto(dto);
        if (validation is not null) return validation;

        var name = dto.ProductName.Trim();
        if (await _bomRepository.GetProductByNameAsync(name, cancellationToken) is not null)
            return ServiceResult.Fail($"Ya existe una ficha técnica para «{name}».");

        var linesResult = await ResolveLinesAsync(dto.Lines, cancellationToken);
        if (!linesResult.Success)
            return ServiceResult.Fail(linesResult.Error!);

        var product = new BomProduct
        {
            ProductName = name,
            IsReference = dto.IsReference,
            Notes = NormalizeNotes(dto.Notes, dto.IsReference),
            HabilitadoParaOrdenes = dto.HabilitadoParaOrdenes
        };
        ApplyMetadata(product, dto);
        ApplyTallas(product, dto.Tallas);

        var patronaje = ApplyPatronaje(product, dto.Piezas, dto.Medidas);
        if (patronaje is not null) return patronaje;

        foreach (var line in linesResult.Lines!)
        {
            product.Items.Add(new BomItem
            {
                ProductName = name,
                MaterialId = line.MaterialId,
                QuantityPerUnit = line.QuantityPerUnit,
                Unit = line.Unit
            });
        }

        await _bomRepository.AddProductAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Ficha técnica «{name}» creada.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, UpsertBomProductDto dto, CancellationToken cancellationToken = default)
    {
        var validation = ValidateDto(dto);
        if (validation is not null) return validation;

        var product = await _bomRepository.GetProductByIdAsync(id, cancellationToken);
        if (product is null) return ServiceResult.Fail("Ficha técnica no encontrada.");

        var name = dto.ProductName.Trim();
        var other = await _bomRepository.GetProductByNameAsync(name, cancellationToken);
        if (other is not null && other.Id != id)
            return ServiceResult.Fail($"Ya existe una ficha técnica para «{name}».");

        var linesResult = await ResolveLinesAsync(dto.Lines, cancellationToken);
        if (!linesResult.Success)
            return ServiceResult.Fail(linesResult.Error!);

        product.ProductName = name;
        product.IsReference = dto.IsReference;
        product.Notes = NormalizeNotes(dto.Notes, dto.IsReference);
        product.HabilitadoParaOrdenes = dto.HabilitadoParaOrdenes;
        ApplyMetadata(product, dto);

        // Quitar medidas primero (evita FK huérfanas al reemplazar tallas)
        foreach (var old in product.Medidas.ToList())
            _bomRepository.RemoveMedida(old);
        product.Medidas.Clear();

        foreach (var old in product.Piezas.ToList())
            _bomRepository.RemovePieza(old);
        product.Piezas.Clear();

        // Reemplazo de tallas: cascade borra BomProductMedidaValor de tallas eliminadas
        foreach (var old in product.Tallas.ToList())
            _bomRepository.RemoveTalla(old);
        product.Tallas.Clear();
        ApplyTallas(product, dto.Tallas);

        var patronaje = ApplyPatronaje(product, dto.Piezas, dto.Medidas);
        if (patronaje is not null) return patronaje;

        // Reemplazo completo de líneas: quitar viejas, agregar nuevas
        foreach (var old in product.Items.ToList())
            _bomRepository.RemoveItem(old);

        product.Items.Clear();
        foreach (var line in linesResult.Lines!)
        {
            product.Items.Add(new BomItem
            {
                BomProductId = product.Id,
                ProductName = name,
                MaterialId = line.MaterialId,
                QuantityPerUnit = line.QuantityPerUnit,
                Unit = line.Unit
            });
        }

        _bomRepository.UpdateProduct(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Ficha técnica «{name}» actualizada.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _bomRepository.GetProductByIdAsync(id, cancellationToken);
        if (product is null) return ServiceResult.Fail("Ficha técnica no encontrada.");

        var name = product.ProductName;
        _bomRepository.RemoveProduct(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Ficha técnica «{name}» eliminada. El producto queda sin BOM.");
    }

    public async Task<ServiceResult> AssignInstructorAsync(
        int bomProductId,
        int instructorUserId,
        CancellationToken cancellationToken = default)
    {
        var product = await _bomRepository.GetProductByIdAsync(bomProductId, cancellationToken);
        if (product is null) return ServiceResult.Fail("Ficha técnica no encontrada.");

        var user = await _userRepository.GetByIdAsync(instructorUserId, cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult.Fail("Instructor no encontrado o inactivo.");
        if (!string.Equals(user.Rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
            return ServiceResult.Fail("Solo se pueden asignar usuarios con rol Instructor.");

        if (product.Instructors.Any(i => i.UserId == instructorUserId))
            return ServiceResult.Fail("Ese instructor ya está asignado a la ficha técnica.");

        product.Instructors.Add(new BomProductInstructor
        {
            BomProductId = product.Id,
            UserId = instructorUserId,
            User = user,
            AssignedAtUtc = DateTime.UtcNow
        });

        _bomRepository.UpdateProduct(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"{user.Nombre} asignado a la ficha técnica «{product.ProductName}».");
    }

    public async Task<ServiceResult> RemoveInstructorAsync(
        int bomProductId,
        int instructorUserId,
        CancellationToken cancellationToken = default)
    {
        var product = await _bomRepository.GetProductByIdAsync(bomProductId, cancellationToken);
        if (product is null) return ServiceResult.Fail("Ficha técnica no encontrada.");

        var assignment = product.Instructors.FirstOrDefault(i => i.UserId == instructorUserId);
        if (assignment is null)
            return ServiceResult.Fail("Ese instructor no está asignado a la ficha técnica.");

        product.Instructors.Remove(assignment);
        _bomRepository.UpdateProduct(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Instructor quitado de la ficha técnica.");
    }

    public async Task<IReadOnlyList<InstructorOptionDto>> GetAssignableInstructorsAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users
            .Where(u => u.IsActive && string.Equals(u.Rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.Nombre)
            .Select(u => new InstructorOptionDto(u.Id, u.Nombre))
            .ToList();
    }

    private static BomProductListItemDto MapListItem(BomProduct p) => new(
        p.Id,
        p.ProductName,
        p.Items.Count,
        p.IsReference,
        p.HabilitadoParaOrdenes,
        p.Notes,
        p.Instructors
            .OrderBy(i => i.User?.Nombre ?? string.Empty)
            .Select(i => new BomProductInstructorDto(i.UserId, i.User?.Nombre ?? $"#{i.UserId}"))
            .ToList());

    private static ServiceResult? ValidateDto(UpsertBomProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductName))
            return ServiceResult.Fail("El nombre del producto es obligatorio.");
        if (dto.Lines is null || dto.Lines.Count == 0)
            return ServiceResult.Fail("La receta debe tener al menos un material. Para dejar el producto sin BOM use Eliminar.");

        var hasMeaningfulMedidas = dto.Medidas is { Count: > 0 }
            && dto.Medidas.Any(m => !string.IsNullOrWhiteSpace(m.Codigo) || !string.IsNullOrWhiteSpace(m.Descripcion));
        var tallasCount = CountNamedTallas(dto.Tallas);
        if (hasMeaningfulMedidas && tallasCount == 0)
        {
            return ServiceResult.Fail(
                "Debe agregar al menos una talla antes de registrar la tabla de medidas.");
        }

        return null;
    }

    private static int CountNamedTallas(IReadOnlyList<BomProductTallaDto>? tallas) =>
        tallas?.Count(t => !string.IsNullOrWhiteSpace(t.Nombre)) ?? 0;

    private static void ApplyMetadata(BomProduct product, UpsertBomProductDto dto)
    {
        product.Referencia = NormalizeOptional(dto.Referencia);
        product.Linea = NormalizeOptional(dto.Linea);
        product.TallaInicial = NormalizeOptional(dto.TallaInicial);
        product.TipoEmpaque = NormalizeOptional(dto.TipoEmpaque);
        product.DescripcionPrenda = NormalizeOptional(dto.DescripcionPrenda);
        product.FechaSolicitud = dto.FechaSolicitud;
        product.FechaElaboracion = dto.FechaElaboracion;
        product.AnioMuestrario = dto.AnioMuestrario is > 0 ? dto.AnioMuestrario : null;
        product.EsDisenoNuevo = dto.EsDisenoNuevo;
        product.EsReplica = dto.EsReplica;
        product.EsBancoDeMuestras = dto.EsBancoDeMuestras;
        product.Disenador = NormalizeOptional(dto.Disenador);
        product.Patronista = NormalizeOptional(dto.Patronista);
        product.Digitacion = NormalizeOptional(dto.Digitacion);
    }

    private static void ApplyTallas(BomProduct product, IReadOnlyList<BomProductTallaDto>? tallas)
    {
        if (tallas is null || tallas.Count == 0)
            return;

        var orden = 0;
        foreach (var t in tallas)
        {
            var nombre = NormalizeOptional(t.Nombre);
            if (nombre is null)
                continue;

            product.Tallas.Add(new BomProductTalla
            {
                BomProductId = product.Id,
                Nombre = nombre,
                Orden = t.Orden >= 0 ? t.Orden : orden
            });
            orden++;
        }
    }

    // Piezas + medidas/valores. Usa navegación a Talla (no solo Id) para Create sin IDs aún.
    private static ServiceResult? ApplyPatronaje(
        BomProduct product,
        IReadOnlyList<BomProductPiezaDto>? piezas,
        IReadOnlyList<BomProductMedidaDto>? medidas)
    {
        if (piezas is not null)
        {
            var orden = 0;
            foreach (var p in piezas)
            {
                var nombre = NormalizeOptional(p.Nombre);
                if (nombre is null)
                    continue;
                if (p.Cantidad <= 0)
                    return ServiceResult.Fail($"La cantidad de la pieza «{nombre}» debe ser mayor a cero.");

                product.Piezas.Add(new BomProductPieza
                {
                    BomProductId = product.Id,
                    Nombre = nombre,
                    Cantidad = p.Cantidad,
                    Tela = NormalizeOptional(p.Tela) ?? string.Empty,
                    Orden = p.Orden >= 0 ? p.Orden : orden
                });
                orden++;
            }
        }

        if (medidas is null || medidas.Count == 0)
            return null;

        var tallas = product.Tallas.OrderBy(t => t.Orden).ThenBy(t => t.Nombre).ToList();
        var meaningful = medidas
            .Where(m => !string.IsNullOrWhiteSpace(m.Codigo) || !string.IsNullOrWhiteSpace(m.Descripcion))
            .ToList();

        if (meaningful.Count == 0)
            return null;

        if (tallas.Count == 0)
        {
            return ServiceResult.Fail(
                "Debe agregar al menos una talla antes de registrar la tabla de medidas.");
        }

        var ordenMed = 0;
        foreach (var m in meaningful)
        {
            var codigo = NormalizeOptional(m.Codigo);
            var descripcion = NormalizeOptional(m.Descripcion);
            if (codigo is null || descripcion is null)
                return ServiceResult.Fail("Cada medida requiere código y descripción.");

            var medida = new BomProductMedida
            {
                BomProductId = product.Id,
                Tipo = m.Tipo,
                Codigo = codigo,
                Descripcion = descripcion,
                Tolerancia = NormalizeOptional(m.Tolerancia),
                ComoMedir = NormalizeOptional(m.ComoMedir),
                Orden = m.Orden >= 0 ? m.Orden : ordenMed
            };

            var seenTallas = new HashSet<BomProductTalla>();
            foreach (var v in m.Valores ?? [])
            {
                var talla = ResolveTalla(tallas, v);
                if (talla is null)
                {
                    return ServiceResult.Fail(
                        $"No se pudo asociar el valor de la medida «{codigo}» a una talla definida.");
                }

                if (!seenTallas.Add(talla))
                    continue;

                medida.Valores.Add(new BomProductMedidaValor
                {
                    Medida = medida,
                    Talla = talla,
                    Valor = v.Valor
                });
            }

            product.Medidas.Add(medida);
            ordenMed++;
        }

        return null;
    }

    private static BomProductTalla? ResolveTalla(
        IReadOnlyList<BomProductTalla> tallas,
        BomProductMedidaValorDto v)
    {
        if (v.TallaId is int tid and > 0)
        {
            var byId = tallas.FirstOrDefault(t => t.Id == tid);
            if (byId is not null) return byId;
        }

        if (!string.IsNullOrWhiteSpace(v.TallaNombre))
        {
            var byName = tallas.FirstOrDefault(t =>
                string.Equals(t.Nombre, v.TallaNombre.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byName is not null) return byName;
        }

        return tallas.FirstOrDefault(t => t.Orden == v.TallaOrden)
               ?? (v.TallaOrden >= 0 && v.TallaOrden < tallas.Count ? tallas[v.TallaOrden] : null);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeNotes(string? notes, bool isReference)
    {
        var trimmed = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (!isReference) return trimmed;
        if (trimmed is null)
            return "Valores de referencia, pendientes de validar con instructor de Trazo.";
        return trimmed;
    }

    private async Task<(bool Success, string? Error, List<(int MaterialId, decimal QuantityPerUnit, MaterialUnit Unit)>? Lines)> ResolveLinesAsync(
        IReadOnlyList<BomRecipeLineDto> lines,
        CancellationToken cancellationToken)
    {
        var resolved = new List<(int MaterialId, decimal QuantityPerUnit, MaterialUnit Unit)>();
        var seenMaterials = new HashSet<int>();

        foreach (var line in lines)
        {
            if (line.QuantityPerUnit <= 0)
                return (false, "La cantidad por unidad debe ser mayor a cero.", null);

            int materialId;
            MaterialUnit unit = line.Unit;

            if (line.MaterialId is > 0)
            {
                var material = await _materialRepository.GetByIdAsync(line.MaterialId.Value, cancellationToken);
                if (material is null)
                    return (false, "Material no encontrado en el catálogo.", null);
                materialId = material.Id;
                unit = material.Unit;
            }
            else if (!string.IsNullOrWhiteSpace(line.NewMaterialName) && line.NewMaterialUnit is not null)
            {
                var created = new Material
                {
                    Code = $"mat{DateTime.UtcNow.Ticks}",
                    Name = line.NewMaterialName.Trim(),
                    Unit = line.NewMaterialUnit.Value,
                    Stock = 0,
                    MinStock = 10,
                    Status = MaterialStatus.Bueno,
                    LastEntryDate = DateOnly.FromDateTime(DateTime.Today)
                };
                await _materialRepository.AddAsync(created, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                materialId = created.Id;
                unit = created.Unit;
            }
            else
            {
                return (false, "Cada línea debe elegir un material existente o indicar uno nuevo (nombre + unidad).", null);
            }

            if (!seenMaterials.Add(materialId))
                return (false, "No se puede repetir el mismo material en la receta.", null);

            resolved.Add((materialId, line.QuantityPerUnit, unit));
        }

        return (true, null, resolved);
    }
}
