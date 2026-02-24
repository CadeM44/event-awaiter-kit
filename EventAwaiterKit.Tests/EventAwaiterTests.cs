using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventAwaiterKit.Tests;

[TestClass]
public sealed class EventAwaiterTests
{
    private sealed class ActionEventSource
    {
        private Action? _handlers;

        public int AddCount { get; private set; }
        public int RemoveCount { get; private set; }

        public event Action Fired
        {
            add
            {
                AddCount++;
                _handlers += value;
            }
            remove
            {
                RemoveCount++;
                _handlers -= value;
            }
        }

        public bool HasSubscribers => _handlers != null;

        public void Raise() => _handlers?.Invoke();
    }

    private sealed class EventHandlerSource
    {
        private EventHandler? _handlers;

        public int AddCount { get; private set; }
        public int RemoveCount { get; private set; }

        public event EventHandler Fired
        {
            add
            {
                AddCount++;
                _handlers += value;
            }
            remove
            {
                RemoveCount++;
                _handlers -= value;
            }
        }

        public bool HasSubscribers => _handlers != null;

        public void Raise() => _handlers?.Invoke(this, EventArgs.Empty);
    }

    [TestMethod]
    public async Task WaitForEventAsync_Action_ReturnsTrueWhenEventFires()
    {
        var source = new ActionEventSource();

        var task = EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.FromSeconds(1));

        Assert.IsTrue(source.HasSubscribers);

        source.Raise();

        var result = await task;

        Assert.IsTrue(result);
        Assert.IsFalse(source.HasSubscribers);
        Assert.IsGreaterThanOrEqualTo(1, source.AddCount);
        Assert.IsGreaterThanOrEqualTo(1, source.RemoveCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_Action_ReturnsFalseOnTimeout()
    {
        var source = new ActionEventSource();

        var result = await EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.FromMilliseconds(150));

        Assert.IsFalse(result);
        Assert.IsFalse(source.HasSubscribers);
        Assert.IsGreaterThanOrEqualTo(1, source.AddCount);
        Assert.IsGreaterThanOrEqualTo(1, source.RemoveCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_Action_Cancels()
    {
        var source = new ActionEventSource();
        using var cts = new CancellationTokenSource();

        var task = EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.FromSeconds(1),
            cts.Token);

        cts.Cancel();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        Assert.IsFalse(source.HasSubscribers);
    }

    [TestMethod]
    public async Task WaitForEventAsync_Action_ThrowsOnNullAddHandler()
    {
        var removeCount = 0;

        await Assert.ThrowsAsync<ArgumentNullException>(() => EventAwaiter.WaitForEventAsync(
            (Action<Action>)null!,
            h => { removeCount++; },
            TimeSpan.FromSeconds(1)));

        Assert.AreEqual(0, removeCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_Action_ThrowsOnNullRemoveHandler()
    {
        var addCount = 0;

        await Assert.ThrowsAsync<ArgumentNullException>(() => EventAwaiter.WaitForEventAsync(
            h => { addCount++; },
            (Action<Action>)null!,
            TimeSpan.FromSeconds(1)));

        Assert.AreEqual(0, addCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_Action_ThrowsOnNegativeTimeout()
    {
        var addCount = 0;
        var removeCount = 0;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => EventAwaiter.WaitForEventAsync(
            (Action<Action>)(h => { addCount++; }),
            (Action<Action>)(h => { removeCount++; }),
            TimeSpan.FromMilliseconds(-2)));

        Assert.AreEqual(0, addCount);
        Assert.AreEqual(0, removeCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_Action_ZeroTimeoutReturnsFalse()
    {
        var source = new ActionEventSource();

        var result = await EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.Zero);

        Assert.IsFalse(result);
        Assert.IsFalse(source.HasSubscribers);
    }

    [TestMethod]
    public async Task WaitForEventAsync_Action_CanceledTokenThrowsImmediately()
    {
        var source = new ActionEventSource();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.FromSeconds(1),
            cts.Token));

        Assert.AreEqual(0, source.AddCount);
        Assert.AreEqual(0, source.RemoveCount);
        Assert.IsFalse(source.HasSubscribers);
    }

    [TestMethod]
    public async Task WaitForEventAsync_EventHandler_ReturnsTrueWhenEventFires()
    {
        var source = new EventHandlerSource();

        var task = EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.FromSeconds(1));

        Assert.IsTrue(source.HasSubscribers);

        source.Raise();

        var result = await task;

        Assert.IsTrue(result);
        Assert.IsFalse(source.HasSubscribers);
        Assert.IsGreaterThanOrEqualTo(1, source.AddCount);
        Assert.IsGreaterThanOrEqualTo(1, source.RemoveCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_EventHandler_ReturnsFalseOnTimeout()
    {
        var source = new EventHandlerSource();

        var result = await EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.FromMilliseconds(150));

        Assert.IsFalse(result);
        Assert.IsFalse(source.HasSubscribers);
        Assert.IsGreaterThanOrEqualTo(1, source.AddCount);
        Assert.IsGreaterThanOrEqualTo(1, source.RemoveCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_EventHandler_Cancels()
    {
        var source = new EventHandlerSource();
        using var cts = new CancellationTokenSource();

        var task = EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.FromSeconds(1),
            cts.Token);

        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => task); 
        Assert.IsFalse(source.HasSubscribers);
    }

    [TestMethod]
    public async Task WaitForEventAsync_EventHandler_ThrowsOnNullAddHandler()
    {
        var removeCount = 0;

        await Assert.ThrowsAsync<ArgumentNullException>(() => EventAwaiter.WaitForEventAsync(
            (Action<EventHandler>)null!,
            h => { removeCount++; },
            TimeSpan.FromSeconds(1)));

        Assert.AreEqual(0, removeCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_EventHandler_ThrowsOnNullRemoveHandler()
    {
        var addCount = 0;

        await Assert.ThrowsAsync<ArgumentNullException>(() => EventAwaiter.WaitForEventAsync(
            h => { addCount++; },
            (Action<EventHandler>)null!,
            TimeSpan.FromSeconds(1)));

        Assert.AreEqual(0, addCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_EventHandler_ThrowsOnNegativeTimeout()
    {
        var addCount = 0;
        var removeCount = 0;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => EventAwaiter.WaitForEventAsync(
            (Action<EventHandler>)(h => { addCount++; }),
            (Action<EventHandler>)(h => { removeCount++; }),
            TimeSpan.FromMilliseconds(-2)));

        Assert.AreEqual(0, addCount);
        Assert.AreEqual(0, removeCount);
    }

    [TestMethod]
    public async Task WaitForEventAsync_EventHandler_ZeroTimeoutReturnsFalse()
    {
        var source = new EventHandlerSource();

        var result = await EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.Zero);

        Assert.IsFalse(result);
        Assert.IsFalse(source.HasSubscribers);
    }

    [TestMethod]
    public async Task WaitForEventAsync_EventHandler_CanceledTokenThrowsImmediately()
    {
        var source = new EventHandlerSource();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => EventAwaiter.WaitForEventAsync(
            h => source.Fired += h,
            h => source.Fired -= h,
            TimeSpan.FromSeconds(1),
            cts.Token));

        Assert.AreEqual(0, source.AddCount);
        Assert.AreEqual(0, source.RemoveCount);
        Assert.IsFalse(source.HasSubscribers);
    }
}
