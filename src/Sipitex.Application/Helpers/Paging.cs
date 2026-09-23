namespace Sipitex.Application.Helpers;

public static class Paging
{
    public const int DefaultPageSize = 25;

    public static int TotalPages(int totalCount, int pageSize)
    {
        var size = pageSize < 1 ? DefaultPageSize : pageSize;
        return totalCount <= 0 ? 1 : (int)Math.Ceiling(totalCount / (double)size);
    }

    public static int ClampPage(int? page, int totalCount, int pageSize)
    {
        var pages = TotalPages(totalCount, pageSize);
        var current = page.GetValueOrDefault(1);
        if (current < 1)
            return 1;
        return current > pages ? pages : current;
    }
}
