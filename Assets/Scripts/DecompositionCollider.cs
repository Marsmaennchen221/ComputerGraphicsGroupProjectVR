using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Decomposes a source mesh into many small MeshColliders, all attached to THIS GameObject.
/// No child GameObjects are created.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
public class DecompositionCollider : MonoBehaviour
{
    [Header("Voxel Grid (World Units)")]
    [Min(0.0001f)] public float spacingWorld = 0.25f;
    [Min(0f)] public float boundsPaddingWorld = 0.0f;

    [Header("Generated MeshColliders")]
    public bool convex = false;
    public bool isTrigger = false;
    public PhysicsMaterial colliderMaterial = null;

    [Header("Safety / Limits")]
    [Min(1)] public int maxColliders = 2000;

    [Tooltip("Skip groups with less than N triangles (reduces collider count).")]
    [Min(1)] public int minTrianglesPerVoxel = 4;

    [Header("Debug")]
    public bool logSummary = true;

    // We track meshes we generate so we can destroy them on regen to avoid memory leaks.
    [NonSerialized] private readonly List<Mesh> _generatedMeshes = new List<Mesh>();

    /// <summary>
    /// Call this from inspector button or your own code.
    /// </summary>
    public void Generate()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.LogWarning("[DecompositionCollider] MeshFilter missing.");
            return;
        }

        Mesh src = mf.sharedMesh;
        if (src == null)
        {
            Debug.LogWarning("[DecompositionCollider] MeshFilter has no mesh assigned.");
            return;
        }

        if (!src.isReadable)
        {
            Debug.LogWarning("[DecompositionCollider] Source mesh is not readable. Enable Read/Write in import settings.");
            return;
        }

        CleanupGenerated();

        // Pull source data
        Vector3[] vLocal = src.vertices;
        int[] tris = src.triangles;

        if (tris == null || tris.Length < 3 || vLocal == null || vLocal.Length < 3)
        {
            Debug.LogWarning("[DecompositionCollider] Source mesh has insufficient geometry.");
            return;
        }

        // Compute WORLD bounds of the mesh (based on renderer if possible, else from vertices).
        Bounds worldBounds = ComputeWorldBounds(src);

        // Expand bounds
        worldBounds.Expand(boundsPaddingWorld * 2f);

        // Map voxel -> list of triangle start indices (into tris[])
        // Each triangle is tris[t], tris[t+1], tris[t+2]
        var voxelToTriStarts = new Dictionary<VoxelKey, List<int>>(1024);

        // Precompute transform
        Transform tr = transform;

        int triCount = tris.Length / 3;
        for (int ti = 0; ti < triCount; ti++)
        {
            int t0 = tris[ti * 3 + 0];
            int t1 = tris[ti * 3 + 1];
            int t2 = tris[ti * 3 + 2];

            // Triangle centroid in WORLD
            Vector3 cLocal = (vLocal[t0] + vLocal[t1] + vLocal[t2]) / 3f;
            Vector3 cWorld = tr.TransformPoint(cLocal);

            VoxelKey key = WorldToVoxelKey(cWorld, worldBounds.min, spacingWorld);

            if (!voxelToTriStarts.TryGetValue(key, out var list))
            {
                list = new List<int>(16);
                voxelToTriStarts.Add(key, list);
            }
            list.Add(ti * 3);
        }

        int created = 0;
        int skippedSmall = 0;
        int skippedLimit = 0;

        foreach (var kvp in voxelToTriStarts)
        {
            List<int> triStarts = kvp.Value;
            int trisInVoxel = triStarts.Count;

            if (trisInVoxel < minTrianglesPerVoxel)
            {
                skippedSmall++;
                continue;
            }

            if (created >= maxColliders)
            {
                skippedLimit++;
                continue;
            }

            Mesh voxelMesh = BuildVoxelMesh(src, vLocal, tris, triStarts);
            if (voxelMesh == null)
                continue;

            // Attach collider component ON THIS GameObject
            MeshCollider mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = voxelMesh;
            mc.convex = convex;
            mc.isTrigger = isTrigger;
            mc.material = colliderMaterial;

            _generatedMeshes.Add(voxelMesh);
            created++;
        }

        if (logSummary)
        {
            Debug.Log($"[DecompositionCollider] Generated {created} MeshColliders on '{name}'. " +
                      $"Voxels total: {voxelToTriStarts.Count}. " +
                      $"Skipped (small): {skippedSmall}. Skipped (limit): {skippedLimit}.");
        }
    }

    /// <summary>
    /// Removes previously generated MeshColliders and destroys generated meshes.
    /// This removes ONLY MeshCollider components (not BoxCollider, etc.).
    /// </summary>
    public void CleanupGenerated()
    {
        // Destroy MeshColliders on this GameObject
        MeshCollider[] existing = GetComponents<MeshCollider>();
        for (int i = 0; i < existing.Length; i++)
        {
            DestroySmart(existing[i]);
        }

        // Destroy meshes we created
        for (int i = 0; i < _generatedMeshes.Count; i++)
        {
            if (_generatedMeshes[i] != null)
                DestroySmart(_generatedMeshes[i]);
        }
        _generatedMeshes.Clear();
    }

    private Bounds ComputeWorldBounds(Mesh src)
    {
        // Prefer Renderer bounds if present (already in world space)
        var rend = GetComponent<Renderer>();
        if (rend != null)
            return rend.bounds;

        // Else compute from vertices transformed to world
        Vector3[] v = src.vertices;
        Transform tr = transform;
        Vector3 p0 = tr.TransformPoint(v[0]);
        Bounds b = new Bounds(p0, Vector3.zero);
        for (int i = 1; i < v.Length; i++)
            b.Encapsulate(tr.TransformPoint(v[i]));
        return b;
    }

    private Mesh BuildVoxelMesh(Mesh src, Vector3[] vLocal, int[] tris, List<int> triStarts)
    {
        // Build a mesh that contains only triangles listed in triStarts.
        // Use original vertex indices to avoid float-key merging issues.
        var oldToNew = new Dictionary<int, int>(256);
        var newVerts = new List<Vector3>(256);
        var newTris = new List<int>(triStarts.Count * 3);

        for (int i = 0; i < triStarts.Count; i++)
        {
            int start = triStarts[i];

            int aOld = tris[start + 0];
            int bOld = tris[start + 1];
            int cOld = tris[start + 2];

            int aNew = MapVertex(aOld, vLocal, oldToNew, newVerts);
            int bNew = MapVertex(bOld, vLocal, oldToNew, newVerts);
            int cNew = MapVertex(cOld, vLocal, oldToNew, newVerts);

            newTris.Add(aNew);
            newTris.Add(bNew);
            newTris.Add(cNew);
        }

        if (newTris.Count < 3)
            return null;

        Mesh m = new Mesh();

#if UNITY_2020_2_OR_NEWER
        // Safer for large meshes
        if (newVerts.Count > 65535)
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
#endif

        m.vertices = newVerts.ToArray();
        m.triangles = newTris.ToArray();

        // Keep it simple and robust: recalc normals/bounds
        m.RecalculateNormals();
        m.RecalculateBounds();

        // Name for debugging
        m.name = $"DecompCollider_{name}_{GetInstanceID()}_{triStarts.Count}tris";

        return m;
    }

    private int MapVertex(int oldIndex, Vector3[] vLocal,
                          Dictionary<int, int> oldToNew,
                          List<Vector3> newVerts)
    {
        if (oldToNew.TryGetValue(oldIndex, out int newIndex))
            return newIndex;

        newIndex = newVerts.Count;
        oldToNew.Add(oldIndex, newIndex);
        newVerts.Add(vLocal[oldIndex]);
        return newIndex;
    }

    private static VoxelKey WorldToVoxelKey(Vector3 pWorld, Vector3 worldMin, float spacing)
    {
        Vector3 rel = pWorld - worldMin;
        int ix = Mathf.FloorToInt(rel.x / spacing);
        int iy = Mathf.FloorToInt(rel.y / spacing);
        int iz = Mathf.FloorToInt(rel.z / spacing);
        return new VoxelKey(ix, iy, iz);
    }

    private void DestroySmart(UnityEngine.Object obj)
    {
        if (obj == null) return;

        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
#if UNITY_EDITOR
            DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }
    }

    [Serializable]
    private struct VoxelKey : IEquatable<VoxelKey>
    {
        public int x, y, z;

        public VoxelKey(int x, int y, int z)
        {
            this.x = x; this.y = y; this.z = z;
        }

        public bool Equals(VoxelKey other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is VoxelKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                // Stable hash for 3 ints
                int h = 17;
                h = h * 31 + x;
                h = h * 31 + y;
                h = h * 31 + z;
                return h;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DecompositionCollider))]
    private class DecompositionColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var t = (DecompositionCollider)target;

            GUILayout.Space(8);

            if (GUILayout.Button("Generate Colliders (On This GameObject)"))
            {
                t.Generate();
                EditorUtility.SetDirty(t);
            }

            if (GUILayout.Button("Cleanup Generated Colliders"))
            {
                t.CleanupGenerated();
                EditorUtility.SetDirty(t);
            }
        }
    }
#endif
}
