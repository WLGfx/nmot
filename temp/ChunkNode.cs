using Godot;

[GlobalClass]
public partial class ChunkNode : MeshInstance3D
{
    // TODO: create arraymesh and assign to MeshInstance3D
    public Godot.Collections.Array _arrays = [];
    public int                     _lod;

    const int __vertex = (int)Mesh.ArrayFormat.FormatVertex;
    const int __normal = (int)Mesh.ArrayFormat.FormatNormal;
    const int __uv     = (int)Mesh.ArrayFormat.FormatTexUV;
    const int __color  = (int)Mesh.ArrayFormat.FormatColor;

    public void Initialise(Vector3I world_position, int face_count, int lod)
    {
        Position = (Vector3)world_position;
        _lod = lod;

		_arrays.Resize((int)Mesh.ArrayType.Max);

        var vertex = new Vector3[face_count * 6];
        var normal = new Vector3[face_count * 6];
        var uv     = new Vector2[face_count * 6];
        var color  = new Color  [face_count * 6];

        // already initialised, duh (I asked AI)
        /*for (int index = 0; index < face_count * 6; index++)
        {
            vertex[index] = new();
            normal[index] = new();
            uv[index]     = new();
            color[index]  = new();
        }*/

        _arrays[__vertex] = vertex;
        _arrays[__normal] = normal;
        _arrays[__uv]     = uv;
        _arrays[__color]  = color;
    }
}