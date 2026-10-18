using Godot;

[GlobalClass]
public partial class OctNode : Resource
{
	//public OctNode[] Children { get; set; }
	[Export] public Vector3I  Position;
	[Export] public Vector3I  Size;
	[Export] public byte[]    DataBits = null; // 8 blocks per byte
	
	// TODO: split arrays into bits and faces and export bits for save cache

	[Export] public int lod;
	[Export] public int total_blocks;

	public Vector3[] _vertices;
	public Vector3[] _normals;
	public Vector2[] _uvs;
	public Color[]   _colors;

	// assumes position is local to the chunk data
	public bool GetBlock(int x, int y, int z)
	{
		if (DataBits == null) return false;
		
		int rowBytes = (lod + 7) / 8;
		int index = z * lod * rowBytes + y * rowBytes + x / 8;
		int bit = 128 >> (x & 7);

		return (DataBits[index] & bit) != 0;
	}

	public void SetBlock(int x, int y, int z, bool value)
	{
		int rowBytes = (lod + 7) / 8;
		int index = z * lod * rowBytes + y * rowBytes + x / 8;
		byte bit = (byte)(128 >> (x & 7));

		if (DataBits == null) 
		{
			if (value == true) 
			{
				DataBits = new byte[lod * lod * rowBytes];
				DataBits[index] = (byte)(DataBits[index] | bit);
			}
		}
		else
		{
			if (value == true)
			{
				DataBits[index] = (byte)(DataBits[index] | bit);
			}
			else
			{
				DataBits[index] = (byte)(DataBits[index] & ~bit);
			}
		}
	}
}
