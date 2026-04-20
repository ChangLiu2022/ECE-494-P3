using System.Collections.Generic;
using UnityEngine;


// spawned at a sound origin, floods outward cell-by-cell using BFS
// stops for walls
// visualized as a flat mesh on the floor
// destroys itself once the wave reaches max_radius
public class NoiseWave : MonoBehaviour
{
    // units per second the wave grows
    [SerializeField] private float expand_speed = 8f;
    // uses a grid of square cells so we can get wall collision easily
    [SerializeField] private float cell_size = 0.2f;
    [SerializeField] private LayerMask wall_mask;

    // how far the wave can ever reach
    private float max_radius;
    // the waves current size as it expands
    private float current_radius = 0f;
    // world position where the sound happened
    private Vector3 origin;
    // lets us know if we need to rebuuild the mesh this frame
    private bool needs_rebuild = false;

    private Mesh mesh;
    
    private List<Vector3> vert_list = new List<Vector3>();
    private List<int> tri_list = new List<int>();

    // cells that have been reached by the wave so far
    private HashSet<Vector2Int> reached = new HashSet<Vector2Int>();

    // cells queued to be reached, with their distance from the origin cell
    // sorted by distance so BFS expands outward
    private Queue<(Vector2Int cell, float dist)> pending = 
        new Queue<(Vector2Int, float)>();

    // the 4 directions we are allowed to expand in
    private static readonly Vector2Int[] DIRECTIONS = {
        new Vector2Int(1, 0), 
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1), 
        new Vector2Int(0, -1)
    };


    public void Initialize(Vector3 world_origin, float radius)
    {
        origin = world_origin;
        max_radius = radius;

        // anchor the sound of the source to a world position
        // so it doesnt move
        transform.position = world_origin;
    }


    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // start at the origin cell, distance 0
        pending.Enqueue((ToCell(origin), 0f));
    }


    private void Update()
    {
        // grow the wave
        current_radius += expand_speed * Time.deltaTime;

        // unlock new BFS cells that are within current radius
        // and raycast for walls
        Expand();

        // only rebuild if something new was added this frame
        if (needs_rebuild)
        {
            RebuildMesh();
            needs_rebuild = false;
        }

        // destroy once it reaches max radius
        if (current_radius >= max_radius)
            Destroy(gameObject);
    }


    private void Expand()
    {
        // proicess cells in order of distance from origin
        while (pending.Count > 0)
        {
            // do not remove the cell
            var (cell, distance) = pending.Peek();

            // if the distance of this cell is greater than the wave
            // let the wave grow more
            if (distance > current_radius)
                break;

            // otherwise, consume the cell
            pending.Dequeue();

            // means we already have been to this cell from some other path
            // skip it
            if (reached.Contains(cell)) 
                continue;

            // cell was vistied, mesh was updated and needs rebuilding
            reached.Add(cell);

            needs_rebuild = true;

            foreach (var direction in DIRECTIONS)
            {
                // where the next cell is in the direction we are
                // expanding
                Vector2Int neighbor = cell + direction;

                // how big that cell is
                float neighbor_dist = distance + cell_size;

                // already were here
                if (reached.Contains(neighbor)) 
                    continue;

                // ouut of the range of our wave
                if (neighbor_dist > max_radius) 
                    continue;

                Vector3 step = 
                    (ToWorld(neighbor) - ToWorld(cell)).normalized;

                // raycast from inside the current cell toward the neighbor
                // offset aboids starting the ray insde a wall collider
                if (Physics.Raycast(
                    ToWorld(cell) + step * 0.01f, 
                    step,
                    cell_size + 0.05f, 
                    wall_mask))
                {
                    continue;
                }

                pending.Enqueue((neighbor, neighbor_dist));
            }
        }
    }


    private void RebuildMesh()
    {
        float half = cell_size * 0.5f;

        vert_list.Clear();
        tri_list.Clear();

        foreach (var cell in reached)
        {
            Vector3 c = ToWorld(cell) - origin;

            int v = vert_list.Count;

            vert_list.Add(new Vector3(c.x - half, 0, c.z - half)); // v+0 bottom left
            vert_list.Add(new Vector3(c.x + half, 0, c.z - half)); // v+1 bottom right
            vert_list.Add(new Vector3(c.x + half, 0, c.z + half)); // v+2 top right
            vert_list.Add(new Vector3(c.x - half, 0, c.z + half)); // v+3 top left

            // tri 1
            tri_list.Add(v + 0); tri_list.Add(v + 2); tri_list.Add(v + 1);
            // tri 2
            tri_list.Add(v + 0); tri_list.Add(v + 3); tri_list.Add(v + 2);
        }

        mesh.Clear();
        mesh.SetVertices(vert_list);
        mesh.SetTriangles(tri_list, 0);
        mesh.RecalculateNormals();
    }


    // mapping from world space to grid cell coordinates
    private Vector2Int ToCell(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x / cell_size);
        int z = Mathf.RoundToInt(pos.z / cell_size);

        return new Vector2Int(x, z);
    }

    // mapping from grid cell coordinates to world space
    private Vector3 ToWorld(Vector2Int cell)
    {
        float x = cell.x * cell_size;
        float z = cell.y * cell_size;

        return new Vector3(x, origin.y, z);
    }
}


// credits
// https://pathfindinginunity.blogspot.com/2016/10/breadth-first-search-on-grid.html