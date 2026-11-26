using Moq;
using Nexora.Media;
using Nexora.Render;

namespace Nexora.Tests.Media;

public class CoverMemoryBudgetTests
{
    private readonly Mock<ICoverCache> coverCacheMock = new Mock<ICoverCache>();
    private readonly Mock<IRenderScaleProvider> renderScaleMock = new Mock<IRenderScaleProvider>();
    private readonly CoverMemoryBudget budget;

    public CoverMemoryBudgetTests()
    {
        coverCacheMock.SetupProperty(it => it.MemoryBudget);
        renderScaleMock.Setup(it => it.RenderScaling).Returns(1);
        budget = new CoverMemoryBudget(coverCacheMock.Object, renderScaleMock.Object);
    }

    [Fact]
    public void Show_OneViewport_SetsBudgetToItsCoversTimesScreens()
    {
        budget.AddViewport().Show(10, 160);

        Assert.Equal(CoverMemoryBudget.Screens * 10L * 160 * 160 * 4, coverCacheMock.Object.MemoryBudget);
    }

    [Fact]
    public void Show_SeveralViewports_SumsTheirCovers()
    {
        budget.AddViewport().Show(10, 160);
        budget.AddViewport().Show(8, 64);

        long expected = CoverMemoryBudget.Screens * (10L * 160 * 160 + 8L * 64 * 64) * 4;
        Assert.Equal(expected, coverCacheMock.Object.MemoryBudget);
    }

    [Fact]
    public void Show_HiddenViewport_NoLongerCounts()
    {
        var grid = budget.AddViewport();
        budget.AddViewport().Show(8, 64);
        grid.Show(10, 160);

        grid.Show(0, 160);

        Assert.Equal(CoverMemoryBudget.Screens * 8L * 64 * 64 * 4, coverCacheMock.Object.MemoryBudget);
    }

    [Fact]
    public void Show_ScaledDisplay_CountsScaledPixels()
    {
        renderScaleMock.Setup(it => it.RenderScaling).Returns(1.5);

        budget.AddViewport().Show(1, 64);

        Assert.Equal(CoverMemoryBudget.Screens * 96L * 96 * 4, coverCacheMock.Object.MemoryBudget);
    }

    [Fact]
    public void Show_SameCoversAgain_DoesNotUpdateBudget()
    {
        var viewport = budget.AddViewport();
        viewport.Show(10, 160);

        viewport.Show(10, 160);

        coverCacheMock.VerifySet(it => it.MemoryBudget = It.IsAny<long>(), Times.Once);
    }

    [Fact]
    public void Show_SameCountOtherSize_UpdatesBudget()
    {
        var viewport = budget.AddViewport();
        viewport.Show(10, 160);

        viewport.Show(10, 64);

        Assert.Equal(64, viewport.CoverSize);
        Assert.Equal(CoverMemoryBudget.Screens * 10L * 64 * 64 * 4, coverCacheMock.Object.MemoryBudget);
    }
}
