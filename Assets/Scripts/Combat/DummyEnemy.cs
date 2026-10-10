using System;
using UnityEngine;

namespace ProjectCindra.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class DummyEnemy : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Auto-Fitted Hurtbox")]
        [SerializeField] private BoxCollider hurtbox;
        [SerializeField] private Vector3 colliderPadding = new Vector3(0.12f, 0.08f, 0.25f);
        [SerializeField] private bool refitColliderWhilePlaying = true;

        [Header("Optional Hit Reaction")]
        [SerializeField] private Animator animator;
        [SerializeField] private string hitStateName = "pushed";
        [SerializeField] private string deathStateName = "died";

        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;
        public event Action<float, Vector3, Vector3> DamageTaken;

        private void Reset()
        {
            CacheComponents();
            ConfigurePhysics();
            FitColliderToRenderers();
            currentHealth = maxHealth;
        }

        private void Awake()
        {
            CacheComponents();
            ConfigurePhysics();
            currentHealth = maxHealth;
            FitColliderToRenderers();
        }

        private void LateUpdate()
        {
            if (refitColliderWhilePlaying)
                FitColliderToRenderers();
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            colliderPadding = Vector3.Max(Vector3.zero, colliderPadding);
            CacheComponents();
            ConfigurePhysics();
        }

        public void TakeDamage(float damage)
        {
            TakeDamage(damage, transform.position, Vector3.zero);
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (!IsAlive || damage <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            DamageTaken?.Invoke(damage, hitPoint, hitDirection);
            if (animator != null)
            {
                string state = IsAlive ? hitStateName : deathStateName;
                if (!string.IsNullOrEmpty(state) && animator.HasState(0, Animator.StringToHash(state)))
                    animator.Play(state, 0, 0f);
            }

            Debug.Log($"[DummyTarget] Hit {damage:0.#}. HP {currentHealth:0.#}/{maxHealth:0.#}", this);
        }

        [ContextMenu("Refit Collider To Mesh")]
        public void FitColliderToRenderers()
        {
            CacheComponents();
            if (hurtbox == null)
                return;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            bool hasBounds = false;
            Bounds localBounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                    continue;

                Bounds worldBounds = targetRenderer.bounds;
                Vector3 min = worldBounds.min;
                Vector3 max = worldBounds.max;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 worldCorner = new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z);
                    Vector3 localCorner = transform.InverseTransformPoint(worldCorner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            if (!hasBounds)
                return;

            hurtbox.center = localBounds.center;
            hurtbox.size = Vector3.Max(localBounds.size + colliderPadding, Vector3.one * 0.05f);
            hurtbox.isTrigger = true;
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
        }

        private void CacheComponents()
        {
            if (hurtbox == null)
                hurtbox = GetComponent<BoxCollider>();
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void ConfigurePhysics()
        {
            if (hurtbox != null)
                hurtbox.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }
    }
}
