using UnityEngine;

[ExecuteAlways]
public sealed class BA1VFXSetup : MonoBehaviour
{
    private const string SourceObjectName = "Circle";
    private const string TargetObjectName = "BA1vfx";

    [Header("Slash silhouette")]
    [SerializeField] private float length = 2.6f;
    [SerializeField] private float frontWidth = 1.5f;
    [SerializeField] private float backWidth = 0.12f;
    [SerializeField] private int resolution = 12;

    private void OnEnable()
    {
        EnsureBA1VFX();
    }

    private void OnValidate()
    {
        EnsureBA1VFX();
    }

    [ContextMenu("Create BA1vfx")]
    public void EnsureBA1VFX()
    {
        GameObject source = GameObject.Find(SourceObjectName);
        if (source == null)
        {
            Debug.LogWarning($"[BA1VFXSetup] Source object '{SourceObjectName}' was not found in the scene.");
            return;
        }

        GameObject target = GameObject.Find(TargetObjectName);
        if (target == null)
        {
            target = new GameObject(TargetObjectName);
        }

        target.transform.SetParent(source.transform.parent, false);
        target.transform.localPosition = source.transform.localPosition;
        target.transform.localRotation = source.transform.localRotation;
        target.transform.localScale = source.transform.localScale;

        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        MeshRenderer targetRenderer = target.GetComponent<MeshRenderer>();
        if (targetRenderer == null)
            targetRenderer = target.AddComponent<MeshRenderer>();

        Material materialToUse = sourceRenderer != null && sourceRenderer.sharedMaterial != null
            ? sourceRenderer.sharedMaterial
            : GetSlashMaterial();
        targetRenderer.sharedMaterial = materialToUse;
        targetRenderer.shadowCastingMode = sourceRenderer != null ? sourceRenderer.shadowCastingMode : UnityEngine.Rendering.ShadowCastingMode.On;
        targetRenderer.receiveShadows = sourceRenderer != null ? sourceRenderer.receiveShadows : true;

        SlashMeshShapeFix shapeFix = target.GetComponent<SlashMeshShapeFix>();
        if (shapeFix == null)
            shapeFix = target.AddComponent<SlashMeshShapeFix>();

        shapeFix.Configure(length, frontWidth, backWidth, resolution);
        shapeFix.ApplyShape();

        if (materialToUse != null)
        {
            materialToUse.SetFloat("_Width", 1.2f);
            materialToUse.SetFloat("_Height", 0.18f);
            materialToUse.SetFloat("_Radius", 0.02f);
            materialToUse.SetFloat("_CoreSize", 0.85f);
            materialToUse.SetFloat("_Scroll", 0.09f);
            materialToUse.SetFloat("_Distortion", 0.32f);
            materialToUse.SetFloat("_DistortionScale", 15f);
        }

        target.transform.hideFlags = HideFlags.None;
        target.hideFlags = HideFlags.None;

        GameObject slashObj = GameObject.Find("Slash");
        if (slashObj != null)
        {
            var popup = slashObj.GetComponent<SlashHalfCirclePopup>();
            if (popup == null)
                popup = slashObj.AddComponent<SlashHalfCirclePopup>();

            popup.SetTargetObject(target);
            popup.SetParticleSystemFromChildren();
        }
    }

    private static Material GetSlashMaterial()
    {
        Shader slashShader = Shader.Find("Shader Graphs/Slash");
        if (slashShader == null)
            slashShader = Shader.Find("TA VFX/Slash Glow");
        return slashShader == null ? null : new Material(slashShader)
        {
            name = "BA1vfx Slash Material"
        };
    }
}
