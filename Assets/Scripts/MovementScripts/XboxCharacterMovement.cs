using UnityEngine;
using UnityEngine.InputSystem;
using Spine;
using Spine.Unity;

namespace ProjectCindra.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class XboxCharacterMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 5f;
        [SerializeField, Min(0f)] private float runSpeed = 8f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpForce = 7f;
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.15f;

        [Header("Spine Animation")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SpineAnimation] private string idleAnimationName = "idle_1";
        [SpineAnimation] private string walkAnimationName = "walk";
        [SpineAnimation] private string runAnimationName = "run";
        [SpineAnimation] private string jumpAnimationName = "jump";

        private Rigidbody characterRigidbody;
        private Collider characterCollider;
        private InputAction moveAction;
        private InputAction runAction;
        private InputAction jumpAction;
        private Vector2 moveInput;
        private Spine.AnimationState spineAnimationState;
        private bool isMoving;
        private bool isRunning;
        private bool isGrounded;
        private bool jumpRequested;
        private string currentAnimation;

        private void Awake()
        {
            characterRigidbody = GetComponent<Rigidbody>();
            characterRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            characterCollider = GetComponent<Collider>();

            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();

            if (skeletonAnimation != null)
            {
                spineAnimationState = skeletonAnimation.AnimationState;
                spineAnimationState.SetAnimation(0, idleAnimationName, true);
            }

            moveAction = new InputAction(
                name: "Move",
                type: InputActionType.Value,
                expectedControlType: "Vector2");

            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            runAction = new InputAction(
                name: "Run",
                type: InputActionType.Button,
                binding: "<Gamepad>/leftStickPress");

            jumpAction = new InputAction(
                name: "Jump",
                type: InputActionType.Button,
                binding: "<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            runAction.Enable();
            jumpAction.Enable();
        }

        private void OnDisable()
        {
            moveInput = Vector2.zero;
            jumpRequested = false;
            moveAction.Disable();
            runAction.Disable();
            jumpAction.Disable();
        }

        private void Update()
        {
            moveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

            isMoving = moveInput.sqrMagnitude > 0.01f;
            isGrounded = CheckGrounded();

            bool runHeld = runAction.IsPressed();

            // Only a fresh press (not held) of A can start a jump, and only while grounded.
            if (jumpAction.WasPressedThisFrame() && isGrounded)
                jumpRequested = true;

            string targetAnimation;

            if (!isGrounded)
            {
                // Airborne: play the jump animation once and hold its last frame (no looping).
                targetAnimation = jumpAnimationName;
                isRunning = false;
            }
            else
            {
                if (!isMoving)
                {
                    targetAnimation = idleAnimationName;
                    isRunning = false;
                }
                else if (runHeld)
                {
                    targetAnimation = runAnimationName;
                    isRunning = true;
                }
                else
                {
                    targetAnimation = walkAnimationName;
                    isRunning = false;
                }
            }

            if (targetAnimation != currentAnimation)
            {
                currentAnimation = targetAnimation;
                // Jump is a one-shot (no looping); every other animation loops.
                bool loop = targetAnimation != jumpAnimationName;
                spineAnimationState?.SetAnimation(0, currentAnimation, loop);
            }

            if (isMoving && skeletonAnimation != null)
            {
                bool faceRight = moveInput.x > 0f;
                skeletonAnimation.skeleton.ScaleX = faceRight ? 1f : -1f;
            }
        }

        private void FixedUpdate()
        {
            isGrounded = CheckGrounded();

            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            Vector3 velocity = characterRigidbody.linearVelocity;
            velocity.x = moveInput.x * currentSpeed;
            velocity.z = moveInput.y * currentSpeed;

            if (jumpRequested && isGrounded)
            {
                velocity.y = jumpForce;
                isGrounded = false;
            }

            jumpRequested = false;
            characterRigidbody.linearVelocity = velocity;
        }

        private bool CheckGrounded()
        {
            Vector3 origin = transform.position;
            if (characterCollider != null)
                origin.y = characterCollider.bounds.min.y + 0.05f;
            return Physics.Raycast(origin, Vector3.down, groundCheckDistance);
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            runAction?.Dispose();
            jumpAction?.Dispose();
        }
    }
}
