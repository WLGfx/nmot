using Godot;

[GlobalClass]
public partial class MeshResource : MeshInstance3D
{
    public void init(int face_count)
    {
        _vertices = new Vector3[face_count * 6]; // 12 bytes
        _normals  = new Vector3[face_count * 6]; // 12 bytes
        _uvs      = new Vector2[face_count * 6]; // 8 bytes
        _colors   = new Color[face_count * 6];   // 4 bytes
        
        for (int i = 0; i < face_count; i++)
        {
            _vertices[i] = new();
            _normals[i] = new();
            _uvs[i] = new();
            _colors[i] = new();
        }
    }
    public Vector3[] _vertices;
	public Vector3[] _normals;
	public Vector2[] _uvs;
	public Color[]   _colors;
}