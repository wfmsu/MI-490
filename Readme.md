# Roadmap
1. Implement basic cube meshing with correct UVs, Normals, and Colors/Textures.
2. Utilize Burst Compiler/Job System to speed things up.
3. Move this to the GPU using compute shaders.
4. Set up a system to benchmark my implementations so far.
4. Implement performance improvements in basic algorithm such as only rendering visable quads and greedy meshing (combining quads where possible).
5. Research further and pick a more advanced meshing algorithm such as marching cubes or surface nets. All of these algorithms have advantages and disadvantages. Additionally, I might need to restructure my SVO for a specific algorithm.
6. Pick one of these algorithms and implement it.
7. See if it's possible/worthwhile to utilize the GPU/Jobs for performance improvements.
8. From there, pick a more advanced version of that algorithm (think surface nets -> dual contouring of hermite data, or marching cubes -> transvoxel algorithm).
9. Implement, optimize, repeat

# Collection of possibly useful links
- https://bonsairobo.medium.com/smooth-voxel-mapping-a-technical-deep-dive-on-real-time-surface-nets-and-texturing-ef06d0f8ca14
- https://ngildea.blogspot.com/2014/11/implementing-dual-contouring.html
- https://www.cs.rice.edu/~jwarren/papers/dualcontour.pdf
- https://people.eecs.berkeley.edu/~jrs/meshpapers/SchaeferWarren2.pdf
- https://github.com/emilk/Dual-Contouring/tree/master
- https://www.youtube.com/watch?v=B_5VBtpVuLQ
- https://research.nvidia.com/sites/default/files/publications/laine2010i3d_paper.pdf
- https://transvoxel.org/