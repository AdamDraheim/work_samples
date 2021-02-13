using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WorldMesh : MonoBehaviour
{

    private Vector3[] vertices;
    private Vector2[] uv;
    private int[] triangles;

    private MinMax elevationMinMax;
    private ColorGenerator colorGen;
    private Mesh mesh;

    public GameObject meshObject;

    public int seed;
    [Header("Map Fields")]
    [Range(1, 200)]
    public int width;
    [Range(1, 200)]
    public int height;
    public Material mat;
    public Color color;


    [Header("Noise fields")]
    public Texture2D noise;
    [Range(0, 10)]
    public float noise_scale;

    [Header("River fields")]
    public float depth;
    public float t_precision;
    public float minRiverWidth;
    public float maxRiverWidth;
    public float widthChange;

    [Header("River Smoothing values")]
    public float smoothing_threshold;
    public int smoothIterations;

    [Header("Test print")]
    public string test;
    private Vector2 c1, c2, c3, c4;

    // Start is called before the first frame update

    private void Awake()
    {
        meshObject = new GameObject("World Mesh Object", typeof(MeshRenderer), typeof(MeshFilter));

    }

    void OnValidate()
    {
        elevationMinMax = new MinMax();
        colorGen = new ColorGenerator(mat);

        Random.InitState(seed);
        GenerateStarterMeshValues();
        if(noise != null)
            ApplyNoise();
        GenerateBezierRiver();
        for (int i = 0; i < smoothIterations; i++)
        {
            Smooth(smoothing_threshold);
        }
        UpdateMinMax();
        colorGen.UpdateElevation(elevationMinMax);

        mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshObject.GetComponent<MeshFilter>().mesh = mesh;
        meshObject.GetComponent<MeshRenderer>().sharedMaterial = mat;
        meshObject.GetComponent<MeshRenderer>().sharedMaterial.color = color;

        meshObject.transform.rotation.eulerAngles.Set(-90, 0, 0);

    }

    private void GenerateStarterMeshValues()
    {
        vertices = new Vector3[(width+1) * (height+1)];
        uv = new Vector2[(width + 1) * (height + 1)];
        triangles = new int[(width + 1) * (height + 1) * 6];

        int index = 0;
        for(int y = 0; y < (height + 1); y++)
        {
            for(int x = 0; x < (width + 1); x++) 
            {
                vertices[index] = new Vector3(x, y, 0);
                uv[index] = new Vector2(x, y);
                index++;
            }
        }

        index = 0;
        int startVertex = 0;
        for (int y = 0; y < height; y++)
        {

            for (int x = 0; x < width; x++)
            {
                triangles[index] = startVertex;
                triangles[index + 1] = startVertex + 1;
                triangles[index + 2] = startVertex + (width+1);

                triangles[index + 3] = startVertex + (width+1);
                triangles[index + 4] = startVertex + 1;
                triangles[index + 5] = startVertex + (width+1) + 1;

                index+=6;
                startVertex += 1;
            }
            startVertex += 1;
        }
    }

    private void ApplyNoise()
    {
        int image_height = noise.height;
        int image_width = noise.width;

        float xScale = noise.width / (width + 1.0f);
        float yScale = noise.height / (height + 1.0f);

        for(int x = 0; x < image_width; x++)
        {
            for(int y = 0; y < image_height; y++)
            {
                Color c = noise.GetPixel(x, y);
                Vector3 orig = vertices[(int)(x / xScale) + (width) * (int)(y / yScale)];
                vertices[(int)(x / xScale) + (width) * (int)(y / yScale)] = 
                    new Vector3(orig.x, orig.y, noise_scale * (orig.z * 0.4f) + (c.r * 0.6f));

            }
        }


    }

    public void GenerateBezierRiver()
    {
        int startY = Random.Range(0, height + 1);
        int endY = Random.Range(0, height + 1);

        Vector2 cp1 = new Vector2(0, startY);
        Vector2 cp2 = new Vector2(Random.Range(0, (width + 1) * 3.0f / 4), Random.Range(0, height + 1));
        Vector2 cp3 = new Vector2(Random.Range((width + 1) / 4, (width + 1)), Random.Range(0, height + 1));
        Vector2 cp4 = new Vector2(width+1, endY);

        c1 = cp1;
        c2 = cp2;
        c3 = cp3;
        c4 = cp4;

        float currWidth = (minRiverWidth + maxRiverWidth) / 2.0f;

        for (int tval = 0; tval < t_precision; tval++)
        {
            float t = tval / (1.0f * t_precision);
            Vector2 nextPos = (Mathf.Pow(1 - t, 3) * cp1) + (Mathf.Pow(1 - t, 2) * t * cp2) +
                        (3 * (1 - t) * Mathf.Pow(t, 2) * cp3) + (Mathf.Pow(t, 3) * cp4);


            for(int v = 0; v < vertices.Length; v++)
            {
                if (Vector2.Distance(vertices[v], nextPos) <= currWidth)
                {
                    Vector2 orig = vertices[v];
                    vertices[v] = new Vector3(orig.x, orig.y, -depth);

                }
            }

            currWidth += Random.Range(-widthChange, widthChange);
            currWidth = Mathf.Max(minRiverWidth, Mathf.Min(currWidth, maxRiverWidth + 1));
            

        }

    }

    private void Smooth(float threshold)
    {
        Vector3[] newVerts = new Vector3[vertices.Length];
        int index = 0;
        for (int y = 0; y < (height + 1); y++)
        {
            for (int x = 0; x < (width + 1); x++)
            {

                newVerts[index] = vertices[index];

                float total = newVerts[index].z;
                int totalCount = 1;

                if (x - 1 >= 0)
                {
                    total += newVerts[index - 1].z;
                    totalCount += 1;
                }
                if (x + 1 < width + 1)
                {
                    total += newVerts[index + 1].z;
                    totalCount += 1;
                }
                if (y - 1 >= 0)
                {
                    total += newVerts[index - width].z;
                    totalCount += 1;
                }
                if (y + 1 < height + 1)
                {
                    total += newVerts[index + width].z;
                    totalCount += 1;
                }

                float newZ = total / totalCount;

                if(Mathf.Abs(vertices[index].z - newZ) < threshold)
                {
                    newZ = vertices[index].z;
                }

                newVerts[index] = new Vector3(newVerts[index].x, newVerts[index].y, newZ);

                index++;
            }
        }

        vertices = newVerts;
    }

    private void UpdateMinMax()
    {
        foreach(Vector3 vec in vertices)
        {
            elevationMinMax.AddValue(vec.z);
        }
    }

    private void OnDrawGizmosSelected()
    {

        Vector2 norm_view = new Vector2(0, 0);

        Vector2 start = c1;
        for (int i = 1; i < t_precision; i++)
        {
            float t = i * 1.0f / t_precision;
            Vector2 next = (Mathf.Pow(1 - t, 3) * c1) + (Mathf.Pow(1 - t, 2) * t * c2) +
                        (3 * (1 - t) * Mathf.Pow(t, 2) * c3) + (Mathf.Pow(t, 3) * c4);

            Gizmos.DrawLine(start, next);


            start = next;
        }

    }
}
