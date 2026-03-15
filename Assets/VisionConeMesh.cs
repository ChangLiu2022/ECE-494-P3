using UnityEngine;

public class VisionConeMesh : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Must match what GuardVisionCone has.")]
    [SerializeField] private float detect_radius = 5f;
    [SerializeField] private float view_angle = 90f;
    // how 'fine' we want the vision cone to display
    // mot much gained from going higher
    [SerializeField] private int segments = 20;


    private void Start()
    {
        Mesh mesh = new Mesh();
        // divided in half for both sides
        // '90' --> 45 degrees one way, 45 the other
        float half_view = view_angle / 2f;
        // break the view angle arc into segments to place
        // points. so 90 / 20 = 4.5, so a point is placed every
        // 4.5 degrees on the arc of the vision cone
        //
        // you are essentially making 20 long pizza slices
        // where each pizza's crust segment is formed by two neighboring
        // arc points that then cast the sides of the pizza segments down to
        // the central point where the guard is looking. So 20 smaller pizza
        // slices make up the entire cone.
        float point_distance = view_angle / segments;

        // only need verticies and triangles to compose the mesh
        // no uv coords because I am applying a flat material
        // for verticies, segments + 2 because 1 is for the starting
        // center point of the guard, and segments + 1 is to draw the
        // out the arc. If the arc is 20 segs long, you need 21 segs
        // to contain those 20 segs, so segments + 2.
        Vector3[] verticies = new Vector3[segments + 2];
        // each triangle has 3 segments that connect 3 verticies together
        int[] triangles = new int[segments * 3];

        // this is where the guard's eyes technically are
        verticies[0] = Vector3.zero;

        // saves where each vertex is placed on the arc
        for (int i = 0; i <= segments; i++)
        {
            // start from -45 in this case and work up to 45
            float angle = -half_view + point_distance * i;
            // we want this to be casted straight out the guards eyes
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            // don't overwrite center point, but set where the point is
            // the detect distance in the direction we solved for
            verticies[i + 1] = direction * detect_radius;
        }

        // triangles. this is telling what vertex points
        // the triangle uses to fill in.
        // so on the first iteration, i=0, triangles[0*3] = 0
        // meaning that triangle[0] starts at vertex 0. triangle[1] = 1
        // and  triangle[2] = 2. This gives the 3 verticies for the first
        // triangle to be formed in.
        for (int i = 0; i < segments; i++)
        {
            // the starting vertex point is always 0
            triangles[i * 3] = 0;
            // the rest are incrementally the following vecticies
            // depending on how many we've done
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = verticies;
        mesh.triangles = triangles;

        GetComponent<MeshFilter>().mesh = mesh;
    }
}
