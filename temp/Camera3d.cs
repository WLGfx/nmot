using Godot;
using System;

[Tool, GlobalClass]
public partial class MapViewer : Camera3D
{
	[Export] public NoiseMapEditor editor;
	Vector3 last_position = new(0, 0, 0);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// has camera moved since last frame?
		if (last_position != Position)
		{
			last_position = Position;
			
		}
	}
}
