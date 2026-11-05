using Sipitex.Application.DTOs;

namespace Sipitex.Web.Models;

public class InventarioImportarViewModel
{
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public InventarioImportResult? Resultado { get; set; }
}
