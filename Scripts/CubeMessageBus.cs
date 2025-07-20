using Godot;

// TODO: make it global?
[GlobalClass]
public partial class CubeMessageBus : Resource
{
    [Signal]
    public delegate void MessageReceivedEventHandler(string message);
}
