using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlashHalfCirclePopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject slashMeshObject;
    [SerializeField] private ParticleSystem particleSystem;

    [Header("Sword slash sweep")]
    [SerializeField, Range(30f, 360f)] private float arc = 360f;
    [SerializeField, Range(0.1f, 5f)] private float radius = 1.2f;
    [SerializeField] private float startAngle = -90f;
    [SerializeField, Min(0f)] private float arcSpeed = 1f;
    [SerializeField] private Vector3 localPosition = Vector3.zero;
    [SerializeField] private Vector3 localRotation = new Vector3(90f, 0f, 0f);

    [Header("Shader Graph motion")]
    [SerializeField] private bool animateShaderOnPlay = true;
    [SerializeField] private float shaderScrollSpeed = 0.09f;

    private void Reset()
    {
        TryResolveReferences();
        ConfigureParticleSystem();
    }

    private void Awake()
    {
        TryResolveReferences();
        ConfigureParticleSystem();
    }

    private void OnValidate()
    {
        TryResolveReferences();
        ConfigureParticleSystem();
    }

    public void SetTargetObject(GameObject target)
    {
        slashMeshObject = target;
        if (slashMeshObject != null)
            slashMeshObject.transform.SetParent(transform, false);
        ConfigureParticleSystem();
    }

    public void SetParticleSystemFromChildren()
    {
        if (particleSystem == null)
            particleSystem = GetComponentInChildren<ParticleSystem>();
        if (particleSystem == null)
        {
            var obj = new GameObject("Slash Burst Particle");
            obj.transform.SetParent(transform, false);
            particleSystem = obj.AddComponent<ParticleSystem>();
        }
        ConfigureParticleSystem();
    }

    [ContextMenu("Play Slash Pop")]
    public void Play()
    {
        if (particleSystem == null)
        {
            Debug.LogWarning("[SlashHalfCirclePopup] No ParticleSystem assigned on Slash object.", this);
            return;
        }

        if (slashMeshObject == null)
        {
            slashMeshObject = GameObject.Find("BA1vfx");
            if (slashMeshObject == null)
            {
                Debug.LogWarning("[SlashHalfCirclePopup] BA1vfx not found. Create it first.", this);
                return;
            }
        }

        slashMeshObject.SetActive(true);
        slashMeshObject.transform.SetParent(transform, false);
        slashMeshObject.transform.localPosition = localPosition;
        slashMeshObject.transform.localRotation = Quaternion.Euler(localRotation);
        slashMeshObject.transform.localScale = Vector3.one;

        var slashRenderer = slashMeshObject.GetComponent<Renderer>();
        if (slashRenderer != null)
            slashRenderer.enabled = false;

        ConfigureParticleSystem();
        particleSystem.transform.localPosition = localPosition;
        particleSystem.transform.localRotation = Quaternion.Euler(localRotation);
        particleSystem.gameObject.SetActive(true);

        SetShaderScroll(animateShaderOnPlay ? shaderScrollSpeed : 0f);
        particleSystem.Clear();
        particleSystem.Play();
    }

    private void TryResolveReferences()
    {
        if (particleSystem == null)
            particleSystem = GetComponentInChildren<ParticleSystem>();
        if (particleSystem == null)
            particleSystem = gameObject.AddComponent<ParticleSystem>();

        if (slashMeshObject == null)
            slashMeshObject = GameObject.Find("BA1vfx");
    }

    private void ConfigureParticleSystem()
    {
        if (particleSystem == null)
            return;

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.arc = arc;
        shape.radius = radius;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;
        shape.arcSpeed = arcSpeed;
        shape.rotation = new Vector3(90f, 0f, startAngle);

        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
            renderer = particleSystem.gameObject.AddComponent<ParticleSystemRenderer>();

        if (slashMeshObject != null)
        {
            var meshFilter = slashMeshObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = meshFilter.sharedMesh;
            }
            else
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            var meshRenderer = slashMeshObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader != meshRenderer.sharedMaterial.shader)
                    renderer.sharedMaterial = new Material(meshRenderer.sharedMaterial)
                    {
                        name = "Slash Particle Runtime Material"
                    };
            }
        }

        particleSystem.transform.localPosition = localPosition;
        particleSystem.transform.localRotation = Quaternion.Euler(localRotation);
    }

    private void SetShaderScroll(float value)
    {
        if (particleSystem == null)
            return;

        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer == null || renderer.sharedMaterial == null)
            return;

        if (renderer.sharedMaterial.HasProperty("_Scroll"))
            renderer.sharedMaterial.SetFloat("_Scroll", value);
    }
}
