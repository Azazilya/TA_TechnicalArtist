using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectCindra.Combat
{
    [DisallowMultipleComponent]
    public sealed class AttackSystem : MonoBehaviour
    {
        [Header("Spine Attack")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField, SpineAnimation] private string attackAnimationName = "sword_attack";
        [SerializeField] private string damageEventName = "sword_attack";
        [SerializeField, Min(0.05f)] private float attackLockDuration = 0.7f;
        [SerializeField, Min(0f)] private float returnToBaseFadeDuration = 0.2f;

        [Header("Damage")]
        [SerializeField] private MeleeHitbox swordHitbox;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0f)] private float heavyAttackDamage = 25f;
        [SerializeField, Min(0.05f)] private float heavyAttackLockDuration = 3f;
        [SerializeField, Min(0.02f)] private float hitboxActiveDuration = 0.16f;

        private InputAction attackAction;
        private InputAction heavyAttackAction;
        private Spine.AnimationState animationState;
        private float lockedUntil;
        private float pendingDamage;
        private bool subscribed;

        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (swordHitbox == null)
                swordHitbox = GetComponentInChildren<MeleeHitbox>(true);

            attackAction = new InputAction("Attack", InputActionType.Button);
            attackAction.AddBinding("<Gamepad>/buttonWest");
            attackAction.AddBinding("<Mouse>/leftButton");
            attackAction.AddBinding("<Keyboard>/enter");

            heavyAttackAction = new InputAction("Heavy Attack", InputActionType.Button);
            heavyAttackAction.AddBinding("<Gamepad>/rightShoulder");
            heavyAttackAction.AddBinding("<Mouse>/rightButton");
        }

        private void OnEnable()
        {
            attackAction.Enable();
            heavyAttackAction.Enable();
            SubscribeToSpineEvents();
        }

        private void Start()
        {
            SubscribeToSpineEvents();
        }

        private void Update()
        {
            if (Time.time < lockedUntil)
                return;

            if (heavyAttackAction.WasPressedThisFrame())
                PlayAttack(heavyAttackDamage, heavyAttackLockDuration);
            else if (attackAction.WasPressedThisFrame())
                PlayAttack(attackDamage, attackLockDuration);
        }

        public void Attack()
        {
            if (Time.time >= lockedUntil)
                PlayAttack(attackDamage, attackLockDuration);
        }

        public void HeavyAttack()
        {
            if (Time.time >= lockedUntil)
                PlayAttack(heavyAttackDamage, heavyAttackLockDuration);
        }

        private void PlayAttack(float selectedDamage, float lockDuration)
        {
            if (animationState == null)
                SubscribeToSpineEvents();
            if (animationState == null || string.IsNullOrEmpty(attackAnimationName))
                return;

            pendingDamage = selectedDamage;
            lockedUntil = Time.time + Mathf.Max(0.05f, lockDuration);
            swordHitbox?.EndAttack();

            // Track 1 overlays the movement animation on track 0, so the existing
            // movement controller remains untouched.
            animationState.SetAnimation(1, attackAnimationName, false);
            animationState.AddEmptyAnimation(1, returnToBaseFadeDuration, 0f);
        }

        private void SubscribeToSpineEvents()
        {
            if (subscribed || skeletonAnimation == null)
                return;

            skeletonAnimation.Initialize(false);
            animationState = skeletonAnimation.AnimationState;
            if (animationState == null)
                return;

            animationState.Event += HandleSpineEvent;
            subscribed = true;
        }

        private void HandleSpineEvent(TrackEntry trackEntry, Spine.Event spineEvent)
        {
            if (spineEvent?.Data == null || spineEvent.Data.Name != damageEventName)
                return;

            swordHitbox?.BeginAttack(pendingDamage, hitboxActiveDuration);
        }

        private void OnDisable()
        {
            if (subscribed && animationState != null)
                animationState.Event -= HandleSpineEvent;
            subscribed = false;
            swordHitbox?.EndAttack();
            attackAction?.Disable();
            heavyAttackAction?.Disable();
        }

        private void OnDestroy()
        {
            attackAction?.Dispose();
            heavyAttackAction?.Dispose();
        }
    }
}
