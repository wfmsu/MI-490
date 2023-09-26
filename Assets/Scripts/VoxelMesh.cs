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
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var colors = new List<Color>();
        
        Debug.Log(_voxelObject.Root.GetLeafVoxels().Count());
        foreach (var leaf in _voxelObject.Root.GetLeafVoxels().Where(x => x.Type.Id != 0)) {
            var initialIndex = vertices.Count;
            
            var c0 = new Vector3(leaf.Position.x - leaf.Size * 0.5f, leaf.Position.y - leaf.Size * 0.5f, leaf.Position.z + leaf.Size * 0.5f);
            var c1 = new Vector3(leaf.Position.x + leaf.Size * 0.5f, leaf.Position.y - leaf.Size * 0.5f, leaf.Position.z + leaf.Size * 0.5f);
            var c2 = new Vector3(leaf.Position.x - leaf.Size * 0.5f, leaf.Position.y + leaf.Size * 0.5f, leaf.Position.z + leaf.Size * 0.5f);
            var c3 = new Vector3(leaf.Position.x + leaf.Size * 0.5f, leaf.Position.y + leaf.Size * 0.5f, leaf.Position.z + leaf.Size * 0.5f);
            var c4 = new Vector3(leaf.Position.x - leaf.Size * 0.5f, leaf.Position.y - leaf.Size * 0.5f, leaf.Position.z - leaf.Size * 0.5f);
            var c5 = new Vector3(leaf.Position.x + leaf.Size * 0.5f, leaf.Position.y - leaf.Size * 0.5f, leaf.Position.z - leaf.Size * 0.5f);
            var c6 = new Vector3(leaf.Position.x - leaf.Size * 0.5f, leaf.Position.y + leaf.Size * 0.5f, leaf.Position.z - leaf.Size * 0.5f);
            var c7 = new Vector3(leaf.Position.x + leaf.Size * 0.5f, leaf.Position.y + leaf.Size * 0.5f, leaf.Position.z - leaf.Size * 0.5f);

            // Add the vertices for each face of the cube
            vertices.AddRange(new[] { c0, c1, c2, c3, c4, c5, c6, c7 });
            
            // Add the triangles for each face of the cube
            triangles.AddRange(new[] { 
                //Top
                initialIndex + 7, initialIndex + 6, initialIndex + 2,
                initialIndex + 2, initialIndex + 3, initialIndex + 7,

                //Bottom
                initialIndex + 0, initialIndex + 4, initialIndex + 5,
                initialIndex + 5, initialIndex + 1, initialIndex + 0,

                //Left
                initialIndex + 0, initialIndex + 2, initialIndex + 6,
                initialIndex + 6, initialIndex + 4, initialIndex + 0,

                //Right
                initialIndex + 7, initialIndex + 3, initialIndex + 1,
                initialIndex + 1, initialIndex + 5, initialIndex + 7,

                //Front
                initialIndex + 3, initialIndex + 2, initialIndex + 0,
                initialIndex + 0, initialIndex + 1, initialIndex + 3,

                //Back
                initialIndex + 4, initialIndex + 6, initialIndex + 7,
                initialIndex + 7, initialIndex + 5, initialIndex + 4
            });
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.SetColors(colors);

        return mesh;
    }
    
}
