using UnityEngine;
using static GameEvents;

public class VisionConeMesh : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Must match what GuardVisionCone has.")]
    // what we will use as the default guard vision
    [SerializeField] private float default_detect_radius = 5f;
    [SerializeField] private float default_view_angle = 90f;
    // how 'fine' we want the vision cone to display
    // mot much gained from going higher
    [SerializeField] private int segments = 20;
    [Tooltip("For now, walls, but maybe player too?")]
    [SerializeField] private LayerMask obstruction_mask;

    [Header("Vision Materials")]
    [SerializeField] private Material default_mat;
    [SerializeField] private Material flashlight_mat;
    [SerializeField] private Material chase_mat;

    private GuardController controller;

    private Mesh mesh;
    private MeshRenderer view_renderer;
    private Vector3[] verticies;
    private int[] triangles;
    // used to actually apply the guard vision fov and distance
    private float view_angle;
    private float detect_radius;
    private bool lights_out;


    private void OnEnable()
    {
        EventBus.Subscribe<LightsOutEvent>(OnLightsOutEvent);
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<LightsOutEvent>(OnLightsOutEvent);
    }


    private void Start()
    {
        mesh = new Mesh();
        controller = GetComponentInParent<GuardController>();
        view_renderer = GetComponent<MeshRenderer>();
        GetComponent<MeshFilter>().mesh = mesh;

        view_angle = default_view_angle;
        detect_radius = default_detect_radius;

        // only need verticies and triangles to compose the mesh
        // no uv coords because I am applying a flat material
        // for verticies, segments + 2 because 1 is for the starting
        // center point of the guard, and segments + 1 is to draw the
        // out the arc. If the arc is 20 segs long, you need 21 segs
        // to contain those 20 segs, so segments + 2.
        verticies = new Vector3[segments + 2];
        // each triangle has 3 segments that connect 3 verticies together
        triangles = new int[segments * 3];

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
    }


    // FOR TESTING, press L and this simulates the lights turning off
    // and the guards switching to flashlight mode
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            EventBus.Publish(new LightsOutEvent());
            Debug.Log("LightsOutEvent published");
        }
    }


    private void LateUpdate()
    {
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

        // this is where the guard's eyes technically are
        verticies[0] = Vector3.zero;

        // saves where each vertex is placed on the arc
        for (int i = 0; i <= segments; i++)
        {
            // start from -45 in this case and work up to 45
            float angle = -half_view + point_distance * i;
            // we want this to be casted straight out the guards eyes
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            // by default cast the detection cone out the full distance
            float detect_distance = detect_radius;

            // need to rotate the local direction
            // to the guards current rotation
            Vector3 world_direction = 
                transform.TransformDirection(direction);

            if (Physics.Raycast(
                transform.position,
                world_direction,
                out RaycastHit hit,
                detect_radius,
                obstruction_mask))
            {
                detect_distance = hit.distance;
            }
            // don't overwrite center point, but set where the point is
            // the detect distance in the direction we solved for
            verticies[i + 1] = direction * detect_distance;
        }

        mesh.Clear();
        mesh.vertices = verticies;
        mesh.triangles = triangles;

        // change the material of the vision cone's mesh renderer depending
        // on if we are chasing or if the lights turned out
        if (controller.is_chasing == true)
        {
            view_renderer.material = chase_mat;
            return;
        }

        // if we are chasing, the flashlight_mat will always be overwritten
        // by the the chase_mat if the guard is chasing
        else if (lights_out == true)
        {
            view_renderer.material = flashlight_mat;
            return;
        }
    }


    // used to change the guards fov and distance based on the lights
    private void OnLightsOutEvent(LightsOutEvent e)
    {
        float lights_off_view_angle = default_view_angle / 2;
        float lights_off_detect_radius = default_detect_radius / 2;

        view_angle = lights_off_view_angle;
        detect_radius = lights_off_detect_radius;

        lights_out = true;
    }


    // getters for guardvisioncone to always have latest fov vals
    public float GetDetectRadius()
    {
        return detect_radius;
    }


    public float GetViewAngle()
    {
        return view_angle;
    }
}


// credits
// https://youtu.be/gmuHI_wsOgI