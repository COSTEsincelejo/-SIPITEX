using Microsoft.EntityFrameworkCore; // Include, OrderByDescending...
using Sipitex.Application.Interfaces.Repositories; // IMaterialRequestRepository
using Sipitex.Domain.Entities; // MaterialRequest
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Solicitudes de salida de materiales (instructor pide, bodeguero aprueba)
public class MaterialRequestRepository : IMaterialRequestRepository
{
    private readonly SipitexDbContext _context;

    public MaterialRequestRepository(SipitexDbContext context) => _context = context;

    // Incluyo material y orden para mostrar todo en la lista sin N+1
    public async Task<IReadOnlyList<MaterialRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.MaterialRequests
            .Include(r => r.Material) // Nombre del material
            .Include(r => r.ProductionOrder) // OP-xxx asociada
            .OrderByDescending(r => r.CreatedAt) // Las más recientes arriba
            .ToListAsync(cancellationToken);

    // Nueva solicitud de material
    public async Task AddAsync(MaterialRequest request, CancellationToken cancellationToken = default) =>
        await _context.MaterialRequests.AddAsync(request, cancellationToken);

    // Busca una solicitud por Id (para aprobar/rechazar)
    public Task<MaterialRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.MaterialRequests
            .Include(r => r.Material)
            .Include(r => r.ProductionOrder)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    // Actualiza estado de la solicitud (aprobada, rechazada, entregada...)
    public void Update(MaterialRequest request) => _context.MaterialRequests.Update(request);
}
