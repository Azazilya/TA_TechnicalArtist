using System.Collections.Generic;
using UnityEngine;

namespace ProjectCindra.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MeleeHitbox : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float defaultActiveDuration = 0.16f;

        private readonly HashSet<int> damagedTargets = new HashSet<int>();
        private Collider hitboxCollider;
        private Transform ownerRoot;
        private float damage;
        private float activeUntil;
        private bool attackActive;

        private void Awake()
        {
            hitboxCollider = GetComponent<Collider>();
            hitboxCollider.isTrigger = true;
            ownerRoot = transform.root;
        }

        private void Update()
        {
            if (attackActive && Time.time >= activeUntil)
                EndAttack();
        }

        public void BeginAttack(float attackDamage)
        {
            BeginAttack(attackDamage, defaultActiveDuration);
        }

        public void BeginAttack(float attackDamage, float activeDuration)
        {
            damage = Mathf.Max(0f, attackDamage);
            activeUntil = Time.time + Mathf.Max(0.02f, activeDuration);
            damagedTargets.Clear();
            attackActive = true;
        }

        public void EndAttack()
        {
            attackActive = false;
            damagedTargets.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // This also catches a target that was already overlapping when the
            // short attack window became active.
            TryDamage(other);
        }

        private void TryDamage(Collider other)
        {
            if (!attackActive || other == null || other.transform.root == ownerRoot)
                return;

            MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is IDamageable target) || !target.IsAlive)
                    continue;

                int targetId = behaviours[i].gameObject.GetInstanceID();
                if (!damagedTargets.Add(targetId))
                    return;

                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitDirection = (other.bounds.center - transform.position).normalized;
                target.TakeDamage(damage, hitPoint, hitDirection);
                return;
            }
        }

        private void OnDisable()
        {
            EndAttack();
        }
    }
}
