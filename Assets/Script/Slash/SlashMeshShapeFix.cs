using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SlashMeshShapeFix : MonoBehaviour
{
    [Header("Slash shape")]
    [SerializeField] private float length = 2.2f;
    [SerializeField] private float frontWidth = 1.35f;
    [SerializeField] private float backWidth = 0.12f;
    [SerializeField] private int resolution = 10;
    [SerializeField] private bool autoApply = true;

    [Header("Material")]
    [SerializeField] private Material materialOverride;
    [SerializeField] private Shader shaderOverride;
    [SerializeField] private bool applyMaterial = true;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Reset()
    {
        ApplyShape();
    }

    private void OnValidate()
    {
        if (autoApply)
            ApplyShape();
    }

    private void Awake()
    {
        if (autoApply)
            ApplyShape();
    }

    private void OnEnable()
    {
        if (autoApply)
            ApplyShape();
    }

    public void Configure(float newLength, float newFrontWidth, float newBackWidth, int newResolution)
    {
        length = newLength;
        frontWidth = newFrontWidth;
        backWidth = newBackWidth;
        resolution = Mathf.Max(4, newResolution);
        ApplyShape();
    }

    [ContextMenu("Apply Slash Shape")]
    public void ApplyShape()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = BuildSlashMesh();
        meshFilter.sharedMesh = mesh;

        if (applyMaterial)
        {
            Material targetMaterial = materialOverride;
            if (targetMaterial == null)
            {
                if (shaderOverride != null)
                    targetMaterial = new Material(shaderOverride)
                    {
                        name = "Slash Fixed Runtime"
                    };
                else
                {
                    Shader autoShader = Shader.Find("TA VFX/Slash Glow");
                    if (autoShader != null)
                        targetMaterial = new Material(autoShader)
                        {
                            name = "Slash Fixed Runtime"
                        };
                }
            }

            if (targetMaterial != null)
            {
                meshRenderer.sharedMaterial = targetMaterial;
                targetMaterial.SetFloat("_GlowWidth", 0.2f);
                targetMaterial.SetFloat("_Intensity", 7.5f);
                targetMaterial.SetFloat("_Opacity", 1f);
                targetMaterial.SetFloat("_Thickness", 0.85f);
                targetMaterial.SetFloat("_TipLength", 0.14f);
                targetMaterial.SetFloat("_TipSoftness", 0.022f);
                targetMaterial.SetFloat("_TrailOpacity", 0.18f);
                targetMaterial.SetFloat("_EdgeGlow", 2.4f);
            }
        }
    }

    private Mesh BuildSlashMesh()
    {
        int steps = Mathf.Max(4, resolution);
        int upperLowerCount = steps + 1;
        Vector3[] vertices = new Vector3[upperLowerCount * 2];
        int[] triangles = new int[steps * 6];

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = Mathf.Lerp(-1f, 1f, t) * length;
            float width = CalculateWidth(t);

            int upperIndex = i;
            int lowerIndex = i + upperLowerCount;

            vertices[upperIndex] = new Vector3(x, width * 0.5f, 0f);
            vertices[lowerIndex] = new Vector3(x, -width * 0.5f, 0f);
        }

        int triangleIndex = 0;
        for (int i = 0; i < steps; i++)
        {
            int upperA = i;
            int upperB = i + 1;
            int lowerA = i + upperLowerCount;
            int lowerB = i + 1 + upperLowerCount;

            triangles[triangleIndex++] = upperA;
            triangles[triangleIndex++] = upperB;
            triangles[triangleIndex++] = lowerA;

            triangles[triangleIndex++] = lowerA;
            triangles[triangleIndex++] = upperB;
            triangles[triangleIndex++] = lowerB;
        }

        Mesh mesh = new Mesh
        {
            name = "Slash Flat Wedge Mesh"
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < upperLowerCount; i++)
        {
            float u = i / (float)steps;
            uv[i] = new Vector2(u, 1f);
            uv[i + upperLowerCount] = new Vector2(u, 0f);
        }
        mesh.uv = uv;

        return mesh;
    }

    private float CalculateWidth(float t)
    {
        float width;

        if (t < 0.2f)
        {
            width = Mathf.Lerp(backWidth, 0.28f, t / 0.2f);
        }
        else if (t < 0.72f)
        {
            width = Mathf.Lerp(0.28f, 0.9f, (t - 0.2f) / (0.72f - 0.2f));
        }
        else
        {
            width = Mathf.Lerp(0.9f, frontWidth, (t - 0.72f) / (1f - 0.72f));
        }

        return Mathf.Max(width, backWidth * 0.5f);
    }
}
