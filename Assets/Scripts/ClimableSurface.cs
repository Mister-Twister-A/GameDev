using UnityEngine;

/// <summary>
/// Pure data holder for a mesh's merged face-adjacency graph.
/// Populated by ClimbableSurfaceBuilder (editor) or FaceGraphBuilder (runtime).
/// No logic lives here — the player controller reads this to walk across faces.
/// </summary>
public class ClimbableSurface : MonoBehaviour
{
    [System.Serializable]
    public class Face
    {
        public Vector3 normal;             
        public Vector3[] vertices;         
        public int[] neighborIndices;      

        public ClimbableSurface[] externalNeighborSurface; 
        public int[] externalNeighborFace;                 

        public Face(Vector3 normal, Vector3[] vertices, int[] neighborIndices)
        {
            this.normal = normal;
            this.vertices = vertices;
            this.neighborIndices = neighborIndices;

            externalNeighborSurface = new ClimbableSurface[vertices.Length];
            externalNeighborFace = new int[vertices.Length];
            for (int i = 0; i < externalNeighborFace.Length; i++) externalNeighborFace[i] = -1;
        }
    }

    [Tooltip("Merged faces of the mesh (e.g. 6 for a cube, regardless of triangle count).")]
    public Face[] faces = System.Array.Empty<Face>();

    [Tooltip("Maps mesh.triangles triangle index -> index into 'faces'. Length = triangleCount.")]
    public int[] triangleToFace = System.Array.Empty<int>();
    public int GetFaceFromTriangle(int triangleIndex)
    {
        if (triangleToFace == null || triangleIndex < 0 || triangleIndex >= triangleToFace.Length)
            return -1;
        return triangleToFace[triangleIndex];
    }

    public bool IsValid()
    {
        if (faces == null || faces.Length == 0) return false;
        foreach (var f in faces)
        {
            if (f.vertices == null || f.neighborIndices == null) return false;
            if (f.vertices.Length != f.neighborIndices.Length) return false;
            if (f.vertices.Length < 3) return false;
        }
        return true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (faces == null) return;

        var centroids = new Vector3[faces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            if (face.vertices == null || face.vertices.Length == 0) continue;

            Vector3 c = Vector3.zero;
            foreach (var vtx in face.vertices) c += transform.TransformPoint(vtx);
            c /= face.vertices.Length;
            centroids[i] = c;

            UnityEditor.Handles.Label(c, i.ToString());
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(c, 0.03f);

            Gizmos.color = Color.green;
            Vector3 worldNormal = transform.TransformDirection(face.normal);
            Gizmos.DrawLine(c, c + worldNormal * 0.2f);
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < faces.Length; i++)
        {
            if (faces[i].neighborIndices == null) continue;
            foreach (int n in faces[i].neighborIndices)
            {
                if (n < 0 || n >= faces.Length) continue;
                Gizmos.DrawLine(centroids[i], centroids[n]);
            }
        }

        Gizmos.color = Color.magenta;
        for (int i = 0; i < faces.Length; i++)
        {
            var extSurfaces = faces[i].externalNeighborSurface;
            var extFaces = faces[i].externalNeighborFace;
            if (extSurfaces == null) continue;

            for (int e = 0; e < extSurfaces.Length; e++)
            {
                var other = extSurfaces[e];
                if (other == null || other.faces == null) continue;
                int of = extFaces[e];
                if (of < 0 || of >= other.faces.Length) continue;

                Vector3 oc = Vector3.zero;
                foreach (var vtx in other.faces[of].vertices) oc += other.transform.TransformPoint(vtx);
                oc /= other.faces[of].vertices.Length;

                Gizmos.DrawLine(centroids[i], oc);
            }
        }
    }
#endif
}