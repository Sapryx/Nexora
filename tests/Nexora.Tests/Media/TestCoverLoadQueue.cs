using Microsoft.Extensions.Time.Testing;
using Moq;
using Nexora.Media;
using Nexora.Threading;

namespace Nexora.Tests.Media;

public static class TestCoverLoadQueue
{
    public static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(150);

    public static CoverLoadQueue Create()
    {
        return Create(Mock.Of<ICoverCache>(), new FakeTimeProvider());
    }

    public static CoverLoadQueue Create(ICoverCache coverCache, TimeProvider timeProvider)
    {
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        return new CoverLoadQueue(coverCache, timeProvider, uiDispatcherMock.Object, ShowDelay, 4);
    }
}
