using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelObject : MonoBehaviour
{
    private float _voxelSize;
    private byte _depth;
    private byte[] _voxels; // Sparse voxel octree where 0 is empty.

    [SerializeField] private bool showDebug;
    
    public delegate void UpdatedHandler();
    public event UpdatedHandler Updated;

    /// <summary>
    /// Represents a voxel in a voxel object.
    /// </summary>
    public class Voxel
    {
        private readonly VoxelObject _source;
        private readonly int _index;
        
        /// <summary>
        /// Gets the index of the voxel.
        /// </summary>
        public int Index => _index;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Voxel"/> class.
        /// </summary>
        /// <param name="source">The voxel object that contains the voxel.</param>
        /// <param name="index">The index of the voxel.</param>
        public Voxel(VoxelObject source, int index) {
            _source = source;
            _index = index;
        }
        
        /// <summary>
        /// Gets or sets the type of the voxel.
        /// </summary>
        public VoxelType Type {
            get => _index >= _source._voxels.Length ? null : VoxelType.Types[_source._voxels[_index]];
            set => _source._voxels[_index] = value.Id;
        }

        /// <summary>
        /// Gets the depth of this voxel.
        /// </summary>
        public int Depth => (int)Math.Log(7 * _index + 1, 8);
        
        /// <summary>
        /// Gets the height of this voxel.
        /// </summary>
        public int Height => _source._depth - Depth;
        
        /// <summary>
        /// Gets the size of this voxel.
        /// </summary>
        public float Size => _source._voxelSize * (int)Math.Pow(2, Height);
        
        /// <summary>
        /// Gets the child voxel at the specified index.
        /// </summary>
        /// <param name="childIndex">The index of the child voxel.</param>
        /// <returns>The child voxel.</returns>
        public Voxel this[int childIndex] => 8 * _index + childIndex + 1 >= _source._voxels.Length ? null : _source[8 * _index + childIndex + 1];

        /// <summary>
        /// Gets the children of this voxel.
        /// </summary>
        /// <returns>An array of 8 child voxels.</returns>
        public Voxel[] GetChildren() {
            var children = new Voxel[8];
            for (var i = 0; i < 8; i++) {
                children[i] = this[i];
            }
            return children;
        }
        
        /// <summary>
        /// Encodes the voxel index using Morton encoding.
        /// </summary>
        /// <returns>The Morton encoded index.</returns>
        public int MortonEncode() {
            return _index - (int)(Math.Pow(8, Depth) - 1) / 7;
        }
        
        /// <summary>
        /// Gets the local position of this voxel.
        /// </summary>
        /// <returns>The local position of the voxel.</returns>
        public Vector3 Position {
            get {
                float x = 0, y = 0, z = 0;
                var layerIndex = MortonEncode();
                var voxelSize = _source._voxelSize;
                voxelSize /= (float)Math.Pow(2, 1 - Height);
                for (var i = Depth; i > 0; i--) {
                    var bx = (byte)(layerIndex & 0b001);
                    var by = (byte)((layerIndex & 0b010) >> 1);
                    var bz = (byte)((layerIndex & 0b100) >> 2);
                    layerIndex >>= 3;
                    x += -voxelSize + bx * 2 * voxelSize;
                    y += -voxelSize + by * 2 * voxelSize;
                    z += -voxelSize + bz * 2 * voxelSize;
                    voxelSize *= 2;
                }
                return new Vector3(x, y, z);
            }
        }
        
        /// <summary>
        /// Checks if this voxel is a leaf node.
        /// </summary>
        /// <returns>True if the voxel is a leaf node, false otherwise.</returns>
        public bool IsLeaf() {
            return Height == 0 || GetChildren().Any(x => x.Type.Id != Type.Id);
        }
        
        /// <summary>
        /// Gets the leaf nodes of this voxel up to a specified maximum depth.
        /// </summary>
        /// <returns>An enumerable of leaf voxels.</returns>
        public IEnumerable<Voxel> GetLeafVoxels() { // TODO: This is very wrong
            if (Type.Id == VoxelType.Full.Id) {
                foreach (var node in GetChildren()) {
                    if (node == null) { // Needed?
                        continue;
                    }
                    foreach (var leaf in node.GetLeafVoxels()) {
                        yield return leaf;
                    }
                } 
            }
            if (Type.Id > 1) {
                yield return this;
            }
        }
    }

    /// <summary>
    /// Gets or sets a specific voxel of the object.
    /// </summary>
    public Voxel this[int index] => new(this, index);
    
    /// <summary>
    /// Gets or sets the root voxel of the object.
    /// </summary>
    public Voxel Root => this[0];

    
    

    // TODO: Debug below
    private void InsertSDF(Voxel voxel, byte voxelType, Func<Voxel, float> sdf) {
        // Set all max-depth nodes that are inside the sphere
        if (voxel.Height == 0) {
            voxel.Type = VoxelType.Types[voxelType];
            return;
        } 
        if (voxel.Type != VoxelType.Full) {
            foreach (var child in voxel.GetChildren()) {
                child.Type = voxel.Type;
            }
        }

        // Subdivide
        var allSame = true;
        byte? first = null;
        foreach (var child in voxel.GetChildren()) {
            // If the child node is contained in the sphere, populate it
            if (sdf(child) < child.Size)
                InsertSDF(child, voxelType, sdf);
            
            // If this is the first child, use its type for the check
            if (first == null)
                first = child.Type.Id;
            // Check for all other children, if any children are different then they are all not the same
            else if (first != child.Type.Id)
                allSame = false;

            // If one of the children is not a leaf node, then they are all not the same
            if (allSame && !child.IsLeaf())
                allSame = false;
        }
        
        // Reduce, if they are all not the same, nothing to do
        if (!allSame || first == VoxelType.Full.Id) {
            voxel.Type = VoxelType.Full; // Since I'm not using nulls, I'm using 0s
            return;
        }

        // If all children nodes are the same type, copy that type to the parent and set all child nodes to empty
        voxel.Type = VoxelType.Types[(int)first!];
        foreach (var child in voxel.GetChildren()) {
            child.Type = VoxelType.Empty;
        }
    }

    private void DebugInit() {
        _voxelSize = 1f;
        _depth = 6;
        var voxelCount = (int)Math.Pow(8, _depth + 1) / 7;
        Debug.Log("Created: " + voxelCount);
        _voxels = new byte[voxelCount];

        var scale = _voxelSize * (int)Math.Pow(2, _depth - 1);
        var center = Vector3.zero + scale * 0.5f * Vector3.up;
        InsertSDF(Root, VoxelType.White.Id, voxel => {
            return (voxel.Position - center).magnitude - scale / 2;
        });
        center = Vector3.zero + scale * 0.5f * Vector3.down;
        InsertSDF(Root, VoxelType.Black.Id, voxel => {
            return (voxel.Position - center).magnitude - scale / 2;
        });
        center = Vector3.zero;
        scale *= 1.25f;
        InsertSDF(Root, VoxelType.Red.Id, voxel => {
            return (voxel.Position - center).magnitude - scale / 2;
        });
        
        
        Updated?.Invoke();
    }
    
    
    
    // TODO: Replace with some sort of model voxelizer
    private void Awake() {
        DebugInit();
    }
    /*private void OnValidate() {
        if (_voxels == null || _voxels.Length == 0) {
            DebugInit();
        }
    }*/

    private void OnDrawGizmos() {
        if (!showDebug || _voxels == null || _voxels.Length == 0) {
            return;
        }
        DrawVoxelDebug(Root, _depth);
    }
    
    private readonly Color _minColor = new(1, 1, 1, 1f);
    private readonly Color _maxColor = new(0, 0.5f, 1, 0.25f);
    private void DrawVoxelDebug(Voxel voxel, byte maxDepth) {
        if (voxel.Depth > maxDepth) {
            return;
        }

        foreach (var child in voxel.GetChildren()) {
            var type = child?.Type;
            if (type != null && type.Id != 0) {
                DrawVoxelDebug(child, maxDepth);
            }
        }
        Gizmos.color = Color.Lerp(_minColor, _maxColor, voxel.Depth / (float)_depth);
        Gizmos.DrawWireCube(transform.position + voxel.Position, new Vector3(voxel.Size, voxel.Size, voxel.Size));
    }
}
