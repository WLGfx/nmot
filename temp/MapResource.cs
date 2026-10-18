using Godot;

[Tool, GlobalClass]
public partial class MapResource : Resource
{
    [Export] public string MapName;
    [Export] public Vector3I Size;

    [Export] public int lod;

	[Export] public Gradient gradient = new();
	[Export] public Material material;
	
    [Export] public CurveResource AddBlocks = new();
	[Export] public CurveResource SubBlocks = new();

    [ExportGroup("Cache data")]
	[Export] public OctNode[] nodes       = null;
	[Export] public int 	  nodes_count = 0;
}