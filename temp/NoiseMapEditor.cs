using Godot;
using System.Collections.Generic;
using System.Threading;

[Tool, GlobalClass]
public partial class NoiseMapEditor : Area3D
{
	[ExportToolButton("Generate Cache")] public Callable generate_cache_button => Callable.From(() => generate_cache()); 
	[ExportToolButton("Save cache")]     public Callable save_cache_button => Callable.From(() => save_cache()); 

	[Export] MapResource map;
	
	List<Thread>  threads = new List<Thread>();
	Godot.Mutex   noise_node_list_mutex = new Godot.Mutex();

	int node_index_noise;
	int node_index_mesh;

	List<MeshInstance3D> meshes;
	Godot.Mutex mutex_meshes = new();

	Godot.Mutex threads_count_mutex = new Godot.Mutex();
	int threads_count = 0;

	void save_cache()
	{
		if (map == null) return;

		int tblocks = 0, tchunks = 0, tfaces = 0;

		foreach (OctNode node in map.nodes)
		{
			int blocks = node.total_blocks;
			if (blocks > 0)
			{
				tblocks += blocks;
				tfaces += node.face_count;
				tchunks++;
			}
		}

		int chbuf = map.lod * map.lod * map.lod / 8;
		int chtot = tchunks * chbuf / 1024 / 1024;

		int vcnt = tfaces * 6 * 36 / 1024 / 1024;

		GD.Print($"Total blocks: {tblocks}, faces: {tfaces}, chunks {tchunks}");
		GD.Print($"Chunk buffers: {chtot}Mb, Vertices: {vcnt}Mb");
	}

	void SetThreadsCount(int count)
	{
		threads_count_mutex.Lock();
		threads_count = count;
		threads_count_mutex.Unlock();
	}

	int GetThreadsCount()
	{
		threads_count_mutex.Lock();
		int count = threads_count;
		threads_count_mutex.Unlock();
		return count;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		noise_node_list_mutex.Lock();
		int remaining = node_index_noise + node_index_mesh;
		noise_node_list_mutex.Unlock();

		if (remaining == 0 && GetThreadsCount() > 0)
		{
			GD.Print($"All nodes processed, closing threads...");
			_close_threads();
			GD.Print($"Threads closed.");
		}
	}

	void _close_threads()
	{
		foreach (Thread thread in threads)
		{
			if (thread.IsAlive) thread.Join();
		}
		threads = new ();
	}

	// Local space
	public bool GetBlock(int x, int y, int z)
	{
		if (map == null) return false;

		if (map.nodes == null) return false;
		if (x < 0 || y < 0 || z < 0) return false;
		if (x >= map.Size.X || y >= map.Size.Y || z >= map.Size.Z) return false;

		int rem = map.lod - 1;

		for (int i = 0; i < map.nodes_count; i++)
		{
			OctNode node = map.nodes[i];
			if (x < node.Position.X + map.lod &&
				y < node.Position.Y + map.lod &&
				z < node.Position.Z + map.lod)
			{
				return node.GetBlock(x & rem, y & rem, z & rem);
			}
		}

		return false;
	}

	public void SetBlock(int x, int y, int z, bool value)
	{
		if (map == null) return;

		if (map.nodes == null || x < 0 || y < 0 || z < 0 || 
			x >= map.Size.X || y >= map.Size.Y || z >= map.Size.Z)
		{
			return;
		}

		int rem = map.lod - 1;

		// assume ordered
		for (int i = 0; i < map.nodes_count; i++)
		{
			OctNode node = map.nodes[i];
			if (x < node.Position.X + map.lod &&
				y < node.Position.Y + map.lod &&
				z < node.Position.Z + map.lod)
			{
				node.SetBlock(x & rem, y & rem, z & rem, value);
				return;
			}
		}
	}

	Vector3I max_pos = new Vector3I();

	public void generate_cache()
	{
		// check
		if (map == null) return;

		if (map.lod < 8)
		{
			GD.PrintErr("LOD must be 8 or greater");
			return;
		}
		if (map.Size.X < 8 || map.Size.Y < 8 || map.Size.Z < 8)
		{
			GD.PrintErr("Size must be greater than 8");
			return;
		}

		// initialize

		_close_threads();

		max_pos.X = (int)((map.Size.X + map.lod - 1) / map.lod) * map.lod;
		max_pos.Y = (int)((map.Size.Y + map.lod - 1) / map.lod) * map.lod;
		max_pos.Z = (int)((map.Size.Z + map.lod - 1) / map.lod) * map.lod;

		map.nodes_count = (max_pos.X / map.lod) * (max_pos.Y / map.lod) * (max_pos.Z / map.lod);
		
		map.nodes = new OctNode[map.nodes_count];
		if (meshes != null) foreach (MeshInstance3D mi in meshes) mi.QueueFree();
		meshes = new();

		GD.Print($"Generating cache for {map.MapName} size {max_pos} with lod {map.lod} and total {map.nodes_count} nodes.");

		int i = 0;

		for (int z = 0; z < max_pos.Z; z += map.lod)
		{
			for (int y = 0; y < max_pos.Y; y += map.lod)
			{
				for (int x = 0; x < max_pos.X; x += map.lod)
				{
					map.nodes[i] = new OctNode
					{
						Position = new Vector3I(x, y, z),
						lod = map.lod,
						Size = new Vector3I(map.Size.X, map.Size.Y, map.Size.Z)
					};

					i++;
				}
			}
		}

		if ( i != map.nodes_count )
		{
			GD.Print($"ERROR: i {i} != nodes_count {map.nodes_count}");
		}

		node_index_mesh = map.nodes_count;
		node_index_noise = map.nodes_count;

		// run threads

		int max_threads = System.Environment.ProcessorCount;
		if (max_threads < 1) max_threads = 1;
		
		SetThreadsCount(max_threads);

		GD.Print($"Initialised total {map.nodes_count} nodes with lod {map.lod}");
		GD.Print($"Starting threads... (max: {max_threads})");

		for (int index = 0; index < max_threads; index++)
		{
			Thread thread = new Thread(new ThreadStart(() => _thread_MAIN_generate_cache_data(index)));
			threads.Add(thread);
			thread.Start();
		}
	}

	//_

	void _thread_MAIN_generate_cache_data(int thread_id)
	{
		GD.Print($"Thread {thread_id} started");

		int node_index = -1;

		do
		{
			noise_node_list_mutex.Lock();
			node_index = -1;

			if (node_index_noise > 0)
			{
				node_index_noise--;
				node_index = node_index_noise;
			}
			
			noise_node_list_mutex.Unlock();

			if (node_index != -1)
			{
				_process_noise_node(thread_id,node_index);
			}
		}
		while (node_index != -1);

		SetThreadsCount(GetThreadsCount() - 1);

		GD.Print($"Thread {thread_id} finished generating cache");

		while (GetThreadsCount() > 0)
		{
			//GD.Print($"Thread {thread_id} waiting for other threads to finish...");
			Thread.Sleep(1);
		}

		GD.Print($"Thread {thread_id} creating cached mesh data...");

		do 
		{
			noise_node_list_mutex.Lock();
			node_index = -1;

			if (node_index_mesh > 0)
			{
				node_index_mesh--;
				node_index = node_index_mesh;
			}

			noise_node_list_mutex.Unlock();

			if (node_index != -1)
			{
				_process_mesh_node(thread_id,node_index);
			}
		} while (node_index != -1);

		GD.Print($"{thread_id} Finished...");
	}

	//_

	void _process_noise_node(int thread_id, int node_index)
	{
		OctNode node = map.nodes[node_index];

		node.total_blocks = 0;

		for (int z = 0; z < map.lod; z++)
		{
			int local_z = node.Position.Z + z;
			float zmult = map.AddBlocks._curve_z.Sample(1.0f / node.Size.Z * local_z);
			
			for (int y = 0; y < map.lod; y++)
			{
				int local_y = node.Position.Y + y;
				float ymult = map.AddBlocks._curve_y.Sample(1.0f - 1.0f / node.Size.Y * local_y);
				
				for (int x = 0; x < map.lod; x++)
				{
					int local_x = node.Position.X + x;
					float xmult = map.AddBlocks._curve_x.Sample(1.0f / node.Size.X * local_x);

					float local_mult = xmult * ymult * zmult;
					float value 	 = (map.AddBlocks._noise.GetNoise3D(local_x, local_y, local_z) + 1.0f) / 2.0f;
					float noise_add  = local_mult * value;

					bool  block      = noise_add > map.AddBlocks._range.X && noise_add < map.AddBlocks._range.Y;

					if (block && map.SubBlocks._noise != null)
					{						
						value = (map.SubBlocks._noise.GetNoise3D(local_x, local_y, local_z) + 1.0f) / 2.0f;

						float _zmult = map.SubBlocks._curve_z.Sample(1.0f / node.Size.Z * local_z);
						float _ymult = map.SubBlocks._curve_y.Sample(1.0f / node.Size.Y * local_y);
						xmult = map.SubBlocks._curve_x.Sample(1.0f / map.Size.X * local_x);

						local_mult = _zmult * _ymult * xmult;
						
						float noise_sub = local_mult * value;

						if (noise_sub > map.SubBlocks._range.X && noise_sub < map.SubBlocks._range.Y)
						{
							block = false;
						}
					}

					if (block) // if no blocks added then no memory is allocated
					{
						node.SetBlock(x, y, z, true);
						node.total_blocks++;
					}
				}
			}
		}

		if (node.total_blocks == 0) return;

		node.face_count = 0;

		for (int z = 0; z < map.lod; z++)
		{
			int nz = z + node.Position.Z;

			for (int y = 0; y < map.lod; y++)
			{
				int ny = y + node.Position.Y;
			
				for (int x = 0; x < map.lod; x++)
				{
					if (node.GetBlock(x, y, z))
					{
						int nx = x + node.Position.X;

						bool x_neg = x > 0           ? node.GetBlock(x - 1, y, z) : GetBlock(nx - 1, ny, nz);
						bool x_pos = x < map.lod - 1 ? node.GetBlock(x + 1, y, z) : GetBlock(nx + 1, ny, nz);
						bool y_neg = y > 0			 ? node.GetBlock(x, y - 1, z) : GetBlock(nx, ny - 1, nz);
						bool y_pos = y < map.lod - 1 ? node.GetBlock(x, y + 1, z) : GetBlock(nx, ny + 1, nz);
						bool z_neg = z > 0 			 ? node.GetBlock(x, y, z - 1) : GetBlock(nx, ny, nz - 1);
						bool z_pos = z < map.lod - 1 ? node.GetBlock(x, y, z + 1) : GetBlock(nx, ny, nz + 1);

						if (!x_neg) node.face_count++;
						if (!x_pos) node.face_count++;
						if (!y_neg) node.face_count++;
						if (!y_pos) node.face_count++;
						if (!z_neg) node.face_count++;
						if (!z_pos) node.face_count++;
					}
				}
			}
		}

		// here we are done with the actual cache...

		GD.Print($"Thread {thread_id} node {node_index}, {node.Position}, {node.total_blocks} blocks");
	}

	// TODO count verts before processing faces
	void _process_mesh_node(int thread_id, int node_index)
	{
		OctNode node = map.nodes[node_index];

		if (node.total_blocks == 0) return;
		
		MeshResource mr = new();
		mr.init(node.face_count);

		int i = 0;

		for (int z = 0; z < map.lod; z++)
		{
			int nz = z + node.Position.Z;

			for (int y = 0; y < map.lod; y++)
			{
				int ny = y + node.Position.Y;

				float sample_offset = 1.0f / node.Size.Y * (node.Position.Y + y);
				Color color = map.gradient.Sample(sample_offset);

				for (int x = 0; x < map.lod; x++)
				{
					if (node.GetBlock(x, y, z))
					{
						int nx = x + node.Position.X;

						bool x_neg = x > 0           ? node.GetBlock(x - 1, y, z) : GetBlock(nx - 1, ny, nz);
						bool x_pos = x < map.lod - 1 ? node.GetBlock(x + 1, y, z) : GetBlock(nx + 1, ny, nz);
						bool y_neg = y > 0			 ? node.GetBlock(x, y - 1, z) : GetBlock(nx, ny - 1, nz);
						bool y_pos = y < map.lod - 1 ? node.GetBlock(x, y + 1, z) : GetBlock(nx, ny + 1, nz);
						bool z_neg = z > 0 			 ? node.GetBlock(x, y, z - 1) : GetBlock(nx, ny, nz - 1);
						bool z_pos = z < map.lod - 1 ? node.GetBlock(x, y, z + 1) : GetBlock(nx, ny, nz + 1);

						if (!x_neg) { _add_face(i, node, x, y, z, _face_xneg, 0, color, mr); i += 6; }
						if (!x_pos) { _add_face(i, node, x, y, z, _face_xpos, 1, color, mr); i += 6; }
						if (!y_neg) { _add_face(i, node, x, y, z, _face_ypos, 2, color, mr); i += 6; }
						if (!y_pos) { _add_face(i, node, x, y, z, _face_yneg, 3, color, mr); i += 6; }
						if (!z_neg) { _add_face(i, node, x, y, z, _face_zpos, 5, color, mr); i += 6; }
						if (!z_pos) { _add_face(i, node, x, y, z, _face_zneg, 4, color, mr); i += 6; }
					}
				}
			}
		}

		//MeshInstance3D mesh_instance = new();
		//mesh_instance.Position = node.Position;
		mr.Position = node.Position;

		ArrayMesh mesh = new();

		Godot.Collections.Array arrays = [];
		arrays.Resize((int)Mesh.ArrayType.Max);

		arrays[(int)Mesh.ArrayType.Vertex] = mr._vertices;
		arrays[(int)Mesh.ArrayType.Normal] = mr._normals;
		arrays[(int)Mesh.ArrayType.TexUV] = mr._uvs;
		arrays[(int)Mesh.ArrayType.Color] = mr._colors;

		Mesh.ArrayFormat flags = Mesh.ArrayFormat.FormatVertex | 
								 Mesh.ArrayFormat.FormatNormal | 
								 Mesh.ArrayFormat.FormatTexUV |
								 Mesh.ArrayFormat.FormatColor;
		
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null, flags);
		if (map.material != null) mesh.SurfaceSetMaterial(0, map.material);

		mr.Mesh = mesh;

		mutex_meshes.Lock();
		meshes.Add(mr);
		mutex_meshes.Unlock();

		CallDeferred("add_child", mr);

		GD.Print($"Node {node_index}, pos:{node.Position}, Mesh {node.total_blocks} blocks, {node.face_count} faces. Finished...");
	}

	void _add_face(int i, OctNode node, int x, int y, int z, List<Vector3> face_vertices, int face_index, Color color, MeshResource mr)
	{
		Vector3 offset = new Vector3(x, y, z);

		mr._vertices[i]   = face_vertices[0] + offset;
		mr._vertices[i+1] = face_vertices[1] + offset;
		mr._vertices[i+2] = face_vertices[2] + offset;
		mr._vertices[i+3] = face_vertices[0] + offset;
		mr._vertices[i+4] = face_vertices[2] + offset;
		mr._vertices[i+5] = face_vertices[3] + offset;
		
		mr._normals[i]   = _face_normals[face_index];
		mr._normals[i+1] = _face_normals[face_index];
		mr._normals[i+2] = _face_normals[face_index];
		mr._normals[i+3] = _face_normals[face_index];
		mr._normals[i+4] = _face_normals[face_index];
		mr._normals[i+5] = _face_normals[face_index];

		mr._uvs[i]   = _uvs[0];
		mr._uvs[i+1] = _uvs[1];
		mr._uvs[i+2] = _uvs[2];
		mr._uvs[i+3] = _uvs[3];
		mr._uvs[i+4] = _uvs[4];
		mr._uvs[i+5] = _uvs[5];
		
		mr._colors[i]   = color;
		mr._colors[i+1] = color;
		mr._colors[i+2] = color;
		mr._colors[i+3] = color;
		mr._colors[i+4] = color;
		mr._colors[i+5] = color;
	}

	List<Vector2> _uvs = new()
	{
		new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 0),
		new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1)
	};

	List<Vector3> _face_xneg = new() { 
		new Vector3(0,0,0), 
		new Vector3(0,1,0), 
		new Vector3(0,1,1), 
		new Vector3(0,0,1) };

	List<Vector3> _face_xpos = new() { 
		new Vector3(1,0,0), 
		new Vector3(1,0,1), 
		new Vector3(1,1,1), 
		new Vector3(1,1,0) };
	
	List<Vector3> _face_yneg = new() { 
		new Vector3(0,1,0), 
		new Vector3(1,1,0), 
		new Vector3(1,1,1), 
		new Vector3(0,1,1) };
	
	List<Vector3> _face_ypos = new() {
		new Vector3(0,0,0),
		new Vector3(0,0,1),
		new Vector3(1,0,1),
		new Vector3(1,0,0) };
	
	List<Vector3> _face_zneg = new() {
		new Vector3(0,0,1),
		new Vector3(0,1,1),
		new Vector3(1,1,1),
		new Vector3(1,0,1) };
	
	List<Vector3> _face_zpos = new() {
		new Vector3(0,0,0),
		new Vector3(1,0,0),
		new Vector3(1,1,0),
		new Vector3(0,1,0) };
	
	List<Vector3> _face_normals = new() {
		new Vector3(-1,0,0),  // xneg
		new Vector3(1,0,0),   // xpos
		new Vector3(0,-1,0),  // yneg
		new Vector3(0,1,0),   // ypos
		new Vector3(0,0,-1),  // zneg
		new Vector3(0,0,1) }; // zpos
}
