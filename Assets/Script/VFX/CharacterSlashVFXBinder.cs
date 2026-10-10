using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace ProjectCindra.VFX
{
    /// <summary>
    /// Connects existing Spine attack events to a selectable slash Particle System.
    /// This component does not read controller input; the existing combat controller
    /// remains the single source of truth for when an attack occurs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterSlashVFXBinder : MonoBehaviour
    {
        [Header("Attack Source")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string spineAttackEvent = "sword_attack";
        [SerializeField, Min(0f)] private float playDelay;

        [Header("Built-in Spine VFX")]
        [Tooltip("Keeps the slash artwork embedded in the Spine attack animation hidden so only the custom VFX is rendered.")]
        [SerializeField] private bool hideBuiltInSlash = true;
        [Tooltip("Spine slots containing the character pack's original slash artwork.")]
        [SerializeField] private string[] builtInSlashSlots = { "Slash_1" };

        [Header("Selectable Slash VFX")]
        [SerializeField] private ParticleSystem[] slashVariants;
        [SerializeField, Min(0)] private int selectedSlashIndex;
        [Tooltip("Creates a second copy of the selected slash behind the character for depth separation.")]
        [SerializeField] private bool createBackSlash = true;
        [SerializeField] private string sortingLayerName = "";
        [SerializeField] private int frontSortingOrder = 20;
        [SerializeField] private int backSortingOrder = 10;

        [Header("Direction and Placement")]
        [SerializeField] private bool mirrorWithCharacterFacing = true;
        [SerializeField] private bool invertDirection;
        [SerializeField] private bool reverseFrames;
        [SerializeField] private Vector3 localOffset = new Vector3(0.8f, 1.45f, -0.2f);
        [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
        [SerializeField, Min(0.01f)] private float uniformScale = 1f;

        private bool subscribed;
        private Coroutine delayedPlay;
        private ParticleSystem backSlash;

        public int SelectedSlashIndex => selectedSlashIndex;

        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();

            StopAllVariants();
            EnsureBackSlash();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (delayedPlay != null)
            {
                StopCoroutine(delayedPlay);
                delayedPlay = null;
            }
        }

        private void OnValidate()
        {
            if (slashVariants != null && slashVariants.Length > 0)
                selectedSlashIndex = Mathf.Clamp(selectedSlashIndex, 0, slashVariants.Length - 1);
            else
                selectedSlashIndex = 0;

            ApplyDirectionAndPlacement();
        }

        public void Configure(SkeletonAnimation source, ParticleSystem[] variants)
        {
            skeletonAnimation = source;
            slashVariants = variants;
            selectedSlashIndex = 0;
            ApplyDirectionAndPlacement();
        }

        /// <summary>Inspector/UI hook for choosing a slash variant without changing controller code.</summary>
        public void SetSlashVariant(int index)
        {
            if (slashVariants == null || slashVariants.Length == 0)
                return;

            selectedSlashIndex = Mathf.Clamp(index, 0, slashVariants.Length - 1);
        }

        [ContextMenu("Play Selected Slash")]
        public void PlaySelectedSlash()
        {
            if (slashVariants == null || slashVariants.Length == 0)
                return;

            selectedSlashIndex = Mathf.Clamp(selectedSlashIndex, 0, slashVariants.Length - 1);
            ApplyDirectionAndPlacement();
            EnsureBackSlash();

            for (int i = 0; i < slashVariants.Length; i++)
            {
                ParticleSystem variant = slashVariants[i];
                if (variant == null)
                    continue;

                variant.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (i != selectedSlashIndex)
                    continue;

                ConfigurePlaybackDirection(variant);
                ApplySorting(variant, frontSortingOrder);
                variant.Play(true);
            }

            if (backSlash != null)
            {
                backSlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ConfigurePlaybackDirection(backSlash);
                ApplySorting(backSlash, backSortingOrder);
                backSlash.Play(true);
            }
        }

        private void Subscribe()
        {
            if (subscribed || skeletonAnimation == null)
                return;

            skeletonAnimation.Initialize(false);
            if (skeletonAnimation.AnimationState == null)
                return;

            skeletonAnimation.AnimationState.Event += HandleSpineEvent;
            skeletonAnimation.UpdateComplete += HideBuiltInSlashSlots;
            subscribed = true;

            HideBuiltInSlashSlots(skeletonAnimation);
        }

        private void Unsubscribe()
        {
            if (!subscribed || skeletonAnimation == null)
                return;

            if (skeletonAnimation.AnimationState != null)
                skeletonAnimation.AnimationState.Event -= HandleSpineEvent;
            skeletonAnimation.UpdateComplete -= HideBuiltInSlashSlots;
            subscribed = false;
        }

        private void HideBuiltInSlashSlots(ISkeletonAnimation animatedSkeleton)
        {
            if (!hideBuiltInSlash || animatedSkeleton == null || animatedSkeleton.Skeleton == null || builtInSlashSlots == null)
                return;

            foreach (string slotName in builtInSlashSlots)
            {
                if (string.IsNullOrEmpty(slotName))
                    continue;

                Slot slot = animatedSkeleton.Skeleton.FindSlot(slotName);
                if (slot != null)
                    slot.Attachment = null;
            }
        }

        private void HandleSpineEvent(TrackEntry trackEntry, Spine.Event spineEvent)
        {
            if (spineEvent == null || spineEvent.Data == null || spineEvent.Data.Name != spineAttackEvent)
                return;

            if (delayedPlay != null)
                StopCoroutine(delayedPlay);

            if (playDelay <= 0f)
                PlaySelectedSlash();
            else
                delayedPlay = StartCoroutine(PlayAfterDelay());
        }

        private IEnumerator PlayAfterDelay()
        {
            yield return new WaitForSeconds(playDelay);
            delayedPlay = null;
            PlaySelectedSlash();
        }

        private void ApplyDirectionAndPlacement()
        {
            if (slashVariants == null)
                return;

            float facing = 1f;
            if (mirrorWithCharacterFacing && skeletonAnimation != null && skeletonAnimation.Skeleton != null)
                facing = skeletonAnimation.Skeleton.ScaleX < 0f ? -1f : 1f;
            if (invertDirection)
                facing *= -1f;

            foreach (ParticleSystem variant in slashVariants)
            {
                if (variant == null)
                    continue;

                Transform effectTransform = variant.transform;
                effectTransform.localPosition = new Vector3(localOffset.x * facing, localOffset.y, localOffset.z);
                effectTransform.localRotation = Quaternion.Euler(localEulerAngles);
                effectTransform.localScale = new Vector3(uniformScale * facing, uniformScale, uniformScale);
            }

            if (backSlash != null)
            {
                Transform effectTransform = backSlash.transform;
                effectTransform.localPosition = new Vector3(localOffset.x * facing, localOffset.y, localOffset.z);
                effectTransform.localRotation = Quaternion.Euler(localEulerAngles);
                effectTransform.localScale = new Vector3(uniformScale * facing, uniformScale, uniformScale);
            }
        }

        private void EnsureBackSlash()
        {
            if (!createBackSlash || backSlash != null || slashVariants == null || slashVariants.Length == 0 || slashVariants[0] == null)
                return;

            backSlash = Instantiate(slashVariants[0], slashVariants[0].transform.parent);
            backSlash.name = "VFXSlash_Back";
            backSlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ApplySorting(backSlash, backSortingOrder);
        }

        private void ApplySorting(ParticleSystem particleSystem, int order)
        {
            ParticleSystemRenderer renderer = particleSystem != null ? particleSystem.GetComponent<ParticleSystemRenderer>() : null;
            if (renderer != null)
            {
                if (!string.IsNullOrEmpty(sortingLayerName))
                    renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = order;
            }
        }

        private void ConfigurePlaybackDirection(ParticleSystem particleSystem)
        {
            ParticleSystem.TextureSheetAnimationModule sheet = particleSystem.textureSheetAnimation;
            if (!sheet.enabled)
                return;

            const float lastUsedFrame = 16f / 20f;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(
                1f,
                reverseFrames
                    ? AnimationCurve.Linear(0f, lastUsedFrame, 1f, 0f)
                    : AnimationCurve.Linear(0f, 0f, 1f, lastUsedFrame));
        }

        private void StopAllVariants()
        {
            if (slashVariants == null)
                return;

            foreach (ParticleSystem variant in slashVariants)
            {
                if (variant != null)
                    variant.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (backSlash != null)
                backSlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
