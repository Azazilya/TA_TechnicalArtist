using System.Collections;
using ProjectCindra.Combat;
using UnityEngine;

namespace ProjectCindra.VFX
{
    /// <summary>
    /// Spawns a reusable impact effect when the object receives damage.
    /// Assign any VFXImpact particle prefab or a scene template by drag-and-drop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VFXImpact : MonoBehaviour
    {
        [Header("Impact Asset")]
        [SerializeField] private GameObject impactPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private bool parentToTarget;
        [SerializeField, Min(0f)] private float destroyAfter = 1.5f;

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "";
        [SerializeField] private int sortingOrder = 30;
        [SerializeField] private bool rotateToHitDirection = true;

        private DummyEnemy damageSource;

        private void Awake()
        {
            damageSource = GetComponent<DummyEnemy>();
            if (damageSource != null)
                damageSource.DamageTaken += HandleDamage;

            if (impactPrefab != null && impactPrefab.scene.IsValid() && impactPrefab != gameObject)
                StopTemplate(impactPrefab);
        }

        private void OnDestroy()
        {
            if (damageSource != null)
                damageSource.DamageTaken -= HandleDamage;
        }

        private void HandleDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (impactPrefab == null)
                return;

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.TransformPoint(localOffset);
            Quaternion rotation = transform.rotation;
            if (rotateToHitDirection && hitDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
                rotation = Quaternion.Euler(0f, 0f, angle);
            }

            GameObject instance = Instantiate(impactPrefab, position, rotation,
                parentToTarget ? transform : null);
            instance.name = impactPrefab.name + " (Enemy Impact)";
            instance.SetActive(true);
            ApplySorting(instance);

            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
