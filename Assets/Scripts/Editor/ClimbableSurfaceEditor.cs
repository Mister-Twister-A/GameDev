#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;



[CustomEditor(typeof(ClimbableSurface))]
public class ClimbableSurfaceEditor : Editor
{
    private const float PositionEpsilon = 1e-4f;
    private const float NormalDotThreshold = 0.999f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var surface = (ClimbableSurface)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Build Face Graph"))
        {
            Build(surface);
        }

        if (surface.faces != null && surface.faces.Length > 0)
        {
            EditorGUILayout.HelpBox(
                $"{surface.faces.Length} faces, {surface.triangleToFace?.Length ?? 0} triangles mapped.",
                MessageType.Info);
        }
    }

    [MenuItem("GameObject/Climbable/Build Face Graph", false, 10)]
    private static void BuildFromMenu(MenuCommand cmd)
    {
        var go = cmd.context as GameObject;
        if (go == null) return;

        var surface = go.GetComponent<ClimbableSurface>();
        if (surface == null)
            surface = go.AddComponent<ClimbableSurface>();

        Build(surface);
    }

    [MenuItem("GameObject/Climbable/Link External Neighbors (Rig)", false, 11)]
    private static void LinkExternalNeighborsFromMenu(MenuCommand cmd)
    {
        var root = cmd.context as GameObject;
        if (root == null) return;

        var surfaces = root.GetComponentsInChildren<ClimbableSurface>();

        if (surfaces.Length < 2)
        {
            Debug.LogWarning("Need at least 2 ClimbableSurface parts.");
            return;
        }

        const float maxDistance = 0.03f;
        const float minParallel = 0.9f;

        var edges = new List<(ClimbableSurface surface, int face, int edge, Vector3 a, Vector3 b)>();

        foreach (var surface in surfaces)
        {
            if (surface.faces == null) continue;

            for (int f = 0; f < surface.faces.Length; f++)
            {
                var face = surface.faces[f];

                for (int e = 0; e < face.vertices.Length; e++)
                {
                    if (face.neighborIndices[e] != -1) continue;

                    if (face.externalNeighborSurface != null &&
                        face.externalNeighborSurface[e] != null)
                        continue;

                    Vector3 a = surface.transform.TransformPoint(face.vertices[e]);
                    Vector3 b = surface.transform.TransformPoint(
                        face.vertices[(e + 1) % face.vertices.Length]);

                    edges.Add((surface, f, e, a, b));
                }
            }
        }

        var used = new HashSet<int>();
        int linkCount = 0;

        for (int i = 0; i < edges.Count; i++)
        {
            if (used.Contains(i)) continue;

            var a = edges[i];

            float bestScore = float.MaxValue;
            int bestIndex = -1;

            for (int j = i + 1; j < edges.Count; j++)
            {
                if (used.Contains(j)) continue;

                var b = edges[j];

                if (a.surface == b.surface)
                    continue;

                Vector3 dirA = (a.b - a.a).normalized;
                Vector3 dirB = (b.b - b.a).normalized;

                if (Vector3.Dot(dirA, dirB) < -minParallel)
                    dirB = -dirB;

                if (Mathf.Abs(Vector3.Dot(dirA, dirB)) < minParallel)
                    continue;

                float d1 = Vector3.Distance(a.a, b.a);
                float d2 = Vector3.Distance(a.b, b.b);
                float d3 = Vector3.Distance(a.a, b.b);
                float d4 = Vector3.Distance(a.b, b.a);

                float endpointDistance =Mathf.Min(d1 + d2, d3 + d4);

                float midpointDistance =Vector3.Distance((a.a + a.b) * 0.5f,(b.a + b.b) * 0.5f);

                float score =endpointDistance +midpointDistance;

                if (score > maxDistance * 3f)
                    continue;

                if (score < bestScore){
                    bestScore = score;
                    bestIndex = j;
                }
            }

            if (bestIndex == -1)
                continue;

            var bBest = edges[bestIndex];

            Undo.RecordObject(a.surface, "Link External Neighbors");
            Undo.RecordObject(bBest.surface, "Link External Neighbors");

            a.surface.faces[a.face].externalNeighborSurface[a.edge] =bBest.surface;

            a.surface.faces[a.face].externalNeighborFace[a.edge] =bBest.face;

            bBest.surface.faces[bBest.face].externalNeighborSurface[bBest.edge] =a.surface;

            bBest.surface.faces[bBest.face].externalNeighborFace[bBest.edge] =a.face;

            EditorUtility.SetDirty(a.surface);
            EditorUtility.SetDirty(bBest.surface);

            used.Add(i);
            used.Add(bestIndex);

            linkCount++;
        }

        Debug.Log($"Linked {linkCount} external edges across {surfaces.Length} parts.");
    }

    private static void Build(ClimbableSurface surface)
    {
        var meshFilter = surface.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError($"[ClimbableSurfaceBuilder] {surface.name} has no MeshFilter/mesh to build from.", surface);
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (!mesh.isReadable)
        {
            Debug.LogError(
                $"[ClimbableSurfaceBuilder] Mesh '{mesh.name}' is not Read/Write Enabled. " +
                "Select the mesh asset and enable Read/Write in the Inspector.", surface);
            return;
        }

        Vector3[] positions = mesh.vertices;
        int[] triangles = mesh.triangles;
        int triCount = triangles.Length / 3;

        // ---- a. Read raw triangles + per-triangle normal ----
        var triNormals = new Vector3[triCount];
        var triVerts = new Vector3[triCount][]; // 3 positions per triangle, in winding order
        for (int t = 0; t < triCount; t++)
        {
            int i0 = triangles[t * 3 + 0];
            int i1 = triangles[t * 3 + 1];
            int i2 = triangles[t * 3 + 2];

            Vector3 p0 = positions[i0];
            Vector3 p1 = positions[i1];
            Vector3 p2 = positions[i2];

            triVerts[t] = new[] { p0, p1, p2 };
            triNormals[t] = Vector3.Cross(p1 - p0, p2 - p0).normalized;
        }

        var triToCluster = new int[triCount];
        for (int i = 0; i < triCount; i++) triToCluster[i] = -1;

        var edgeToTris = BuildEdgeToTriangleMap(triVerts);

        var clusters = new List<List<int>>();
        for (int t = 0; t < triCount; t++)
        {
            if (triToCluster[t] != -1) continue;

            var clusterIndex = clusters.Count;
            var cluster = new List<int> { t };
            triToCluster[t] = clusterIndex;

            // BFS across shared edges, only following into coplanar neighbors
            var queue = new Queue<int>();
            queue.Enqueue(t);
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                foreach (int neighbor in GetEdgeNeighbors(cur, triVerts, edgeToTris))
                {
                    if (triToCluster[neighbor] != -1) continue;
                    if (Vector3.Dot(triNormals[cur], triNormals[neighbor]) < NormalDotThreshold) continue;

                    triToCluster[neighbor] = clusterIndex;
                    cluster.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            clusters.Add(cluster);
        }

        // ---- c. Merge each cluster's triangles into a single boundary polygon ----
        var faces = new List<ClimbableSurface.Face>();
        var faceKeyEdges = new List<List<(Vector3 a, Vector3 b)>>(); // boundary edges per face, for neighbor pass

        foreach (var cluster in clusters)
        {
            Vector3 normal = triNormals[cluster[0]];
            List<(Vector3 a, Vector3 b)> boundary = ExtractBoundaryLoop(cluster, triVerts);

            if (boundary == null || boundary.Count < 3)
            {
                Debug.LogWarning($"[ClimbableSurfaceBuilder] Could not stitch a clean boundary for a cluster on '{surface.name}'; skipping.", surface);
                continue;
            }

            Vector3[] verts = boundary.Select(e => e.a).ToArray();
            faces.Add(new ClimbableSurface.Face(normal, verts, new int[verts.Length])); // neighborIndices filled below
            faceKeyEdges.Add(boundary);

            // record which face every triangle in this cluster maps to
            foreach (int t in cluster)
                triToCluster[t] = faces.Count - 1; // repurpose to final face index
        }

        // ---- d. Build neighbours via position-based edge matching ----
        // Key = unordered pair of rounded positions, so both faces sharing that boundary edge find each other
        var edgeOwners = new Dictionary<(Vector3, Vector3), List<(int faceIdx, int edgeIdx)>>();

        for (int f = 0; f < faces.Count; f++)
        {
            var verts = faces[f].vertices;
            for (int e = 0; e < verts.Length; e++)
            {
                Vector3 a = Snap(verts[e]);
                Vector3 b = Snap(verts[(e + 1) % verts.Length]);
                var key = EdgeKey(a, b);

                if (!edgeOwners.TryGetValue(key, out var list))
                {
                    list = new List<(int, int)>();
                    edgeOwners[key] = list;
                }
                list.Add((f, e));
            }
        }

        foreach (var kvp in edgeOwners)
        {
            var owners = kvp.Value;
            if (owners.Count == 2)
            {
                var (faceA, edgeA) = owners[0];
                var (faceB, edgeB) = owners[1];
                faces[faceA].neighborIndices[edgeA] = faceB;
                faces[faceB].neighborIndices[edgeB] = faceA;
            }
            else if (owners.Count == 1)
            {
                var (faceA, edgeA) = owners[0];
                faces[faceA].neighborIndices[edgeA] = -1; // open/boundary edge, no neighbor
            }
            else if (owners.Count > 2)
            {
                Debug.LogWarning(
                    $"[ClimbableSurfaceBuilder] Non-manifold edge on '{surface.name}' at local " +
                    $"{kvp.Key.Item1}-{kvp.Key.Item2}: faces [{string.Join(",", owners.Select(o => o.faceIdx))}] " +
                    $"all share it. Only the first two were linked - this pairing may be wrong.", surface);
                var (faceA, edgeA) = owners[0];
                var (faceB, edgeB) = owners[1];
                faces[faceA].neighborIndices[edgeA] = faceB;
                faces[faceB].neighborIndices[edgeB] = faceA;
            }
        }

        // ---- e. Store into the component ----
        var triangleToFace = new int[triCount];
        for (int t = 0; t < triCount; t++)
            triangleToFace[t] = triToCluster[t];

        Undo.RecordObject(surface, "Build Face Graph");
        surface.faces = faces.ToArray();
        surface.triangleToFace = triangleToFace;
        EditorUtility.SetDirty(surface);

        Debug.Log($"[ClimbableSurfaceBuilder] Built {faces.Count} faces from {triCount} triangles on '{surface.name}'.", surface);
    }

    // ---------- helpers ----------

    private static Vector3 Snap(Vector3 v)
    {
        float inv = 1f / PositionEpsilon;
        return new Vector3(
            Mathf.Round(v.x * inv) / inv,
            Mathf.Round(v.y * inv) / inv,
            Mathf.Round(v.z * inv) / inv);
    }

    private static (Vector3, Vector3) EdgeKey(Vector3 a, Vector3 b)
    {
        // unordered: sort so (a,b) and (b,a) produce the same key
        if (CompareVec(a, b) <= 0) return (a, b);
        return (b, a);
    }

    private static int CompareVec(Vector3 a, Vector3 b)
    {
        int c = a.x.CompareTo(b.x);
        if (c != 0) return c;
        c = a.y.CompareTo(b.y);
        if (c != 0) return c;
        return a.z.CompareTo(b.z);
    }

    private static Dictionary<(Vector3, Vector3), List<int>> BuildEdgeToTriangleMap(Vector3[][] triVerts)
    {
        var map = new Dictionary<(Vector3, Vector3), List<int>>();
        for (int t = 0; t < triVerts.Length; t++)
        {
            var v = triVerts[t];
            for (int e = 0; e < 3; e++)
            {
                Vector3 a = Snap(v[e]);
                Vector3 b = Snap(v[(e + 1) % 3]);
                var key = EdgeKey(a, b);
                if (!map.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    map[key] = list;
                }
                list.Add(t);
            }
        }
        return map;
    }

    private static IEnumerable<int> GetEdgeNeighbors(int tri, Vector3[][] triVerts, Dictionary<(Vector3, Vector3), List<int>> edgeToTris)
    {
        var v = triVerts[tri];
        for (int e = 0; e < 3; e++)
        {
            Vector3 a = Snap(v[e]);
            Vector3 b = Snap(v[(e + 1) % 3]);
            var key = EdgeKey(a, b);
            if (!edgeToTris.TryGetValue(key, out var list)) continue;
            foreach (int other in list)
                if (other != tri) yield return other;
        }
    }

    private static List<(Vector3 a, Vector3 b)> ExtractBoundaryLoop(List<int> cluster, Vector3[][] triVerts)
    {
        var edgeCount = new Dictionary<(Vector3, Vector3), int>();
        var directedBoundary = new Dictionary<Vector3, Vector3>(); 

        foreach (int t in cluster)
        {
            var v = triVerts[t];
            for (int e = 0; e < 3; e++)
            {
                Vector3 a = Snap(v[e]);
                Vector3 b = Snap(v[(e + 1) % 3]);
                var key = EdgeKey(a, b);
                edgeCount[key] = edgeCount.TryGetValue(key, out var c) ? c + 1 : 1;
            }
        }

        foreach (int t in cluster)
        {
            var v = triVerts[t];
            for (int e = 0; e < 3; e++)
            {
                Vector3 a = Snap(v[e]);
                Vector3 b = Snap(v[(e + 1) % 3]);
                var key = EdgeKey(a, b);
                if (edgeCount[key] == 1)
                {
                    
                    directedBoundary[a] = b;
                }
            }
        }

        if (directedBoundary.Count == 0) return null;

        var loop = new List<(Vector3, Vector3)>();
        Vector3 first = directedBoundary.Keys.First();
        Vector3 current = first;
        var visited = new HashSet<Vector3>();

        int guard = directedBoundary.Count + 1;
        while (guard-- > 0)
        {
            if (!directedBoundary.TryGetValue(current, out Vector3 next))
                return null; 

            loop.Add((current, next));
            visited.Add(current);

            if (next == first) break;
            current = next;

            if (visited.Contains(current))
                return null; 
        }

        if (loop.Count != directedBoundary.Count) return null; 

        return loop;
    }
}
#endif