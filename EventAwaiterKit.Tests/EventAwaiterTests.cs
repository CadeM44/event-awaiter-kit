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
}
