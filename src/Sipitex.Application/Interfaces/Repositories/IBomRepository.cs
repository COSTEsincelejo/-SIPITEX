using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Bill of Materials: cabeceras de producto + líneas de receta
public interface IBomRepository
{
    Task<IReadOnlyList<BomItem>> GetByProductAsync(string productName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomProduct>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<BomProduct?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BomProduct?> GetProductByNameAsync(string productName, CancellationToken cancellationToken = default);
    Task AddProductAsync(BomProduct product, CancellationToken cancellationToken = default);
    void UpdateProduct(BomProduct product);
    void RemoveProduct(BomProduct product);
    Task AddItemAsync(BomItem item, CancellationToken cancellationToken = default);
    void UpdateItem(BomItem item);
    void RemoveItem(BomItem item);
    void RemoveTalla(BomProductTalla talla);
    void RemovePieza(BomProductPieza pieza);
    void RemoveMedida(BomProductMedida medida);
    Task<IReadOnlyList<string>> GetProductNamesUsingMaterialAsync(int materialId, CancellationToken cancellationToken = default);
}
