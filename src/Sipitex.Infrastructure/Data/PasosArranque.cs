namespace Sipitex.Infrastructure.Data;

// Un fallo de migración o semilla aborta el proceso. El nombre del paso queda en el log.
public static class PasosArranque
{
    public const string LineaBaseMigraciones = "LineaBaseMigraciones";
    public const string CompatibilidadFichaTurno = "CompatibilidadFichaTurno";
    public const string Migraciones = "Migraciones";
    public const string Semilla = "Semilla";
    public const string CadenaConexion = "CadenaConexion";
    public const string Inicializacion = "Inicializacion";

    public static async Task Ejecutar(string paso, Func<Task> accion)
    {
        try
        {
            await accion();
        }
        catch (ArranqueException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArranqueException(paso, ex);
        }
    }
}

public sealed class ArranqueException : Exception
{
    public string Paso { get; }

    public ArranqueException(string paso, Exception inner)
        : base($"El arranque falló en el paso {paso}.", inner)
    {
        Paso = paso;
    }
}
