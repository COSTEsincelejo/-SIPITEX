namespace Sipitex.Domain.Enums;

public enum ActaTipo
{
    Ingreso = 0,
    Egreso = 1
}

public enum ActaItemTipo
{
    Material = 0,
    ProductoEnProceso = 1,
    ProductoTerminado = 2
}

public enum ActaOrigen
{
    Manual = 0,
    Stock = 1,
    Consumo = 2,
    EstadoProducto = 3
}
