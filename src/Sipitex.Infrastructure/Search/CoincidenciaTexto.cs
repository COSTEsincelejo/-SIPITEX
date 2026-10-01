using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Sipitex.Infrastructure.Search;

// Dobla acentos y mayúsculas en la expresión SQL y compara con LIKE (SQLite) o ILIKE (PostgreSQL).
internal static class CoincidenciaTexto
{
    private static readonly (string Desde, string Hasta)[] Reemplazos =
    [
        ("á", "a"), ("à", "a"), ("ä", "a"), ("â", "a"),
        ("é", "e"), ("è", "e"), ("ë", "e"), ("ê", "e"),
        ("í", "i"), ("ì", "i"), ("ï", "i"), ("î", "i"),
        ("ó", "o"), ("ò", "o"), ("ö", "o"), ("ô", "o"),
        ("ú", "u"), ("ù", "u"), ("ü", "u"), ("û", "u"),
        ("ñ", "n"),
        ("Á", "a"), ("À", "a"), ("Ä", "a"), ("Â", "a"),
        ("É", "e"), ("È", "e"), ("Ë", "e"), ("Ê", "e"),
        ("Í", "i"), ("Ì", "i"), ("Ï", "i"), ("Î", "i"),
        ("Ó", "o"), ("Ò", "o"), ("Ö", "o"), ("Ô", "o"),
        ("Ú", "u"), ("Ù", "u"), ("Ü", "u"), ("Û", "u"),
        ("Ñ", "n")
    ];

    public static IQueryable<T> DondeContiene<T>(
        IQueryable<T> consulta,
        bool ilike,
        string needleNormalizado,
        params Expression<Func<T, string?>>[] campos)
    {
        if (campos.Length == 0 || string.IsNullOrEmpty(needleNormalizado))
            return consulta.Where(_ => false);

        var parametro = campos[0].Parameters[0];
        Expression? or = null;
        var patron = "%" + Escapar(needleNormalizado) + "%";
        foreach (var campo in campos)
        {
            var cuerpo = new ReemplazoParametro(campo.Parameters[0], parametro).Visit(campo.Body)!;
            var predicado = Comparar(cuerpo, patron, ilike);
            or = or is null ? predicado : Expression.OrElse(or, predicado);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(or!, parametro);
        return consulta.Where(lambda);
    }

    private static Expression Comparar(Expression valor, string patron, bool ilike)
    {
        var doblado = Doblar(valor);
        var funciones = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);
        var tipo = ilike
            ? typeof(NpgsqlDbFunctionsExtensions)
            : typeof(DbFunctionsExtensions);
        var metodo = tipo.GetMethods().Single(m =>
            m.Name == (ilike ? "ILike" : "Like") && m.GetParameters().Length == 4);
        return Expression.Call(
            metodo,
            funciones,
            doblado,
            Expression.Constant(patron),
            Expression.Constant("\\"));
    }

    private static Expression Doblar(Expression valor)
    {
        var coalescido = Expression.Coalesce(valor, Expression.Constant(""));
        Expression actual = coalescido;
        foreach (var (desde, hasta) in Reemplazos)
        {
            actual = Expression.Call(
                actual,
                nameof(string.Replace),
                Type.EmptyTypes,
                Expression.Constant(desde),
                Expression.Constant(hasta));
        }

        return Expression.Call(actual, nameof(string.ToLower), Type.EmptyTypes);
    }

    private static string Escapar(string texto) =>
        texto.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed class ReemplazoParametro(ParameterExpression origen, ParameterExpression destino) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == origen ? destino : node;
    }
}
