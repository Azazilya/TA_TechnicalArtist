using System.Collections;
using ProjectCindra.Combat;
using UnityEngine;

namespace ProjectCindra.VFX
{
    /// <summary>
    /// Spawns a reusable impact effect when the object receives damage.
    /// Assign any VFXImpact particle prefab or a scene template by drag-and-drop.
    /// </summary>
    [RequireComponent(typeof(DummyEnemy))]
    public sealed class VFXImpact : MonoBehaviour
    {
        [Header("Impact Asset")]
        [SerializeField] private GameObject impactPrefab;
        [SerializeField, Min(0f)] private float delay;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool spawnAtHitPoint = true;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private bool parentToTarget;
        [SerializeField] private Vector3 scale = Vector3.one;
        [SerializeField, Min(0f)] private float destroyAfter = 1.5f;

        [Header("Rotation")]
        [Tooltip("Align the slash to the direction from the weapon to the hit point.")]
        [SerializeField] private bool rotateToHitDirection = true;
        [Tooltip("Extra rotation applied after the hit direction. Use Z for a 2D slash angle.")]
        [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 0f, 20f);

        [Header("Slash Appearance")]
        [Tooltip("Apply the values below to the impact material at runtime without changing the asset.")]
        [SerializeField] private bool overrideMaterialAppearance = true;
        [Tooltip("Mask thickness. 0 is very thin/faded, 1 is fully solid. Keep this between 0 and 1.")]
        [SerializeField, Range(0f, 1f)] private float thickness = 0.9f;
        [Tooltip("HDR brightness of the slash. Increase this when the color looks washed out.")]
        [SerializeField, Min(0f)] private float intensity = 12f;

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "";
        [SerializeField] private int sortingOrder = 100;

        private static readonly int ThicknessProperty = Shader.PropertyToID("_Thickness");
        private static readonly int IntensityProperty = Shader.PropertyToID("_Intensity");

        private DummyEnemy damageSource;

        private void Awake()
        {
            damageSource = GetComponent<DummyEnemy>();
            if (impactPrefab != null && impactPrefab.scene.IsValid() && impactPrefab != gameObject)
                StopTemplate(impactPrefab);
        }

        private void OnEnable()
        {
            if (damageSource != null)
                damageSource.DamageTaken += HandleDamage;
        }

        private void OnDisable()
        {
            if (damageSource != null)
                damageSource.DamageTaken -= HandleDamage;
        }

        private void HandleDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (impactPrefab == null)
                return;

            if (delay <= 0f)
            {
                SpawnImpact(hitPoint, hitDirection);
                return;
            }

            StartCoroutine(SpawnAfterDelay(hitPoint, hitDirection));
        }

        private IEnumerator SpawnAfterDelay(Vector3 hitPoint, Vector3 hitDirection)
        {
            yield return new WaitForSeconds(delay);
            SpawnImpact(hitPoint, hitDirection);
        }

        private void SpawnImpact(Vector3 hitPoint, Vector3 hitDirection)
        {
            if (impactPrefab == null)
                return;

            Vector3 position;
            if (spawnPoint != null)
                position = spawnPoint.position;
            else if (spawnAtHitPoint)
                position = hitPoint + transform.TransformVector(localOffset);
            else
                position = transform.TransformPoint(localOffset);

            Quaternion rotation = transform.rotation;
            if (rotateToHitDirection && hitDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
                rotation = Quaternion.Euler(0f, 0f, angle);
            }
            rotation *= Quaternion.Euler(rotationOffset);

            GameObject instance = Instantiate(impactPrefab, position, rotation,
                parentToTarget ? transform : null);
            instance.name = impactPrefab.name + " (Enemy Impact)";
            instance.SetActive(true);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale);
            ApplySorting(instance);
            ApplyMaterialAppearance(instance);

            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = particle.main;
                main.startDelay = 0f;
                particle.Play(true);
            }

            if (destroyAfter > 0f)
                StartCoroutine(DestroyAfter(instance, destroyAfter));
        }

        private IEnumerator DestroyAfter(GameObject instance, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (instance != null)
                Destroy(instance);
        }

        private void StopTemplate(GameObject template)
        {
            foreach (ParticleSystem particle in template.GetComponentsInChildren<ParticleSystem>(true))
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            template.SetActive(false);
        }

        private void ApplyMaterialAppearance(GameObject instance)
        {
            if (!overrideMaterialAppearance)
                return;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material == null ||
                    (!material.HasProperty(ThicknessProperty) && !material.HasProperty(IntensityProperty)))
                    continue;

                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                if (material.HasProperty(ThicknessProperty))
                    properties.SetFloat(ThicknessProperty, thickness);
                if (material.HasProperty(IntensityProperty))
                    properties.SetFloat(IntensityProperty, intensity);
                renderer.SetPropertyBlock(properties);
            }
        }

        private void ApplySorting(GameObject instance)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!string.IsNullOrEmpty(sortingLayerName))
                    renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;
            }
        }
    }
}
