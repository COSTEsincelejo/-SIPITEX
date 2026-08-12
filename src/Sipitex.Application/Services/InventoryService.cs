using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Inventario de materiales y solicitudes de bodega
public class InventoryService : IInventoryService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IMaterialRequestRepository _requestRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IBomRepository _bomRepository;
    private readonly IStockMovementRepository _stockMovements;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IMaterialRepository materialRepository,
        IMaterialRequestRepository requestRepository,
        IProductionOrderRepository orderRepository,
        IBomRepository bomRepository,
        IStockMovementRepository stockMovements,
        IUnitOfWork unitOfWork)
    {
        _materialRepository = materialRepository;
        _requestRepository = requestRepository;
        _orderRepository = orderRepository;
        _bomRepository = bomRepository;
        _stockMovements = stockMovements;
        _unitOfWork = unitOfWork;
    }

    // Traigo todos los materiales ya mapeados a DTO para la vista
    public async Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(CancellationToken cancellationToken = default)
    {
        // Query a la tabla Materials
        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        // Paso cada entidad al DTO con unidad legible y flag de stock bajo
        return materials.Select(MapMaterial).ToList();
    }

    // Crea un material nuevo con código autogenerado
    public async Task<ServiceResult> AddMaterialAsync(
        CreateMaterialDto dto,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        // Acá reviso nombre y que el stock no sea negativo
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Stock < 0)
            return ServiceResult.Fail("Ingrese nombre y stock válidos.");

        if (!Enum.IsDefined(dto.Origen))
            return ServiceResult.Fail("Seleccione el origen de la entrada (compra, devolución u otra fuente autorizada).");

        if (actorUserId <= 0)
            return ServiceResult.Fail("Usuario responsable no válido.");

        // Armo la entidad con valores iniciales
        var material = new Material
        {
            // Código único con ticks para no repetir
            Code = $"mat{DateTime.UtcNow.Ticks}",
            Name = dto.Name.Trim(),
            Unit = dto.Unit,
            Stock = dto.Stock,
            MinStock = 10, // por ahora fijo, después podría ser configurable
            Status = MaterialStatus.Bueno,
            LastEntryDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // INSERT en el contexto de EF
        await _materialRepository.AddAsync(material, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // Necesito material.Id para el ledger

        await _stockMovements.AddAsync(new StockMovement
        {
            MaterialId = material.Id,
            FechaUtc = DateTime.UtcNow,
            UsuarioId = actorUserId,
            TipoMovimiento = StockMovementType.Entrada,
            Origen = dto.Origen,
            Cantidad = material.Stock,
            StockResultante = material.Stock,
            Referencia = $"Material:{material.Id}"
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Material agregado.");
    }

    // Ajuste manual de stock (no deja negativo). Origen obligatorio si el stock sube.
    public async Task<ServiceResult> AdjustStockAsync(
        AdjustStockDto dto,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0)
            return ServiceResult.Fail("Usuario responsable no válido.");

        // Busco el material por id del form
        var material = await _materialRepository.GetByIdAsync(dto.MaterialId, cancellationToken);
        if (material is null) return ServiceResult.Fail("Material no encontrado.");

        var previous = material.Stock;
        var newStock = Math.Max(0, dto.NewStock);
        var isIncrease = newStock > previous;

        if (isIncrease && (dto.Origen is null || !Enum.IsDefined(dto.Origen.Value)))
            return ServiceResult.Fail("Indique el origen de la entrada (compra, devolución u otra fuente autorizada).");

        // Math.Max evita stock negativo
        material.Stock = newStock;
        // Actualizo fecha de última entrada
        material.LastEntryDate = DateOnly.FromDateTime(DateTime.Today);
        // Marco la entidad como modificada
        _materialRepository.Update(material);

        var delta = material.Stock - previous;
        await _stockMovements.AddAsync(new StockMovement
        {
            MaterialId = material.Id,
            FechaUtc = DateTime.UtcNow,
            UsuarioId = actorUserId,
            TipoMovimiento = StockMovementType.Ajuste,
            Origen = isIncrease ? dto.Origen : null,
            Cantidad = Math.Abs(delta),
            StockResultante = material.Stock,
            Referencia = $"Ajuste:{material.Id}"
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Stock actualizado.");
    }

    // Edición completa de metadatos (nombre, unidad, mínimo). No modifica stock.
    public async Task<ServiceResult> UpdateMaterialAsync(
        UpdateMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return ServiceResult.Fail("Ingrese un nombre válido.");

        if (dto.MinStock < 0)
            return ServiceResult.Fail("El stock mínimo no puede ser negativo.");

        if (!Enum.IsDefined(dto.Unit))
            return ServiceResult.Fail("Unidad no válida.");

        var material = await _materialRepository.GetByIdAsync(dto.MaterialId, cancellationToken);
        if (material is null) return ServiceResult.Fail("Material no encontrado.");

        material.Name = dto.Name.Trim();
        material.Unit = dto.Unit;
        material.MinStock = dto.MinStock;
        _materialRepository.Update(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Material «{material.Name}» actualizado.");
    }

    // Cambia el estado físico: Bueno / Regular / Deteriorado
    public async Task<ServiceResult> UpdateStatusAsync(UpdateMaterialStatusDto dto, CancellationToken cancellationToken = default)
    {
        var material = await _materialRepository.GetByIdAsync(dto.MaterialId, cancellationToken);
        if (material is null) return ServiceResult.Fail("Material no encontrado.");

        // Asigno el nuevo estado del enum
        material.Status = dto.Status;
        _materialRepository.Update(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Estado actualizado a {dto.Status}.");
    }

    // Solicitudes de material (pendientes, aprobadas, rechazadas)
    public async Task<IReadOnlyList<MaterialRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default)
    {
        // Traigo solicitudes con Material y Orden incluidos
        var requests = await _requestRepository.GetAllAsync(cancellationToken);
        // Proyecto a DTO plano para la vista
        return requests.Select(r => new MaterialRequestDto(
            r.Id,
            r.Material.Name,
            r.Quantity,
            r.ProductionOrder.OrderNumber,
            r.Status)).ToList();
    }

    // Instructor pide material para una orden
    public async Task<ServiceResult> CreateRequestAsync(CreateMaterialRequestDto dto, CancellationToken cancellationToken = default)
    {
        // Verifico que el material exista
        var material = await _materialRepository.GetByIdAsync(dto.MaterialId, cancellationToken);
        // Y que la orden de producción también
        var order = await _orderRepository.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        // Los tres datos tienen que ser válidos
        if (material is null || order is null || dto.Quantity <= 0)
            return ServiceResult.Fail("Datos inválidos.");

        // Nueva fila en MaterialRequests
        await _requestRepository.AddAsync(new MaterialRequest
        {
            MaterialId = dto.MaterialId,
            ProductionOrderId = dto.ProductionOrderId,
            Quantity = dto.Quantity,
            Status = RequestStatus.Pendiente
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Solicitud creada.");
    }

    // Bodega aprueba: descuenta stock y marca la solicitud
    public async Task<ServiceResult> ApproveRequestAsync(
        int requestId,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0)
            return ServiceResult.Fail("Usuario responsable no válido.");

        // Cargo solicitud con navegación a Material
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        // Solo se aprueban solicitudes pendientes
        if (request is null || request.Status != RequestStatus.Pendiente)
            return ServiceResult.Fail("Solicitud no válida.");

        // Acá reviso si alcanza el stock en bodega
        if (request.Material.Stock < request.Quantity)
            return ServiceResult.Fail("Stock insuficiente para aprobar solicitud.");

        // Esto descuenta del inventario
        request.Material.Stock -= request.Quantity;
        request.Status = RequestStatus.Aprobada;
        _materialRepository.Update(request.Material);
        _requestRepository.Update(request);

        await _stockMovements.AddAsync(new StockMovement
        {
            MaterialId = request.MaterialId,
            FechaUtc = DateTime.UtcNow,
            UsuarioId = actorUserId,
            TipoMovimiento = StockMovementType.AprobacionSolicitud,
            Cantidad = request.Quantity,
            StockResultante = request.Material.Stock,
            Referencia = $"MaterialRequest:{request.Id}"
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Solicitud aprobada.");
    }

    // Rechazo: no toco el stock, solo cambio el estado
    public async Task<ServiceResult> RejectRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null || request.Status != RequestStatus.Pendiente)
            return ServiceResult.Fail("Solicitud no válida.");

        // Solo cambio estado, el stock queda igual
        request.Status = RequestStatus.Rechazada;
        _requestRepository.Update(request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Solicitud rechazada.");
    }

    // Elimina del catálogo solo si no está en ninguna ficha técnica activa
    public async Task<ServiceResult> DeleteMaterialAsync(int materialId, CancellationToken cancellationToken = default)
    {
        var material = await _materialRepository.GetByIdAsync(materialId, cancellationToken);
        if (material is null) return ServiceResult.Fail("Material no encontrado.");

        var products = await _bomRepository.GetProductNamesUsingMaterialAsync(materialId, cancellationToken);
        if (products.Count > 0)
        {
            return ServiceResult.Fail(
                $"No se puede eliminar «{material.Name}»: está en fichas técnicas de {string.Join(", ", products)}. Quítelo de esas recetas antes.");
        }

        _materialRepository.Remove(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Material «{material.Name}» eliminado.");
    }

    // Armo el DTO e indico si está bajo el mínimo (para pintar en rojo en la vista)
    private static MaterialDto MapMaterial(Material m) => new(
        m.Id,
        m.Name,
        UnitHelper.ToDisplay(m.Unit),
        m.Unit,
        m.Stock,
        m.Status,
        m.MinStock,
        m.Stock < m.MinStock,
        m.LastEntryDate);
}
