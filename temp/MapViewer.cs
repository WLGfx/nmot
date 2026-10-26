using Godot;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class MapViewer : Camera3D
{
	[Export] public NoiseMapEditor editor;
	[Export] public int step; // steps through each lowering of LOD
	[Export] public int count; // max LOD detail (ie distance)

	Vector3I _last_position = new(0, 0, 0);

	public List<ChunkNode> nodes;

	int _lod = 64; // define chunk size ^ 3

	public void InitView()
	{
		//
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;

		// has camera moved since last frame?
		if (_last_position != (Vector3I)Position)
		{
			_last_position = (Vector3I)Position;
			
			_build_tree();
		}
	}

	private void _build_tree()
	{
		
	}
}
