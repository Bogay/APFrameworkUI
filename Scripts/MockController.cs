using Godot;
using System;

[GlobalClass]
public partial class MockController : Node
{
    [Export]
    public CubeCredits Credits { get; set; }
    [Export]
    public CubeMessageBus MessageBus { get; set; }

    private Timer _timer;

    public override void _Ready()
    {
        this._timer = new Timer();
        this._timer.WaitTime = 1.0f;
        this._timer.OneShot = false;
        this._timer.Timeout += this.OnTimerTimeout;
        AddChild(this._timer);
        this._timer.Start();
        this.MessageBus.MessageReceived += (message) =>
        {
            GD.Print($"Message received: {message}");
        };
    }

    private void OnTimerTimeout()
    {
        this.Credits.Value += 1;
        GD.Print($"Credits updated: {this.Credits.Value}");
    }
}
