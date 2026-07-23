using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class MrpService : IMrpService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MrpService(
        IBomRepository bomRepository,
        IMaterialRepository materialRepository,
        IProductionOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BomItemDto>> GetBomAsync(CancellationToken cancellationToken = default)
    {
        var items = await _bomRepository.GetAllAsync(cancellationToken);
        return items.Select(i => new BomItemDto(
            i.ProductName,
            i.Material.Name,
            i.QuantityPerUnit,
            UnitHelper.ToDisplay(i.Unit))).ToList();
    }

    public async Task<MrpSimulationResultDto> SimulateAsync(string productName, decimal quantity, CancellationToken cancellationToken = default)
    {
        var name = (productName ?? string.Empty).Trim();
        var recipe = await _bomRepository.GetByProductAsync(name, cancellationToken);
        var lines = new List<MrpLineDto>();

        foreach (var item in recipe)
        {
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            var available = material?.Stock ?? 0;
            var required = item.QuantityPerUnit * quantity;
            var deficit = Math.Max(0, required - available);

            lines.Add(new MrpLineDto(
                item.Material.Name,
                required,
                available,
                deficit,
                UnitHelper.ToDisplay(item.Unit),
                deficit <= 0));
        }

        return new MrpSimulationResultDto(name, quantity, lines);
    }

    public async Task<bool> ProductHasBomAsync(string productName, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        return recipe.Count > 0;
    }

    public async Task<IReadOnlyList<string>> GetKnownProductNamesAsync(CancellationToken cancellationToken = default)
    {
        var fromBom = await _bomRepository.GetDistinctProductNamesAsync(cancellationToken);
        var fromOrders = await _orderRepository.GetDistinctProductNamesAsync(cancellationToken);
        return fromBom
            .Concat(fromOrders)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<ServiceResult> AddBomItemAsync(string productName, string materialName, decimal quantityPerUnit, MaterialUnit unit = MaterialUnit.Metros, CancellationToken cancellationToken = default)
    {
        var name = (productName ?? string.Empty).Trim();
        var matName = (materialName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult.Fail("Indique el nombre del producto.");
        if (string.IsNullOrWhiteSpace(matName))
            return ServiceResult.Fail("Indique el material que necesita (puede escribir cualquiera).");
        if (name.Length > 80 || matName.Length > 80)
            return ServiceResult.Fail("Nombre demasiado largo (máx. 80).");
        if (quantityPerUnit <= 0)
            return ServiceResult.Fail("La cantidad por unidad debe ser mayor que cero.");

        var material = await _materialRepository.GetByNameAsync(matName, cancellationToken);
        if (material is null)
        {
            material = new Material
            {
                Code = $"mat{DateTime.UtcNow.Ticks}",
                Name = matName,
                Unit = unit,
                Stock = 0,
                MinStock = 10,
                Status = MaterialStatus.Bueno,
                LastEntryDate = DateOnly.FromDateTime(DateTime.Today)
            };
            await _materialRepository.AddAsync(material, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var existing = await _bomRepository.GetByProductAsync(name, cancellationToken);
        if (existing.Any(b => b.MaterialId == material.Id))
            return ServiceResult.Fail($"El material «{material.Name}» ya está en la ficha de «{name}».");

        var canonicalName = existing.FirstOrDefault()?.ProductName ?? name;

        await _bomRepository.AddAsync(new BomItem
        {
            ProductName = canonicalName,
            MaterialId = material.Id,
            QuantityPerUnit = quantityPerUnit,
            Unit = material.Unit
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Material «{material.Name}» agregado a la ficha de «{canonicalName}».");
    }
}
