using Godot;

[Tool, GlobalClass]
public partial class CurveResource : Resource
{
    [Export] public Curve _curve_x;
    [Export] public Curve _curve_y;
    [Export] public Curve _curve_z;

    [Export] public Vector2 _range;

    [Export] public Noise _noise;
}