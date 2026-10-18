using Godot;

[Tool, GlobalClass]
public partial class OctNode : Resource
{
	[Export] public Vector3I  Position;
	[Export] public Vector3I  Size;
	[Export] public byte[]    DataBits = null; // 8 blocks per byte

	[Export] public int lod;
	[Export] public int total_blocks;
	[Export] public int face_count;

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
