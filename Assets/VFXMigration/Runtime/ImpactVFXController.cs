using UnityEngine;

namespace VFXMigration
{
    public sealed class ImpactVFXController : MonoBehaviour
    {
        [SerializeField] private GameObject impactPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 0f, 20f);
        [SerializeField] private Vector3 scale = Vector3.one;
        [SerializeField, Min(0f)] private float destroyAfter = 1.5f;
        [SerializeField] private int sortingOrder = 100;

        public void PlayImpact() => PlayImpact(transform.position, Vector3.right);

        public void PlayImpact(Vector3 position, Vector3 direction)
        {
            if (impactPrefab == null) return;
            Quaternion rotation = Quaternion.identity;
            if (direction.sqrMagnitude > 0.0001f)
                rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            GameObject instance = Instantiate(impactPrefab, spawnPoint != null ? spawnPoint.position : position, rotation * Quaternion.Euler(rotationOffset));
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.sortingOrder = sortingOrder;
            foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play(true);
            }
            if (destroyAfter > 0f) Destroy(instance, destroyAfter);
        }
    }
}
