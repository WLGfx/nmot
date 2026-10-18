using Godot;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class MapResource : Resource
{
    [Export] public string MapName;
    [Export] public Vector3I Size;

    [Export] public int lod;

	[Export] public Gradient gradient;
	[Export] public Material material;

	[Export] public OctNode[] nodes       = null;
	[Export] public int 	  nodes_count = 0;

	[Export] public CurveResource AddBlocks;
	[Export] public CurveResource SubBlocks;
}