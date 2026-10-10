using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace ProjectCindra.VFX
{
    /// <summary>Controls the TrailRenderer attached to the sword bone for the attack swing.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class SlashTrailController : MonoBehaviour
    {
        [Header("Attack Sequence")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string attackEventName = "sword_attack";
        [SerializeField, Min(0.01f)] private float swingDuration = 0.38f;
        [SerializeField, Min(0f)] private float eventStartDelay;

        [Header("Trail Look")]
        [SerializeField] private Material trailMaterial;
        [SerializeField] private string sortingLayerName = "";
        [SerializeField] private int sortingOrder = 12;
        [SerializeField, Min(0f)] private float widthMultiplier = 1f;
        [SerializeField] private Gradient color = null;
        [SerializeField, Min(0f)] private float minVertexDistance = 0.02f;
        [SerializeField] private AnimationCurve widthCurve = null;

        private TrailRenderer trail;
        private Coroutine stopRoutine;

        private void Awake()
        {
            trail = GetComponent<TrailRenderer>();
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInParent<SkeletonAnimation>();
            ApplySettings();
            StopTrail();
        }

        private void OnEnable()
        {
            if (Application.isPlaying && skeletonAnimation != null && skeletonAnimation.AnimationState != null)
                skeletonAnimation.AnimationState.Event += HandleSpineEvent;
        }

        private void Start()
        {
            if (skeletonAnimation == null)
                return;

            skeletonAnimation.Initialize(false);
            if (skeletonAnimation.AnimationState != null)
                skeletonAnimation.AnimationState.Event += HandleSpineEvent;
        }

        private void OnDisable()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
                skeletonAnimation.AnimationState.Event -= HandleSpineEvent;
            StopTrail();
        }

        private void OnValidate()
        {
            if (trail == null)
                trail = GetComponent<TrailRenderer>();
            ApplySettings();
        }

        private void HandleSpineEvent(TrackEntry entry, Spine.Event spineEvent)
        {
            if (spineEvent?.Data == null || spineEvent.Data.Name != attackEventName)
                return;

            if (stopRoutine != null)
                StopCoroutine(stopRoutine);
            stopRoutine = StartCoroutine(PlaySwing());
        }

        private IEnumerator PlaySwing()
        {
            if (eventStartDelay > 0f)
                yield return new WaitForSeconds(eventStartDelay);

            trail.Clear();
            trail.emitting = true;
            yield return new WaitForSeconds(swingDuration);
            StopTrail();
            stopRoutine = null;
        }

        private void StopTrail()
        {
            if (trail == null)
                return;
            trail.emitting = false;
            trail.Clear();
        }

        private void ApplySettings()
        {
            if (trail == null)
                return;

            if (trailMaterial != null)
                trail.sharedMaterial = trailMaterial;
            if (!string.IsNullOrEmpty(sortingLayerName))
                trail.sortingLayerName = sortingLayerName;
            trail.sortingOrder = sortingOrder;
            trail.widthMultiplier = widthMultiplier;
            trail.minVertexDistance = minVertexDistance;
            if (color != null)
                trail.colorGradient = color;
            if (widthCurve != null)
                trail.widthCurve = widthCurve;
        }
    }
}
