using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drag-and-drop UI slash VFX spawner. The host and spawned effect scales are
/// never modified; motion is expressed only through position and rotation.
/// </summary>
// Runtime-only native helper objects are initialized from Awake/lazily.
// Shader controls use stable reference names shared with Slashes.shadergraph.
[DisallowMultipleComponent]
public sealed class UIVFXSlash : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string displayName = "UI Slash";

    [Header("VFX Source")]
    [SerializeField] private GameObject slashPrefab;
    [Tooltip("World/UI spawn point. The effect is instantiated here.")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private bool reuseInstance = true;

    [Header("Transform (Scale Is Never Modified)")]
    [SerializeField] private Vector3 rotation;
    [Tooltip("Extra offset from the UI top edge.")]
    [SerializeField] private Vector2 topOffset;
    [Tooltip("Extra offset from the UI bottom edge.")]
    [SerializeField] private Vector2 bottomOffset;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float duration = 0.35f;
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField] private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool hideWhenFinished = true;

    [Header("Dissolve")]
    [Tooltip("Slashes.shadergraph uses _Slider for its UV-Y sweep/dissolve.")]
    [SerializeField] private string dissolveProperty = "_Slider";
    [SerializeField] private float dissolveStart = -1f;
    [SerializeField] private float dissolveEnd = 1f;
    [SerializeField] private AnimationCurve dissolveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Custom1.y used by Slash.shadergraph.")]
    [SerializeField, Min(0f)] private float colorIntensity = 4f;
    [Tooltip("Custom1.z used by Slash.shadergraph.")]
    [SerializeField] private float voronoiSpeed = 1f;
    [Tooltip("Custom1.w used by Slash.shadergraph.")]
    [SerializeField, Min(0.01f)] private float voronoiScale = 7f;

    private readonly List<ParticleSystem> particles = new List<ParticleSystem>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Graphic> graphics = new List<Graphic>();
    private readonly List<Vector4> customData = new List<Vector4>(1);
    // UnityEngine native objects must not be created in a MonoBehaviour field
    // initializer because Unity also invokes it during deserialization.
    private MaterialPropertyBlock propertyBlock;

    private GameObject instance;
    private RectTransform instanceRect;
    private Coroutine playRoutine;
    private Material[] uiMaterialInstances = Array.Empty<Material>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public bool IsPlaying => playRoutine != null;
    public event Action<UIVFXSlash> Completed;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    [ContextMenu("Play Slash")]
    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        StopInternal(false);
        if (!PrepareInstance())
            return;

        instance.SetActive(true);
        playRoutine = StartCoroutine(AnimateSlash());
    }

    public void Stop()
    {
        StopInternal(true);
    }

    public void SetRotation(Vector3 eulerRotation)
    {
        rotation = eulerRotation;
        if (instance != null)
            instance.transform.localRotation = Quaternion.Euler(rotation);
    }

    private bool PrepareInstance()
    {
        if (slashPrefab == null)
        {
            Debug.LogWarning($"{nameof(UIVFXSlash)} on '{name}' has no Slash Prefab.", this);
            return false;
        }

        Transform parent = spawnParent != null ? spawnParent : transform;
        if (parent == null)
        {
            Debug.LogWarning($"{nameof(UIVFXSlash)} requires a Spawn Parent.", this);
            return false;
        }

        if (instance == null)
        {
            instance = Instantiate(slashPrefab, parent, false);
            instance.name = slashPrefab.name + " (UI VFX)";
            instanceRect = instance.GetComponent<RectTransform>();
            CacheTargets();
        }

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(rotation);
        return true;
    }

    private void CacheTargets()
    {
        particles.Clear();
        renderers.Clear();
        graphics.Clear();
        instance.GetComponentsInChildren(true, particles);
        instance.GetComponentsInChildren(true, renderers);
        instance.GetComponentsInChildren(true, graphics);

        var materials = new List<Material>();
        foreach (Graphic graphic in graphics)
        {
            if (graphic.material == null)
                continue;
            Material materialInstance = new Material(graphic.material)
            {
                name = graphic.material.name + " (UI Slash Runtime)"
            };
            graphic.material = materialInstance;
            materials.Add(materialInstance);
        }
        uiMaterialInstances = materials.ToArray();
    }

    private IEnumerator AnimateSlash()
    {
        if (startDelay > 0f)
            yield return Wait(startDelay);

        RestartParticles();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float positionT = movementCurve.Evaluate(normalizedTime);
            float dissolveT = dissolveCurve.Evaluate(normalizedTime);

            ApplyPosition(positionT);
            ApplyDissolve(Mathf.LerpUnclamped(dissolveStart, dissolveEnd, dissolveT));
            yield return null;
        }

        ApplyPosition(1f);
        ApplyDissolve(dissolveEnd);
        playRoutine = null;

        if (hideWhenFinished && instance != null)
            instance.SetActive(false);
        if (!reuseInstance)
            DestroyInstance();

        Completed?.Invoke(this);
    }

    private void ApplyPosition(float t)
    {
        Transform area = spawnParent != null ? spawnParent : transform;
        if (area == null || instance == null)
            return;

        RectTransform rectArea = area as RectTransform;
        float top = rectArea != null ? rectArea.rect.yMax : 0f;
        float bottom = rectArea != null ? rectArea.rect.yMin : 0f;
        Vector2 from = new Vector2(topOffset.x, top + topOffset.y);
        Vector2 to = new Vector2(bottomOffset.x, bottom + bottomOffset.y);
        Vector2 position = Vector2.LerpUnclamped(from, to, t);

        if (instanceRect != null)
            instanceRect.anchoredPosition = position;
        else
        {
            Vector3 localPosition = instance.transform.localPosition;
            localPosition.x = position.x;
            localPosition.y = position.y;
            instance.transform.localPosition = localPosition;
        }
    }

    private void ApplyDissolve(float value)
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
        int propertyId = Shader.PropertyToID(dissolveProperty);
        foreach (Renderer targetRenderer in renderers)
        {
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(propertyId, value);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        foreach (Material material in uiMaterialInstances)
            if (material != null && material.HasProperty(propertyId))
                material.SetFloat(propertyId, value);

        // Slash.shadergraph receives its controls through Custom1.xyzw / UV1.
        foreach (ParticleSystem particle in particles)
        {
            int count = particle.particleCount;
            if (count == 0)
                continue;
            customData.Clear();
            particle.GetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
            while (customData.Count < count)
                customData.Add(Vector4.zero);
            for (int i = 0; i < count; i++)
                customData[i] = new Vector4(value, colorIntensity, voronoiSpeed, voronoiScale);
            particle.SetCustomParticleData(customData, ParticleSystemCustomData.Custom1);
        }
    }

    private void RestartParticles()
    {
        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }
    }

    private object Wait(float seconds)
    {
        return useUnscaledTime ? new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
    }

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void StopInternal(bool hide)
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        foreach (ParticleSystem particle in particles)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (hide && instance != null)
            instance.SetActive(false);
    }

    private void DestroyInstance()
    {
        foreach (Material material in uiMaterialInstances)
            if (material != null)
                Destroy(material);
        if (instance != null)
            Destroy(instance);
        instance = null;
        instanceRect = null;
        uiMaterialInstances = Array.Empty<Material>();
    }

    private void OnDestroy()
    {
        if (reuseInstance)
            DestroyInstance();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        colorIntensity = Mathf.Max(0f, colorIntensity);
        voronoiScale = Mathf.Max(0.01f, voronoiScale);
        if (instance != null)
            instance.transform.localRotation = Quaternion.Euler(rotation);
    }
}
