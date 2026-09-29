/* 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SlashModelSpawnerUI : MonoBehaviour
{
    [Header("Daftar Model 3D")]
    [Tooltip("Masukkan semua model slash sesuai urutan tampil, termasuk model dari Slash1.")]
    [SerializeField] private GameObject[] slashModels;
    [SerializeField] private Transform spawnPoint;
    [SerializeField, Min(0)] private int startingIndex;

    [Header("UI Buatan Anda")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [Tooltip("Tombol untuk memainkan ulang serangan model yang sedang aktif.")]
    [SerializeField] private Button replayButton;
    [Tooltip("Isi salah satu: Text lama atau TMP Text.")]
    [SerializeField] private Text circleNameText;
    [SerializeField] private TMP_Text circleNameTMP;
    [Tooltip("Token yang tersedia: {current}, {total}, {name}. Nama diambil dari model yang benar-benar tampil.")]
    [SerializeField] private string textFormat = "{current}/{total} - {name}";

    [Header("Material Slash")]
    [Tooltip("Opsional: material preset. Jika kosong, material dibuat otomatis dari Slash Shader.")]
    [SerializeField] private Material slashMaterial;
    [Tooltip("Drag shader TA VFX/Slash Glow ke sini jika tidak menggunakan Material.")]
    [SerializeField] private Shader slashShader;
    [SerializeField, Range(0.01f, 0.5f)] private float pointedTipLength = 0.18f;
    [SerializeField, Range(0.001f, 0.15f)] private float pointedTipSoftness = 0.025f;

    [Header("Tampilan Real-time")]
    [ColorUsage(true, true)] [SerializeField] private Color coreColor = Color.white;
    [ColorUsage(true, true)] [SerializeField] private Color glowColor = Color.white;
    [SerializeField, Range(0f, 20f)] private float glowIntensity = 6f;
    [SerializeField, Range(0f, 1f)] private float opacity = 1f;
    [Tooltip("Ketebalan visual berdasarkan UV model. 1 = ketebalan asli.")]
    [SerializeField, Range(0.05f, 1f)] private float thickness = 1f;

    [Header("Animasi Particle Slash")]
    [SerializeField] private ParticleSystem slashParticleSystem;
    [SerializeField] private bool createParticleSystemAutomatically = true;
    [SerializeField] private bool playParticleOnSpawn = true;
    [FormerlySerializedAs("particleDuration")]
    [Tooltip("Kecepatan sweep. Nilai besar = lebih cepat.")]
    [SerializeField, Range(0.2f, 5f)] private float sweepSpeed = 1.5f;
    [Tooltip("Aktif: sisi UV kanan/bold muncul lebih dahulu.")]
    [SerializeField] private bool reverseSweep = true;
    [Tooltip("Deteksi otomatis ujung model yang paling tebal sebagai awal sweep.")]
    [SerializeField] private bool autoDetectBoldEnd = true;
    [SerializeField, Range(0.01f, 0.4f)] private float sweepHeadWidth = 0.12f;
    [SerializeField, Range(0f, 1f)] private float fadedTrailOpacity = 0.28f;
    [SerializeField, Range(1, 12)] private int particlesPerFrame = 4;
    [SerializeField, Range(0.005f, 0.3f)] private float particleSize = 0.055f;
    [Tooltip("Jeda setelah slash muncul penuh sebelum mulai menghilang.")]
    [SerializeField, Range(0f, 2f)] private float holdTime = 0.12f;
    [Tooltip("Kecepatan menghilang. Nilai besar = lebih cepat.")]
    [SerializeField, Range(0.2f, 5f)] private float disappearSpeed = 1.8f;
    [SerializeField] private bool hideAfterSweep = true;

    [Header("Shader Graph Particle Streams")]
    [Tooltip("Nilai maksimum Custom1.x. Semakin besar, mesh semakin larut oleh Power.")]
    [SerializeField, Range(1f, 16f)] private float dissolveAmount = 8f;
    [Tooltip("Custom1.z untuk kecepatan animasi Voronoi.")]
    [SerializeField, Range(-10f, 10f)] private float voronoiSpeed = 2f;
    [Tooltip("Custom1.w untuk skala/cell density Voronoi.")]
    [SerializeField, Range(0.1f, 30f)] private float voronoiScale = 8f;

    [Header("Preview Shader Graph (Edit Mode)")]
    [Tooltip("Tampilkan model terpilih menggunakan Particle System dan Custom Vertex Streams tanpa Play Mode.")]
    [SerializeField] private bool previewInEditMode = true;
    [Tooltip("Animasikan dissolve di Scene View. Sweep Speed dapat diubah langsung saat Edit Mode.")]
    [SerializeField] private bool animateEditModePreview = true;
    [Tooltip("Preview nilai Custom1.x. 0 = penuh, nilai besar = semakin dissolve.")]
    [SerializeField, Range(0f, 16f)] private float editModeDissolve;

    [Header("Transform Model")]
    [SerializeField] private Vector3 localPosition = Vector3.zero;
    [SerializeField] private Vector3 localRotation = Vector3.zero;
    [FormerlySerializedAs("localScale")]
    [Tooltip("Ukuran model setiap kali muncul. Gunakan (1, 1, 1) untuk ukuran normal.")]
    [SerializeField] private Vector3 spawnScale = Vector3.one;
    [Tooltip("Circle.007 mempunyai ukuran mesh impor berbeda. Nilai ini hanya mengoreksi model tersebut.")]
    [SerializeField, Range(0.001f, 1f)] private float circle007ScaleMultiplier = 0.1f;

    [Header("Kamera")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Matikan jika posisi kamera sudah Anda atur sendiri.")]
    [SerializeField] private bool autoFrameCamera;
    [SerializeField] private bool applyFixedCameraRotation = true;
    [SerializeField] private Vector3 fixedCameraRotation = new Vector3(45f, 0f, 0f);
    [SerializeField] private Vector3 cameraDirection = new Vector3(0f, 0.15f, -1f);
    [SerializeField, Min(1f)] private float cameraPadding = 1.35f;
    [SerializeField] private Color cameraBackground = new Color(0.67f, 0.74f, 0.86f, 1f);
    [SerializeField] private bool setFullHDOnBuild = true;

    private GameObject spawnedModel;
    private Material runtimeMaterial;
    private Material particleMaterial;
    private Coroutine sweepCoroutine;
    private MeshFilter activeMeshFilter;
    private readonly List<int> sweepVertexCandidates = new List<int>();
    private readonly List<Vector4> particleCustomData = new List<Vector4>(1);
    private readonly ParticleSystem.Particle[] liveParticles = new ParticleSystem.Particle[1];
    private int currentIndex;
    private Vector3 modelBaseScale = Vector3.one;
    private float currentDissolve;

    public int CurrentIndex => currentIndex;
    public GameObject CurrentModel => spawnedModel;
    private float SweepDuration => 1f / Mathf.Max(sweepSpeed, 0.01f);
    private float DisappearDuration => 1f / Mathf.Max(disappearSpeed, 0.01f);

    private void Awake()
    {
        RemoveLegacyGeneratedUI();
        if (spawnPoint == null) spawnPoint = transform;
        if (targetCamera == null) targetCamera = Camera.main;
        HideSceneTemplateMeshes();
        ConfigureCameraQuality();
    }

    private void HideSceneTemplateMeshes()
    {
        // slashModels pada scene ini adalah tujuh GameObject sumber yang tetap
        // berada di Hierarchy. Mereka hanya template; jangan ikut dirender.
        if (slashModels != null)
        {
            foreach (GameObject template in slashModels)
            {
                if (template == null) continue;
                foreach (Renderer templateRenderer in template.GetComponentsInChildren<Renderer>(true))
                    templateRenderer.enabled = false;
            }
        }

        // Controller lama juga masih memiliki MeshRenderer hasil setup awal.
        foreach (Renderer controllerRenderer in GetComponents<Renderer>())
            controllerRenderer.enabled = false;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            PreviewInEditMode();
            return;
        }
        ApplyRuntimeAppearance();
        ApplyModelTransform();
        ApplyFixedCameraRotation();
        ConfigureMeshParticle();
        if (slashParticleSystem != null && slashParticleSystem.particleCount > 0)
        {
            SetParticleStreams(currentDissolve);
            UpdateParticleColor();
        }
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorPreviewUpdate;
        UnityEditor.EditorApplication.update += EditorPreviewUpdate;
#endif
        if (previousButton != null) previousButton.onClick.AddListener(PreviousModel);
        if (nextButton != null) nextButton.onClick.AddListener(NextModel);
        if (replayButton != null) replayButton.onClick.AddListener(ReplayAttack);
    }

    private void Start()
    {
        if (!HasModels())
        {
            Debug.LogWarning("Slash Model Spawner: daftar model masih kosong.", this);
            UpdateUI();
            return;
        }
        currentIndex = Mathf.Clamp(startingIndex, 0, slashModels.Length - 1);
        SpawnCurrentModel();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorPreviewUpdate;
#endif
        if (previousButton != null) previousButton.onClick.RemoveListener(PreviousModel);
        if (nextButton != null) nextButton.onClick.RemoveListener(NextModel);
        if (replayButton != null) replayButton.onClick.RemoveListener(ReplayAttack);
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
            if (particleMaterial != null) Destroy(particleMaterial);
        }
#if UNITY_EDITOR
        else
        {
            if (runtimeMaterial != null) DestroyImmediate(runtimeMaterial);
            if (particleMaterial != null) DestroyImmediate(particleMaterial);
        }
#endif
    }

    public void NextModel()
    {
        if (!HasModels()) return;
        currentIndex = (currentIndex + 1) % slashModels.Length;
        SpawnCurrentModel();
    }

    public void PreviousModel()
    {
        if (!HasModels()) return;
        currentIndex = (currentIndex - 1 + slashModels.Length) % slashModels.Length;
        SpawnCurrentModel();
    }

    public void SelectModel(int index)
    {
        if (!HasModels() || index < 0 || index >= slashModels.Length) return;
        currentIndex = index;
        SpawnCurrentModel();
    }

    private void SpawnCurrentModel()
    {
        if (!HasModels() || slashModels[currentIndex] == null)
        {
            Debug.LogWarning("Model pada index " + currentIndex + " belum di-assign.", this);
            UpdateUI();
            return;
        }

        if (spawnedModel != null) Destroy(spawnedModel);
        spawnedModel = Instantiate(slashModels[currentIndex], spawnPoint);
        spawnedModel.name = slashModels[currentIndex].name;
        modelBaseScale = spawnedModel.transform.localScale;
        ApplyModelTransform();
        activeMeshFilter = spawnedModel.GetComponentInChildren<MeshFilter>(true);
        if (autoDetectBoldEnd) DetectBoldEndDirection();
        ApplySlashMaterial();
        PlaySlashSweep();
        UpdateUI();
        if (autoFrameCamera) FrameCameraToModel();
        ApplyFixedCameraRotation();
    }

    private void UpdateUI()
    {
        if (!HasModels())
        {
            SetNameText("Model belum di-assign");
            return;
        }
        string modelName = spawnedModel != null
            ? spawnedModel.name
            : slashModels[currentIndex] != null ? slashModels[currentIndex].name : "Kosong";
        string label = textFormat
            .Replace("{current}", (currentIndex + 1).ToString())
            .Replace("{total}", slashModels.Length.ToString())
            .Replace("{name}", modelName);
        SetNameText(label);
    }

    private void ApplySlashMaterial()
    {
        if (spawnedModel == null) return;
        if (runtimeMaterial == null)
        {
            if (slashMaterial != null)
            {
                runtimeMaterial = new Material(slashMaterial)
                {
                    name = slashMaterial.name + " (Runtime)"
                };
            }
            else
            {
                Shader selectedShader = slashShader != null
                    ? slashShader
                    : Shader.Find("Shader Graphs/FX/Slash");

                if (selectedShader == null)
                {
                    Debug.LogError("Shader Graph 'Shader Graphs/FX/Slash' tidak ditemukan. Assign Slash.shadergraph di Inspector.", this);
                    return;
                }

                runtimeMaterial = new Material(selectedShader)
                {
                    name = "Slash Glow (Runtime)"
                };
            }
        }

        runtimeMaterial.SetFloat("_TipLength", pointedTipLength);
        runtimeMaterial.SetFloat("_TipSoftness", pointedTipSoftness);
        runtimeMaterial.SetFloat("_SweepReverse", reverseSweep ? 1f : 0f);
        runtimeMaterial.SetFloat("_SweepSoftness", sweepHeadWidth);
        runtimeMaterial.SetFloat("_TrailOpacity", fadedTrailOpacity);
        runtimeMaterial.SetFloat("_UseSweepGradient", 0f);
        ApplyRuntimeAppearance();
        Renderer[] renderers = spawnedModel.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer modelRenderer in renderers) modelRenderer.sharedMaterial = runtimeMaterial;
    }

    private void ApplyRuntimeAppearance()
    {
        if (runtimeMaterial == null) return;
        runtimeMaterial.SetColor("_CoreColor", coreColor);
        runtimeMaterial.SetColor("_GlowColor", glowColor);
        runtimeMaterial.SetFloat("_Intensity", glowIntensity);
        runtimeMaterial.SetFloat("_Opacity", opacity);
        runtimeMaterial.SetFloat("_Thickness", thickness);
        runtimeMaterial.SetFloat("_TipLength", pointedTipLength);
        runtimeMaterial.SetFloat("_TipSoftness", pointedTipSoftness);
        runtimeMaterial.SetFloat("_UseSweepGradient", 0f);
    }

    private void ApplyModelTransform()
    {
        if (spawnedModel == null) return;
        spawnedModel.transform.localPosition = localPosition;
        spawnedModel.transform.localRotation = Quaternion.Euler(localRotation);
        float modelCorrection = spawnedModel.name.StartsWith("Circle.007")
            ? circle007ScaleMultiplier
            : 1f;
        spawnedModel.transform.localScale = Vector3.Scale(modelBaseScale, spawnScale) * modelCorrection;
    }

    private void PreviewInEditMode()
    {
        if (!previewInEditMode || slashModels == null || slashModels.Length == 0) return;
        int previewIndex = Mathf.Clamp(startingIndex, 0, slashModels.Length - 1);
        GameObject previewModel = slashModels[previewIndex];
        if (previewModel == null || slashParticleSystem == null) return;

        activeMeshFilter = previewModel.GetComponentInChildren<MeshFilter>(true);
        if (activeMeshFilter == null || activeMeshFilter.sharedMesh == null) return;

        if (runtimeMaterial == null)
        {
            if (slashMaterial != null) runtimeMaterial = new Material(slashMaterial);
            else if (slashShader != null) runtimeMaterial = new Material(slashShader);
            if (runtimeMaterial == null) return;
            runtimeMaterial.name = "Slash Edit Preview (Temporary)";
            runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        ApplyRuntimeAppearance();
        ConfigureMeshParticle();
        if (slashParticleSystem.particleCount == 0) EmitSlashMeshParticle();
        SetParticleStreams(editModeDissolve);
        UpdateParticleColor();
    }

#if UNITY_EDITOR
    private void EditorPreviewUpdate()
    {
        if (Application.isPlaying || !previewInEditMode || !animateEditModePreview) return;
        editModeDissolve = Mathf.PingPong(
            (float)UnityEditor.EditorApplication.timeSinceStartup * Mathf.Max(sweepSpeed, 0.01f),
            Mathf.Max(dissolveAmount, 0.01f));
        PreviewInEditMode();
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    [ContextMenu("Play Slash Sweep")]
    public void PlaySlashSweep()
    {
        if (!playParticleOnSpawn || spawnedModel == null) return;
        StartSlashSweep();
    }

    [ContextMenu("Replay Attack")]
    public void ReplayAttack()
    {
        if (spawnedModel == null)
        {
            if (HasModels()) SpawnCurrentModel();
            return;
        }

        spawnedModel.SetActive(true);
        ApplyModelTransform();
        ApplySlashMaterial();
        StartSlashSweep();
    }

    private void StartSlashSweep()
    {
        if (slashParticleSystem == null && createParticleSystemAutomatically)
            CreateSlashParticleSystem();
        ConfigureMeshParticle();
        if (sweepCoroutine != null) StopCoroutine(sweepCoroutine);
        sweepCoroutine = StartCoroutine(SweepRoutine());
    }

    private IEnumerator SweepRoutine()
    {
        if (runtimeMaterial == null) yield break;
        Renderer[] modelRenderers = spawnedModel.GetComponentsInChildren<Renderer>(true);
        if (modelRenderers.Length == 0) yield break;

        if (slashParticleSystem == null || activeMeshFilter == null || activeMeshFilter.sharedMesh == null)
            yield break;

        foreach (Renderer modelRenderer in modelRenderers) modelRenderer.enabled = false;
        slashParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        EmitSlashMeshParticle();

        float elapsed = 0f;
        while (elapsed < SweepDuration)
        {
            if (spawnedModel == null) yield break;
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / SweepDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetParticleStreams(Mathf.Lerp(dissolveAmount, 0f, easedProgress));
            yield return null;
        }

        SetParticleStreams(0f);
        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

        elapsed = 0f;
        while (elapsed < DisappearDuration)
        {
            if (spawnedModel == null) yield break;
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / DisappearDuration);
            SetParticleStreams(Mathf.Lerp(0f, dissolveAmount, Mathf.SmoothStep(0f, 1f, progress)));
            yield return null;
        }

        SetParticleStreams(dissolveAmount);
        slashParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (hideAfterSweep && spawnedModel != null) spawnedModel.SetActive(false);
        sweepCoroutine = null;
    }

    private void ConfigureMeshParticle()
    {
        if (slashParticleSystem == null || activeMeshFilter == null || activeMeshFilter.sharedMesh == null) return;

        Transform particleTransform = slashParticleSystem.transform;
        particleTransform.SetPositionAndRotation(activeMeshFilter.transform.position, activeMeshFilter.transform.rotation);
        Vector3 parentScale = particleTransform.parent != null ? particleTransform.parent.lossyScale : Vector3.one;
        Vector3 meshScale = activeMeshFilter.transform.lossyScale;
        if (!Application.isPlaying) meshScale = Vector3.Scale(meshScale, spawnScale);
        if (activeMeshFilter.gameObject.name.StartsWith("Circle.007") ||
            (spawnedModel != null && spawnedModel.name.StartsWith("Circle.007")))
            meshScale *= circle007ScaleMultiplier;
        particleTransform.localScale = new Vector3(
            meshScale.x / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
            meshScale.y / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f),
            meshScale.z / Mathf.Max(Mathf.Abs(parentScale.z), 0.0001f));

        ParticleSystem.MainModule main = slashParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = 1f;
        main.startSizeY = 1f;
        main.startSizeZ = 1f;
        main.startLifetime = SweepDuration + holdTime + DisappearDuration + 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 1;
        main.startColor = Color.white;

        ParticleSystem.EmissionModule emission = slashParticleSystem.emission;
        emission.enabled = false;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = slashParticleSystem.shape;
        shape.enabled = false;
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = slashParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = false;
        ParticleSystem.ColorBySpeedModule colorBySpeed = slashParticleSystem.colorBySpeed;
        colorBySpeed.enabled = false;
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = slashParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = false;
        ParticleSystem.SizeBySpeedModule sizeBySpeed = slashParticleSystem.sizeBySpeed;
        sizeBySpeed.enabled = false;
        ParticleSystem.TrailModule trails = slashParticleSystem.trails;
        trails.enabled = false;
        ParticleSystem.NoiseModule noise = slashParticleSystem.noise;
        noise.enabled = false;

        ParticleSystem.CustomDataModule customData = slashParticleSystem.customData;
        customData.enabled = true;
        customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
        customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 4);

        ParticleSystemRenderer particleRenderer = slashParticleSystem.GetComponent<ParticleSystemRenderer>();
        particleRenderer.enabled = true;
        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.mesh = activeMeshFilter.sharedMesh;
        particleRenderer.sharedMaterial = runtimeMaterial;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        particleRenderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.Custom1XYZW
        });
    }

    private void EmitSlashMeshParticle()
    {
        slashParticleSystem.Play(false);
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
        {
            position = Vector3.zero,
            rotation3D = Vector3.zero,
            startSize3D = Vector3.one,
            startLifetime = SweepDuration + holdTime + DisappearDuration + 0.25f,
            startColor = glowColor
        };
        slashParticleSystem.Emit(emit, 1);
        // Custom particle data baru tersedia setelah particle benar-benar masuk
        // ke sistem. Simulate(0) menyinkronkannya tanpa memajukan animasi.
        slashParticleSystem.Simulate(0f, false, false, false);
        SetParticleStreams(dissolveAmount);
    }

    private void SetParticleStreams(float dissolve)
    {
        if (slashParticleSystem == null || slashParticleSystem.particleCount == 0) return;
        currentDissolve = dissolve;
        particleCustomData.Clear();
        slashParticleSystem.GetCustomParticleData(particleCustomData, ParticleSystemCustomData.Custom1);
        if (particleCustomData.Count == 0) particleCustomData.Add(Vector4.zero);
        // Custom1.y adalah pengali intensitas HDR di Slash Shader Graph.
        // Nilai 0 membuat seluruh output Base Color menjadi hitam.
        particleCustomData[0] = new Vector4(
            dissolve,
            Mathf.Max(glowIntensity, 0.0001f),
            voronoiSpeed,
            voronoiScale);
        slashParticleSystem.SetCustomParticleData(particleCustomData, ParticleSystemCustomData.Custom1);
    }

    private void UpdateParticleColor()
    {
        int count = slashParticleSystem.GetParticles(liveParticles);
        if (count == 0) return;
        liveParticles[0].startColor = glowColor;
        slashParticleSystem.SetParticles(liveParticles, count);
    }

    private void EmitSweepParticles(float progress)
    {
        if (slashParticleSystem == null || activeMeshFilter == null || activeMeshFilter.sharedMesh == null) return;
        Mesh mesh = activeMeshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        if (vertices.Length == 0 || uvs.Length != vertices.Length) return;

        float targetU = reverseSweep ? 1f - progress : progress;
        float tolerance = Mathf.Max(0.012f, sweepHeadWidth * 0.2f);
        sweepVertexCandidates.Clear();
        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        for (int vertexIndex = 0; vertexIndex < uvs.Length; vertexIndex++)
        {
            float distance = Mathf.Abs(uvs[vertexIndex].x - targetU);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = vertexIndex;
            }
            if (distance <= tolerance) sweepVertexCandidates.Add(vertexIndex);
        }
        if (sweepVertexCandidates.Count == 0) sweepVertexCandidates.Add(closestIndex);

        for (int i = 0; i < particlesPerFrame; i++)
        {
            int vertexIndex = sweepVertexCandidates[Random.Range(0, sweepVertexCandidates.Count)];
            Vector3 headPosition = activeMeshFilter.transform.TransformPoint(vertices[vertexIndex]);
            Vector3 jitter = Random.insideUnitSphere * particleSize * 0.2f;
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
            {
                position = headPosition + jitter,
                startSize = particleSize * Random.Range(0.65f, 1.35f),
                startLifetime = Mathf.Max(0.08f, SweepDuration * 0.3f),
                startColor = Color.Lerp(glowColor, coreColor, Random.value)
            };
            slashParticleSystem.Emit(emit, 1);
        }
    }

    private void DetectBoldEndDirection()
    {
        if (activeMeshFilter == null || activeMeshFilter.sharedMesh == null) return;
        Mesh mesh = activeMeshFilter.sharedMesh;
        if (!mesh.isReadable)
        {
            // Arah default tetap dapat dipakai. Jangan membaca vertices/UV dari
            // mesh import yang Read/Write-nya dimatikan karena Unity akan error.
            return;
        }
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        if (vertices.Length == 0 || uvs.Length != vertices.Length) return;

        Vector3 lowCenter = Vector3.zero;
        Vector3 highCenter = Vector3.zero;
        int lowCount = 0;
        int highCount = 0;
        for (int i = 0; i < uvs.Length; i++)
        {
            if (uvs[i].x <= 0.12f) { lowCenter += vertices[i]; lowCount++; }
            if (uvs[i].x >= 0.88f) { highCenter += vertices[i]; highCount++; }
        }
        if (lowCount == 0 || highCount == 0) return;
        lowCenter /= lowCount;
        highCenter /= highCount;

        float lowSpread = 0f;
        float highSpread = 0f;
        for (int i = 0; i < uvs.Length; i++)
        {
            if (uvs[i].x <= 0.12f) lowSpread = Mathf.Max(lowSpread, (vertices[i] - lowCenter).sqrMagnitude);
            if (uvs[i].x >= 0.88f) highSpread = Mathf.Max(highSpread, (vertices[i] - highCenter).sqrMagnitude);
        }
        reverseSweep = highSpread > lowSpread;
    }

    private void CreateSlashParticleSystem()
    {
        GameObject particleObject = new GameObject("Slash Mesh Particle");
        particleObject.transform.SetParent(spawnPoint, false);
        slashParticleSystem = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = slashParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.startSize = particleSize;
        main.startLifetime = SweepDuration * 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 1;

        ParticleSystem.EmissionModule emission = slashParticleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[0]);

        ParticleSystem.ColorOverLifetimeModule color = slashParticleSystem.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(1f, 0.72f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
    }

    [ContextMenu("Setup Particle System (Edit Mode)")]
    public void SetupParticleSystemForEditing()
    {
        if (slashParticleSystem == null) CreateSlashParticleSystem();
        if (slashParticleSystem == null) return;

        ParticleSystem.MainModule main = slashParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startSpeed = 0f;
        main.startSize = particleSize;
        main.startLifetime = SweepDuration * 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 256;

        ParticleSystem.EmissionModule emission = slashParticleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[0]);

        Debug.Log("Particle System edit-mode sudah dibuat. Pilih child 'Slash Sweep Particle' di Hierarchy untuk mengatur modulnya.", slashParticleSystem);
    }

    private static void RemoveLegacyGeneratedUI()
    {
        // Hanya menghapus Canvas buatan controller lama. Canvas milik user tidak disentuh.
        GameObject legacyCanvas = GameObject.Find("Slash Controller Canvas");
        if (legacyCanvas != null) Destroy(legacyCanvas);
    }

    private void SetNameText(string value)
    {
        if (circleNameText != null) circleNameText.text = value;
        if (circleNameTMP != null) circleNameTMP.text = value;
    }

    private void FrameCameraToModel()
    {
        if (targetCamera == null || spawnedModel == null) return;
        Renderer[] renderers = spawnedModel.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 direction = cameraDirection.sqrMagnitude > 0.001f ? cameraDirection.normalized : Vector3.back;

        if (targetCamera.orthographic)
        {
            float verticalSize = bounds.extents.y;
            float horizontalSize = bounds.extents.x / Mathf.Max(targetCamera.aspect, 0.01f);
            targetCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize) * cameraPadding;
            targetCamera.transform.position = bounds.center + direction * Mathf.Max(bounds.size.magnitude, 1f);
        }
        else
        {
            float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
            float halfFov = targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = radius / Mathf.Tan(halfFov) * cameraPadding;
            targetCamera.transform.position = bounds.center + direction * distance;
        }
        targetCamera.transform.LookAt(bounds.center);
    }

    private void ConfigureCameraQuality()
    {
        if (setFullHDOnBuild && !Application.isEditor) Screen.SetResolution(1920, 1080, true);
        QualitySettings.antiAliasing = 8;
        if (targetCamera == null) return;
        targetCamera.allowHDR = true;
        targetCamera.allowMSAA = true;
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = cameraBackground;
        ApplyFixedCameraRotation();
    }

    private void ApplyFixedCameraRotation()
    {
        if (targetCamera != null && applyFixedCameraRotation)
            targetCamera.transform.rotation = Quaternion.Euler(fixedCameraRotation);
    }

    private bool HasModels()
    {
        return slashModels != null && slashModels.Length > 0;
    }
}
*/