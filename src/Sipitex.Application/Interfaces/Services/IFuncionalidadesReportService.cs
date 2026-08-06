using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Genera el documento Word con el listado de funcionalidades del sistema
public interface IFuncionalidadesReportService
{
    // Catálogo tipado (módulo, funcionalidad, descripción, rol)
    IReadOnlyList<FuncionalidadCatalogItem> GetCatalog();

    // Arma un .docx profesional (portada + tablas por módulo)
    ReportFileDto GenerateDocx(DateTime? generatedAt = null);
}
