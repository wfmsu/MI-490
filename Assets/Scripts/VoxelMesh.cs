using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(VoxelObject))]
public class VoxelMesh : MonoBehaviour
{
    private VoxelObject _voxelObject;

    private void OnEnable() {
        _voxelObject = GetComponent<VoxelObject>();
        _voxelObject.Updated += OnVoxelsUpdated;
    }

    private void OnDisable() {
        _voxelObject.Updated -= OnVoxelsUpdated;
    }

    private void OnVoxelsUpdated() {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var mesh = BasicCubeMeshing(); // Basic cube meshing (all faces of leaf nodes)
        stopwatch.Stop();
        Debug.Log("Basic Mesh Time: " + stopwatch.ElapsedMilliseconds + "ms");
        stopwatch.Reset();
        stopwatch.Start();
        mesh = JobBasicCubeMeshing(); // Basic cube meshing + job system
        stopwatch.Stop();
        Debug.Log("Job System Mesh Time: " + stopwatch.ElapsedMilliseconds + "ms");
        //var mesh = GPUBasicCubeMeshing(); // Basic cube meshing + gpu
        //var mesh = CubeMeshing(); // Optimized cube meshing (visible faces + greedy meshing)
        //var mesh = JobCubeMeshing(); // Optimized cube meshing + job system
        //var mesh = GPUCubeMeshing(); // Optimized cube meshing + gpu
        if (mesh != null) {
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }

    /// <summary>
    /// Generate a mesh where every leaf node is represented by a cube.
    /// TODO: Implement 
    /// </summary>
    /// <returns>A generated mesh.</returns>
    private Mesh BasicCubeMeshing() {
        var mesh = new Mesh {
            name = "Voxel Mesh",
            indexFormat = IndexFormat.UInt32
        };

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uvs = new List<Vector2>(); // TODO
        var normals = new List<Vector3>();
        var colors = new List<Color>();
        
        Debug.Log(_voxelObject.Root.GetLeafVoxels().Count());
        var sides = new[] {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
        };
        foreach (var leaf in _voxelObject.Root.GetLeafVoxels().Where(x => x.Type.Id != 0)) {
            foreach (var side in sides) {
                // TODO: Only visible sides
                var vertexIndex = vertices.Count;
                var orthogonalVec1 = Vector3.Cross(side, side + new Vector3(1, 1, 1)).normalized;
                var orthogonalVec2 = Vector3.Cross(side, orthogonalVec1).normalized;
                orthogonalVec1 *= leaf.Size * 0.5f;
                orthogonalVec2 *= leaf.Size * 0.5f;

                var scale = Mathf.Sqrt(0.5f);
                var center = leaf.Position + side * leaf.Size * 0.5f;

                vertices.AddRange(new[] {
                    center + (orthogonalVec1 + orthogonalVec2) * scale + (orthogonalVec1 - orthogonalVec2) * scale,
                    center + (orthogonalVec1 - orthogonalVec2) * scale + (-orthogonalVec1 - orthogonalVec2) * scale,
                    center + (-orthogonalVec1 - orthogonalVec2) * scale + (-orthogonalVec1 + orthogonalVec2) * scale,
                    center + (-orthogonalVec1 + orthogonalVec2) * scale + (orthogonalVec1 + orthogonalVec2) * scale
                });

                triangles.AddRange(new[] {
                    vertexIndex + 0,
                    vertexIndex + 3,
                    vertexIndex + 2,
                    vertexIndex + 2,
                    vertexIndex + 1,
                    vertexIndex + 0
                });
                
                normals.AddRange(new[] {
                    side,
                    side,
                    side,
                    side
                });
                
                colors.AddRange(new[] {
                    leaf.Type.Color,
                    leaf.Type.Color,
                    leaf.Type.Color,
                    leaf.Type.Color
                });

                var size = leaf.Size;
                uvs.AddRange(new[] {
                    new Vector2(0, 0),
                    new Vector2(size, 0),
                    new Vector2(size, size),
                    new Vector2(0, size)
                });
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);

        return mesh;
    }

    private struct LeafVoxel {
        public Vector3 Position;
        public float Size;
        public byte Type;
        public Color Color;
    }

    private struct CalculateMeshDataJob : IJobParallelFor {
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> vertices;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> triangles;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> normals;
        [NativeDisableParallelForRestriction]
        public NativeArray<Color> colors;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> uvs;
        [ReadOnly]
        public NativeArray<LeafVoxel> leafVoxels;

        public void Execute(int i) {
            var sides = new[] {
                Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
            };
            var leaf = leafVoxels[i];
            if (leaf.Type == VoxelType.Empty.Id) 
                return;

            for (var j = 0; j < sides.Length; j++) {
                var side = sides[j];

                var orthogonalVec1 = Vector3.Cross(side, side + new Vector3(1, 1, 1)).normalized;
                var orthogonalVec2 = Vector3.Cross(side, orthogonalVec1).normalized;
                orthogonalVec1 *= leaf.Size * 0.5f;
                orthogonalVec2 *= leaf.Size * 0.5f;

                var scale = Mathf.Sqrt(0.5f);
                var center = leaf.Position + side * leaf.Size * 0.5f;

                var vertexIndex = i * 4 * 6 + j * 4;
                vertices[vertexIndex + 0] = center + (orthogonalVec1 + orthogonalVec2) * scale + (orthogonalVec1 - orthogonalVec2) * scale;
                vertices[vertexIndex + 1] = center + (orthogonalVec1 - orthogonalVec2) * scale + (-orthogonalVec1 - orthogonalVec2) * scale;
                vertices[vertexIndex + 2] = center + (-orthogonalVec1 - orthogonalVec2) * scale + (-orthogonalVec1 + orthogonalVec2) * scale;
                vertices[vertexIndex + 3] = center + (-orthogonalVec1 + orthogonalVec2) * scale + (orthogonalVec1 + orthogonalVec2) * scale;

                var triangleIndex = i * 6 * 6 + j * 6;
                triangles[triangleIndex + 0] = vertexIndex + 0;
                triangles[triangleIndex + 1] = vertexIndex + 3;
                triangles[triangleIndex + 2] = vertexIndex + 2;
                triangles[triangleIndex + 3] = vertexIndex + 2;
                triangles[triangleIndex + 4] = vertexIndex + 1;
                triangles[triangleIndex + 5] = vertexIndex + 0;
                
                normals[vertexIndex + 0] = side;
                normals[vertexIndex + 1] = side;
                normals[vertexIndex + 2] = side;
                normals[vertexIndex + 3] = side;

                colors[vertexIndex + 0] = leaf.Color;
                colors[vertexIndex + 1] = leaf.Color;
                colors[vertexIndex + 2] = leaf.Color;
                colors[vertexIndex + 3] = leaf.Color;

                var size = leaf.Size;
                uvs[vertexIndex + 0] = new Vector2(0, 0);
                uvs[vertexIndex + 1] = new Vector2(size, 0);
                uvs[vertexIndex + 2] = new Vector2(size, size);
                uvs[vertexIndex + 3] = new Vector2(0, size);
            }
        }
    }
    
    private Mesh JobBasicCubeMeshing() {
        // Create arrays to hold the data that will be calculated in parallel
        var leafVoxels = _voxelObject.Root.GetLeafVoxels().ToList();
        var leafCounts = leafVoxels.Count;
        
        var vertices = new NativeArray<Vector3>(leafCounts * 4 * 6, Allocator.TempJob);
        var triangles = new NativeArray<int>(leafCounts * 6 * 6, Allocator.TempJob);
        var normals = new NativeArray<Vector3>(leafCounts * 6 * 4, Allocator.TempJob);
        var colors = new NativeArray<Color>(leafCounts * 6 * 4, Allocator.TempJob);
        var uvs = new NativeArray<Vector2>(leafCounts * 6 * 4, Allocator.TempJob);
        var leaves = new NativeArray<LeafVoxel>(leafCounts, Allocator.TempJob);
        for (var i = 0; i < leafCounts; i++) {
            leaves[i] = new LeafVoxel {
                Position = leafVoxels[i].Position,
                Size = leafVoxels[i].Size,
                Type = leafVoxels[i].Type.Id,
                Color = leafVoxels[i].Type.Color
            };
        }

        // Create a job to calculate the data in parallel
        var job = new CalculateMeshDataJob {
            vertices = vertices,
            triangles = triangles,
            normals = normals,
            colors = colors,
            uvs = uvs,
            leafVoxels = leaves
        };

        // Schedule the job and wait for it to complete
        job.Schedule(leafCounts, 1).Complete();
        leaves.Dispose();
        
        // Create the mesh using the calculated data
        var mesh = new Mesh {
            name = "Voxel Mesh",
            indexFormat = IndexFormat.UInt32
        };
        
        mesh.SetVertices(vertices.ToList());
        vertices.Dispose();
        mesh.SetTriangles(triangles.ToList(), 0);
        triangles.Dispose();
        mesh.SetNormals(normals.ToList());
        normals.Dispose();
        mesh.SetColors(colors.ToList());
        colors.Dispose();
        mesh.SetUVs(0, uvs.ToList());
        uvs.Dispose();
        return mesh;
    }
    
}
