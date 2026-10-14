using System;
using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace ProjectCindra.VFX
{
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationVFXController : MonoBehaviour
    {
        [Serializable]
        private sealed class AnimationVFXBinding
        {
            [Tooltip("Exact Spine animation name. The VFX triggers as soon as this animation starts.")]
            [SerializeField] public string animationName;
            [SerializeField] public ParticleSystem effect;
            [Tooltip("Seconds to wait after the animation starts. Use 0 for immediate playback.")]
            [SerializeField, Min(0f)] public float delay;
            [Tooltip("Seconds the VFX stays visible. Use 0 to let its Particle System finish naturally.")]
            [SerializeField, Min(0f)] public float visibleDuration = 0.5667f;
            [SerializeField] public bool mirrorWithCharacterFacing = true;
            [SerializeField] public bool invertDirection;
            [SerializeField] public bool reverseFrames;
            [SerializeField] public Vector3 localOffset = new Vector3(3.1f, 1.35f, 0.2f);
            [SerializeField] public Vector3 localEulerAngles;
            [SerializeField, Min(0.01f)] public float uniformScale = 1f;
            [SerializeField] public int sortingOrder = 20;

            [NonSerialized] public Coroutine delayedPlay;
            [NonSerialized] public Coroutine stopRoutine;
            [NonSerialized] public TrackEntry pendingTrackEntry;
        }

        [Header("Animation Source")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private AnimationVFXBinding[] animationBindings = Array.Empty<AnimationVFXBinding>();

        [Header("Built-in Spine VFX")]
        [SerializeField] private bool hideBuiltInSlash = true;
        [SerializeField] private string[] builtInSlashSlots = { "Slash_1" };

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "";

        private bool subscribed;

        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopAllBindings();
        }

        private void Subscribe()
        {
            if (subscribed || skeletonAnimation == null)
                return;

            skeletonAnimation.Initialize(false);
            Spine.AnimationState animationState = skeletonAnimation.AnimationState;
            if (animationState == null)
                return;

            animationState.Start += HandleAnimationStart;
            animationState.Interrupt += HandleAnimationStopped;
            animationState.End += HandleAnimationStopped;
            skeletonAnimation.UpdateComplete += HideBuiltInSlashSlots;
            subscribed = true;
            HideBuiltInSlashSlots(skeletonAnimation);
        }

        private void Unsubscribe()
        {
            if (!subscribed || skeletonAnimation == null)
                return;

            Spine.AnimationState animationState = skeletonAnimation.AnimationState;
            if (animationState != null)
            {
                animationState.Start -= HandleAnimationStart;
                animationState.Interrupt -= HandleAnimationStopped;
                animationState.End -= HandleAnimationStopped;
            }
            skeletonAnimation.UpdateComplete -= HideBuiltInSlashSlots;
            subscribed = false;
        }

        private void HandleAnimationStart(TrackEntry trackEntry)
        {
            if (trackEntry?.Animation == null || animationBindings == null)
                return;

            foreach (AnimationVFXBinding binding in animationBindings)
            {
                if (binding == null || binding.effect == null ||
                    !string.Equals(trackEntry.Animation.Name, binding.animationName, StringComparison.Ordinal))
                    continue;

                CancelBinding(binding, true);
                if (binding.delay <= 0f)
                {
                    PlayBinding(binding);
                    continue;
                }

                binding.pendingTrackEntry = trackEntry;
                binding.delayedPlay = StartCoroutine(PlayAfterDelay(binding));
            }
        }

        private void HandleAnimationStopped(TrackEntry trackEntry)
        {
            if (animationBindings == null)
                return;

            foreach (AnimationVFXBinding binding in animationBindings)
            {
                if (binding == null || !ReferenceEquals(binding.pendingTrackEntry, trackEntry))
                    continue;

                if (binding.delayedPlay != null)
                    StopCoroutine(binding.delayedPlay);
                binding.delayedPlay = null;
                binding.pendingTrackEntry = null;
            }
        }

        private IEnumerator PlayAfterDelay(AnimationVFXBinding binding)
        {
            yield return new WaitForSeconds(binding.delay);
            binding.delayedPlay = null;
            binding.pendingTrackEntry = null;
            PlayBinding(binding);
        }

        private void PlayBinding(AnimationVFXBinding binding)
        {
            ParticleSystem effect = binding.effect;
            if (effect == null)
                return;

            if (binding.stopRoutine != null)
                StopCoroutine(binding.stopRoutine);
            binding.stopRoutine = null;

            if (!effect.gameObject.activeSelf)
                effect.gameObject.SetActive(true);

            ApplyPlacement(binding);
            ConfigurePlaybackDirection(binding);
            ApplySorting(effect, binding.sortingOrder);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(true);

            if (binding.visibleDuration > 0f)
                binding.stopRoutine = StartCoroutine(StopAfterDuration(binding));
        }

        private IEnumerator StopAfterDuration(AnimationVFXBinding binding)
        {
            yield return new WaitForSeconds(binding.visibleDuration);
            binding.effect?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            binding.stopRoutine = null;
        }

        private void CancelBinding(AnimationVFXBinding binding, bool clearEffect)
        {
            if (binding.delayedPlay != null)
                StopCoroutine(binding.delayedPlay);
            if (binding.stopRoutine != null)
                StopCoroutine(binding.stopRoutine);

            binding.delayedPlay = null;
            binding.stopRoutine = null;
            binding.pendingTrackEntry = null;
            if (clearEffect && binding.effect != null)
                binding.effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void StopAllBindings()
        {
            if (animationBindings == null)
                return;

            foreach (AnimationVFXBinding binding in animationBindings)
            {
                if (binding != null)
                    CancelBinding(binding, true);
            }
        }

        private void ApplyPlacement(AnimationVFXBinding binding)
        {
            float facing = 1f;
            if (binding.mirrorWithCharacterFacing && skeletonAnimation.Skeleton != null)
                facing = skeletonAnimation.Skeleton.ScaleX < 0f ? -1f : 1f;
            if (binding.invertDirection)
                facing *= -1f;

            Transform effectTransform = binding.effect.transform;
            effectTransform.localPosition = new Vector3(binding.localOffset.x * facing, binding.localOffset.y, binding.localOffset.z);
            effectTransform.localRotation = Quaternion.Euler(binding.localEulerAngles);
            effectTransform.localScale = new Vector3(binding.uniformScale * facing, binding.uniformScale, binding.uniformScale);
        }

        private void ConfigurePlaybackDirection(AnimationVFXBinding binding)
        {
            ParticleSystem.TextureSheetAnimationModule sheet = binding.effect.textureSheetAnimation;
            if (!sheet.enabled)
                return;

            const float lastUsedFrame = 16f / 20f;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(
                1f,
                binding.reverseFrames
                    ? AnimationCurve.Linear(0f, lastUsedFrame, 1f, 0f)
                    : AnimationCurve.Linear(0f, 0f, 1f, lastUsedFrame));
        }

        private void ApplySorting(ParticleSystem effect, int order)
        {
            ParticleSystemRenderer[] renderers = effect.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                if (!string.IsNullOrEmpty(sortingLayerName))
                    renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = order;
            }
        }

        private void HideBuiltInSlashSlots(ISkeletonAnimation animatedSkeleton)
        {
            if (!hideBuiltInSlash || animatedSkeleton?.Skeleton == null || builtInSlashSlots == null)
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
    }
}
