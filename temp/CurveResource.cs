using Godot;

[Tool, GlobalClass]
public partial class CurveResource : Resource
{
    [Export] public Curve _curve_x = new();
    [Export] public Curve _curve_y = new();
    [Export] public Curve _curve_z = new();

    [Export] public Vector2 _range = new();

    [Export] public Noise _noise;
}