# event-awaiter-kit


A small, zero-dependency .NET Standard 2.0 library for “awaiting” any event or delegate callback pattern, with built-in support for timeouts and cancellation.

<!-- Table of Contents -->
- [Introduction](#introduction)
- [Examples](#examples)
- [API Reference](#api-reference)
- [Roadmap](#roadmap)

## Introduction

EventAwaiterKit simplifies asynchronous workflows by letting you `await` any .NET event or callback pattern as a `Task`. It provides:

- **Timeout Support**: returns `false` if the event doesn’t fire within a specified `TimeSpan`.
- **Cancellation**: propagates `OperationCanceledException` if the provided `CancellationToken` is canceled.
- **Memory-safe**: automatically unsubscribes handlers to avoid leaks.
- **.NET Standard 2.0**: works across .NET Framework, .NET Core, Xamarin, Unity, and more.

Whether you’re building UI dialogs, hardware integrations, or game logic, EventAwaiterKit streamlines waiting for events without boilerplate.


## Examples

### 1. UI Examples (WinForms/WPF)

```csharp
async Task PromptUserAsync(Button okButton, CancellationToken ct = default)
{
    // Enable the button or show your dialog here...
    okButton.Enabled = true;

    bool clicked = await EventAwaiter.WaitForEventAsync(
        h => okButton.Click += h,
        h => okButton.Click -= h,
        TimeSpan.FromSeconds(10),
        ct);

    if (clicked)
        MessageBox.Show("Thanks for clicking!");
    else
        MessageBox.Show("Timed out—closing.");
}
```

```csharp
async Task FadeOutAndCloseAsync(Window window, TimeSpan duration, CancellationToken ct = default)
{
    // Build a simple fade‐out animation
    var storyboard = new Storyboard { Duration = new Duration(duration) };
    var animation = new DoubleAnimation(1.0, 0.0, duration);
    Storyboard.SetTarget(animation, window);
    Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
    storyboard.Children.Add(animation);

    // Start the storyboard
    storyboard.Begin();

    // Await its Completed event or timeout
    bool finished = await EventAwaiter.WaitForEventAsync(
        h => storyboard.Completed += h,
        h => storyboard.Completed -= h,
        duration,
        ct);

    if (finished)
        window.Close();
}
```
### 2. Hardware Examples

```csharp
// Assume MySensor.ReadComplete is an event Action
async Task ReadSensorAsync(MySensor sensor, CancellationToken ct = default)
{
    sensor.StartMeasurement();

    bool gotValue = await EventAwaiter.WaitForEventAsync(
        h => sensor.ReadComplete += h,
        h => sensor.ReadComplete -= h,
        TimeSpan.FromSeconds(2),
        ct);

    if (gotValue)
        Console.WriteLine($"Sensor value: {sensor.LastValue}");
    else
        Console.WriteLine("Sensor read timed out.");
}
```

```csharp
async Task MoveMotorWithLimitSwitchAsync(IMotorController motorController, ILimitSwitch limitSwitch, int steps, TimeSpan timeout, CancellationToken ct = default)
{
    // Start the motor movement
    motorController.StartMove(steps);

    // Prepare two awaitable tasks:
    // 1) motor completes its move
    var moveTask = EventAwaiter.WaitForEventAsync(
        h => motorController.MoveCompleted += h,
        h => motorController.MoveCompleted -= h,
        timeout,
        ct);

    // 2) limit switch is triggered
    var limitTask = EventAwaiter.WaitForEventAsync(
        h => limitSwitch.Triggered += h,
        h => limitSwitch.Triggered -= h,
        timeout,
        ct);

    // Wait for whichever happens first
    var finished = await Task.WhenAny(moveTask, limitTask);

    if (finished == moveTask && moveTask.Result)
    {
        Console.WriteLine($"Motor reached target position: {motorController.CurrentPosition}");
    }
    else if (finished == limitTask && limitTask.Result)
    {
        Console.WriteLine("Limit switch triggered. Stopping motor.");
        motorController.Stop();
    }
    else
    {
        Console.WriteLine("Operation timed out. Stopping motor.");
        motorController.Stop();
    }
}
```
### 3. Unity Example (Button Click)

```csharp
using UnityEngine;
using UnityEngine.UI;

public class ClickAwaiter : MonoBehaviour
{
    [SerializeField] private Button uiButton;

    private async void Start()
    {
        bool clicked = await EventAwaiter.WaitForEventAsync(
            handler => uiButton.onClick.AddListener(handler),
            handler => uiButton.onClick.RemoveListener(handler),
            TimeSpan.FromSeconds(5));

        if (clicked)
            Debug.Log("Unity UI Button clicked!");
        else
            Debug.Log("No click within 5 seconds.");
    }
}
```


### 4. File Watcher Example
```csharp
async Task WatchFileChangesAsync(string filePath, CancellationToken ct = default)
{
    using var watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath))
    {
        Filter = Path.GetFileName(filePath),
        EnableRaisingEvents = true
    };

    // Wait up to 30 seconds for the file to change
    bool changed = await EventAwaiter.EventAwaiter.WaitForEventAsync<FileSystemEventArgs>(
        handler => watcher.Changed += handler,
        handler => watcher.Changed -= handler,
        TimeSpan.FromSeconds(30),
        ct);

    if (changed)
        Console.WriteLine($"{filePath} was modified!");
    else
        Console.WriteLine("No changes detected within 30 seconds.");
}
```


## API Reference

### `EventAwaiter`

```csharp
// Await an Action-style callback (with timeout)
Task<bool> WaitForEventAsync(
    Action subscribe,
    Action unsubscribe,
    TimeSpan timeout,
    CancellationToken cancellationToken = default
);

// Await an Action-style callback (no timeout)
Task<bool> WaitForEventAsync(
    Action subscribe,
    Action unsubscribe,
    CancellationToken cancellationToken = default
);

// Await an EventHandler-style event (with timeout)
Task<bool> WaitForEventAsync(
    Action<EventHandler> subscribe,
    Action<EventHandler> unsubscribe,
    TimeSpan timeout,
    CancellationToken cancellationToken = default
);

// Await an EventHandler-style event (no timeout)
Task<bool> WaitForEventAsync(
    Action<EventHandler> subscribe,
    Action<EventHandler> unsubscribe,
    CancellationToken cancellationToken = default
);
```

- Returns true if the event fires (or callback runs) before timeout; otherwise false.
- On cancellation, throws an OperationCanceledException.
- Auto-unsubscribes handlers to avoid memory leaks.



## Roadmap
- [ ] Add unit tests
- [ ] Add XML doc comments to public API
- [ ] Add “Contributing” section to the README
- [ ] Add “License” section to the README
- [ ] Publish EventAwaiterKit to NuGet
- [ ] Support `Action<TArg>` overloads (await callbacks that pass a value)
- [ ] Support `EventHandler<TEventArgs>` overloads (await events carrying event data)
