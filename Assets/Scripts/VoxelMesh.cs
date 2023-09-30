using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

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
        var mesh = BasicCubeMeshing();
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
    
}
