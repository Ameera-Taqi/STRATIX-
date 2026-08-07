using Stratix.Application.Common;

namespace Stratix.Api.Tests;

public class PageQueryTests
{
    [Fact]
    public void Normalize_clamps_huge_pageSize_to_max()
    {
        var (_, size) = PageQuery.Normalize(1, 1_000_000);
        Assert.Equal(PageQuery.MaxPageSize, size);
        Assert.Equal(100, size);
    }

    [Fact]
    public void Normalize_uses_default_when_pageSize_missing()
    {
        var (page, size) = PageQuery.Normalize(null, null);
        Assert.Equal(1, page);
        Assert.Equal(PageQuery.DefaultPageSize, size);
        Assert.Equal(20, size);
    }

    [Fact]
    public void Normalize_rejects_zero_or_negative_pageSize_with_default()
    {
        var (_, size) = PageQuery.Normalize(1, 0);
        Assert.Equal(PageQuery.DefaultPageSize, size);

        (_, size) = PageQuery.Normalize(1, -5);
        Assert.Equal(PageQuery.DefaultPageSize, size);
    }

    [Fact]
    public void Normalize_keeps_valid_pageSize_within_bounds()
    {
        var (_, size) = PageQuery.Normalize(2, 50);
        Assert.Equal(50, size);
    }
}
