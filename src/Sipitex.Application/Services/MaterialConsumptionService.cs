using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class MaterialConsumptionService : IMaterialConsumptionService
{
    private readonly IConsumoMaterialRepository _consumos;
    private readonly IProductionOrderRepository _orders;
    private readonly IMaterialRepository _materials;
    private readonly IUserRepository _users;
    private readonly IStockMovementRepository _stockMovements;
    private readonly IMaterialConsumptionCostService _costService;
    private readonly IUnitOfWork _unitOfWork;

    public MaterialConsumptionService(
        IConsumoMaterialRepository consumos,
        IProductionOrderRepository orders,
        IMaterialRepository materials,
        IUserRepository users,
        IStockMovementRepository stockMovements,
        IMaterialConsumptionCostService costService,
        IUnitOfWork unitOfWork)
    {
        _consumos = consumos;
        _orders = orders;
        _materials = materials;
        _users = users;
        _stockMovements = stockMovements;
        _costService = costService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ConsumoMaterialDto>> GetByOrderAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _consumos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<ServiceResult> RegisterAsync(
        RegisterConsumoMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Cantidad <= 0)
            return ServiceResult.Fail("La cantidad utilizada debe ser mayor que cero.");
        if (dto.ResponsableUserId <= 0)
            return ServiceResult.Fail("Indique el responsable del consumo.");

        var order = await _orders.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden de producción no encontrada.");

        var material = await _materials.GetByIdAsync(dto.MaterialId, cancellationToken);
        if (material is null)
            return ServiceResult.Fail("Material no encontrado.");

        var responsable = await _users.GetByIdAsync(dto.ResponsableUserId, cancellationToken);
        if (responsable is null)
            return ServiceResult.Fail("Responsable no encontrado.");

        if (material.Stock < dto.Cantidad)
            return ServiceResult.Fail(
                $"Stock insuficiente de «{material.Name}»: hay {material.Stock}, se requieren {dto.Cantidad}.");

        var costo = material.CostoAdquisicion;
        var fecha = dto.FechaUtc ?? DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            material.Stock -= dto.Cantidad;
            _materials.Update(material);

            var consumo = new ConsumoMaterial
            {
                ProductionOrderId = order.Id,
                GrupoConfeccionId = dto.GrupoConfeccionId,
                MaterialId = material.Id,
                Cantidad = dto.Cantidad,
                FechaUtc = fecha,
                ResponsableUserId = responsable.Id,
                CostoUnitario = costo
            };
            await _consumos.AddAsync(consumo, ct);

            await _stockMovements.AddAsync(new StockMovement
            {
                MaterialId = material.Id,
                FechaUtc = fecha,
                UsuarioId = responsable.Id,
                TipoMovimiento = StockMovementType.Salida,
                Cantidad = dto.Cantidad,
                StockResultante = material.Stock,
                Referencia = $"ConsumoMaterial:{order.OrderNumber}",
                CostoUnitario = costo
            }, ct);
        }, cancellationToken);

        return ServiceResult.Ok(
            $"Consumo de {dto.Cantidad} {UnitHelper.ToDisplay(material.Unit)} de «{material.Name}» registrado en {order.OrderNumber}.");
    }

    public async Task<ConsumoCostoResumenDto> GetCostoPromedioByOrderAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _consumos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        var lineas = rows.Select(r => new ConsumoCostoLineaDto(r.Cantidad, r.CostoUnitario)).ToList();
        var promedio = _costService.CalcularPromedioPonderado(lineas);
        var totalCantidad = lineas.Sum(l => l.Cantidad);
        var totalCosto = lineas.Sum(l => l.Cantidad * l.CostoUnitario);
        return new ConsumoCostoResumenDto(productionOrderId, totalCantidad, promedio, totalCosto);
    }

    private static ConsumoMaterialDto Map(ConsumoMaterial c) => new(
        c.Id,
        c.ProductionOrderId,
        c.ProductionOrder?.OrderNumber ?? string.Empty,
        c.GrupoConfeccionId,
        c.MaterialId,
        c.Material?.Code ?? string.Empty,
        c.Material?.Name ?? string.Empty,
        c.Cantidad,
        c.Material is null ? "—" : UnitHelper.ToDisplay(c.Material.Unit),
        c.FechaUtc,
        c.ResponsableUserId,
        c.Responsable?.Nombre ?? $"#{c.ResponsableUserId}",
        c.CostoUnitario,
        c.Cantidad * c.CostoUnitario);
}
