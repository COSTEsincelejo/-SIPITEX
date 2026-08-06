using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Gestión de fichas técnicas (CRUD de BomProduct + líneas)
public class BomCatalogService : IBomCatalogService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BomCatalogService(
        IBomRepository bomRepository,
        IMaterialRepository materialRepository,
        IUnitOfWork unitOfWork)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BomProductListItemDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _bomRepository.GetProductsAsync(cancellationToken);
        return products.Select(p => new BomProductListItemDto(
            p.Id,
            p.ProductName,
            p.Items.Count,
            p.IsReference,
            p.HabilitadoParaOrdenes,
            p.Notes)).ToList();
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
                UnitHelper.ToDisplay(i.Unit))).ToList());
    }

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

    private static ServiceResult? ValidateDto(UpsertBomProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductName))
            return ServiceResult.Fail("El nombre del producto es obligatorio.");
        if (dto.Lines is null || dto.Lines.Count == 0)
            return ServiceResult.Fail("La receta debe tener al menos un material. Para dejar el producto sin BOM use Eliminar.");
        return null;
    }

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
