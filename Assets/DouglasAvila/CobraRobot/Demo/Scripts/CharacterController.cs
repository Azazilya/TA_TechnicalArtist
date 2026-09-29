using System;
using UnityEngine;

namespace DouglasAvila.CobraRobot.Scripts
{
    public class CharacterController : MonoBehaviour
    {
        [Header("Horizontal Movement")]
        [Header("Variables")] [SerializeField] private float maxHorizontalSpeed = 1000f;
        [SerializeField] private float acceleration = 500f;
        [SerializeField] private float deceleration = -10f;
        [Header("Jump")]
        [SerializeField] private float jumpForce = 10f;
        [Header("Melee Attack")]
        [SerializeField] private bool onlyMeleeAttackOnGround = false;
        [SerializeField] private int maxMeleeAttackComboCount = 1;
        [SerializeField] private float maxMeleeComboExpireTimer = 0.2f;
        [SerializeField] private float maxMeleeAttackInputBuffer = 0.2f;
        [SerializeField] private float meleeAttackRecoveringTimer = 0.5f;
        [Header("Range Attack")]
        [SerializeField] private bool onlyRangeAttackOnGround = false;
        [SerializeField] private float rangedAttackRecoveringTimer = 0.3f;


        private SpriteRenderer _spriteRenderer;
        private Animator _animator;
        private Rigidbody2D _rigidbody;
        private GroundCollider _groundCollider;

        // Inputs
        private float _inputHorizontalMovementDirection = 0f;
        private bool _inputJump = false;
        private bool _inputMeleeAttack = false;
        private bool _inputRangeAttack = false;
        private bool _inputSimulateDamage = false;
        private bool _inputSimulateDefeat = false;

        // Movement
        private bool _isGrounded = false;
        private bool _isHorizontalMoving = false;
        private float _xVelocity = 0f;
        private float _disableMovementTimer = 0f;
        private bool _jump = false;

        // Melee Attack
        private bool _isMeleeAttacking = false;
        private bool _meleeAttack = false;
        private int _meleeAttackComboCount = 0;
        private float _meleeComboExpireTimer = 0f;
        private float _meleeAttackInputBufferTimer = 0f;
        
        // Range Attack
        private bool _rangeAttack = false;
        
        // Simulations
        private bool _simulateDamage = false;
        private float damageRecoveringTimer = 0.2f;
        private bool _simulateDefeat = false;
        private float defeatRecoveringTimer = 1f;
        
        
        void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _groundCollider = transform.Find("GroundCollider").GetComponent<GroundCollider>();
        }
        
        void Update()
        {
            GetInput();
            HandleCollisions();
            HandleAnimationsParameters();
            HandleJump();
            HandleRangeAttack();
            HandleMeleeAttack();
            HandleSimulateDamage();
            HandleSimulateDefeat();
        }

        private void FixedUpdate()
        {
            HandleHorizontalMovement();
            HandleHorizontalMovementStop();
        }

        private void GetInput()
        {
            _inputHorizontalMovementDirection = Input.GetAxisRaw("Horizontal");
            _inputJump = Input.GetButtonDown("Jump");
            _inputRangeAttack = Input.GetKey(KeyCode.Period);
            _inputMeleeAttack = Input.GetKeyDown(KeyCode.Comma);
            _inputSimulateDamage = Input.GetKeyDown(KeyCode.RightShift);
            _inputSimulateDefeat = Input.GetKeyDown(KeyCode.RightControl);

            // Buffer the attack input button.
            if (_inputMeleeAttack) _meleeAttackInputBufferTimer = maxMeleeAttackInputBuffer;
        }

        /**
         * Handles the character collisions with the game scenery,
         * enemy and other elements
         */
        private void HandleCollisions()
        {
            _isGrounded = _groundCollider.IsColliding();
        }
      
        
        private void HandleAnimationsParameters()
        {
            _animator.SetFloat("velocityX", _rigidbody.linearVelocity.x);
            _animator.SetFloat("velocityY", _rigidbody.linearVelocity.y);
            _animator.SetBool("isHorizontalMoving", _isHorizontalMoving);
            _animator.SetBool("isGrounded", _isGrounded);
            _animator.SetInteger("meleeAttackComboCount", _meleeAttackComboCount);

            if (_jump)
            {
                _animator.SetTrigger("jump");
                _jump = false;
            }

            if (_rangeAttack)
            {
                _animator.SetTrigger("rangeAttack");
                _rangeAttack = false;
            }

            if (_meleeAttack)
            {
                _animator.SetTrigger("meleeAttack");
                _meleeAttack = false;
            }

            if (_simulateDamage)
            {
                _animator.SetTrigger("simulateDamage");
                _simulateDamage = false;
            }
            
            if (_simulateDefeat)
            {
                _animator.SetTrigger("simulateDefeat");
                _simulateDefeat = false;
            }
            
        }
        
        private void HandleHorizontalMovement()
        {
            // We always decrease the _disableMovementTimer. This timer is used to temporary disable
            // the character movement when needed 
            if (_disableMovementTimer > 0)
            {
                _disableMovementTimer -= Time.deltaTime;
                return;
            }
                
            // If we do not have horizontal movement input, we do nothing here
            if (_inputHorizontalMovementDirection == 0) return;

            // If we are attacking and grouded we do not move. If we attack on air we move normally
            if (_isMeleeAttacking && _isGrounded) return;
            
            // If we get some horizontal moving input, we move the character
            _xVelocity = Mathf.Clamp(_inputHorizontalMovementDirection * acceleration * Time.deltaTime, 
                    -maxHorizontalSpeed, 
                    maxHorizontalSpeed
                );
            _rigidbody.linearVelocity = new Vector2(_xVelocity, _rigidbody.linearVelocity.y);
            _spriteRenderer.flipX = _xVelocity < 0;
            _isHorizontalMoving = true;
        }
        

        private void HandleHorizontalMovementStop()
        {
            // If we have horizontal movement input, it means the player is pressing to move so we do nothing here
            if (_inputHorizontalMovementDirection != 0) return;
         
            _xVelocity = Mathf.Lerp(0.0f, _rigidbody.linearVelocity.x, Mathf.Pow(2, deceleration * Time.deltaTime));
            _rigidbody.linearVelocity = new Vector2(_xVelocity, _rigidbody.linearVelocity.y);
            _isHorizontalMoving = false;
        }


        private void HandleJump()
        {
            // If we are not grounded or we had no jump input button pressed, we do nothing here
            if (!_inputJump || !_isGrounded) return;
            
            InterruptRangeAttacking();
            InterruptMeleeAttacking();
            
            _groundCollider.Disable(0.2f);
            _disableMovementTimer = 0;
            _jump = true;
            _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, jumpForce);
        }

        /**
         * Handles the character ranged attacks
         * ATTENTION: This implementation do not handles the shooting particles, colliders and systems to
         * create the shooting bullets or cause damage to other characters such as enemies.
         * You need to implement it according to your game needings!
         */
        private void HandleRangeAttack()
        {
            // If we can only range attack on ground and we are not grounded we do nothing here
            if (onlyRangeAttackOnGround && !_isGrounded) return;
            
            if (!_inputRangeAttack) return;
            
            // If we are grounded we stop moving
            if (_isGrounded)
            {
                _disableMovementTimer = rangedAttackRecoveringTimer;
                _rigidbody.linearVelocity = new Vector2(0, _rigidbody.linearVelocity.y);
            }
            
            InterruptMeleeAttacking();
            _isHorizontalMoving = false;
            _rangeAttack = true;
        }
        
        /**
        * Called by other states that may need to interrupt the meleeAttack before it reaches the finishing point
        */
        private void InterruptRangeAttacking()
        {
            _rangeAttack = false;
            _inputRangeAttack = false;
        }

        /**
         * Handles the character melee attacks
         * ATTENTION: This implementation do not handles the attack colliders and systems to make
         * the attacks to cause damage to other characters such as enemies.
         * You need to implement it according to your game needings!
         */
        private void HandleMeleeAttack()
        {
            // We always decrease the meleeComboExpireTimer. This timer is used to know when a combo had expired
            if (_meleeComboExpireTimer > 0) _meleeComboExpireTimer -= Time.deltaTime;

            // We always decrease the meleeAttackBuffer. This timer is used to buff the attack button press and 
            // give the player a better response for attack button pressing
            if (_meleeAttackInputBufferTimer > 0) _meleeAttackInputBufferTimer -= Time.deltaTime;
            
            // If we are already attacking we do nothing here
            if (_isMeleeAttacking) return;
            
            // If we have no melee attack input and we have no input buffered, we do nothing here
            if (!_inputMeleeAttack && _meleeAttackInputBufferTimer <= 0) return;
            
            // If we can only melee attack on ground and we are not grounded we do nothing here
            if (onlyMeleeAttackOnGround && !_isGrounded) return;

            if (_meleeComboExpireTimer <= 0)
            {
                _meleeAttackComboCount = 0;
            }

            // If we are grounded we stop moving
            if (_isGrounded)
            {
                _disableMovementTimer = meleeAttackRecoveringTimer;
                _rigidbody.linearVelocity = new Vector2(0, _rigidbody.linearVelocity.y);
            }
            _isHorizontalMoving = false;
            _meleeAttack = true;
            _isMeleeAttacking = true;

        }
        
        /**
         * Called by other states that may need to interrupt the meleeAttack before it reaches the finishing point
         */
        private void InterruptMeleeAttacking()
        {
            _meleeAttack = false;
            _isMeleeAttacking = false;
            _inputMeleeAttack = false;
            _meleeAttackComboCount = 0;
            _meleeComboExpireTimer = 0;
            _meleeAttackInputBufferTimer = 0;
        }

        /**
         * Function to be called by character's animator component to notify the script
         * that the currently executing attack had finished
         * In the character "Animation" menu, on the melee attacks animations, there's an "event"
         * that calls this function at the point we want the character to be able to connect
         * the next attack combo
         */
        private void FinishMeleeAttacking()
        {
            _isMeleeAttacking = false;
            _meleeComboExpireTimer = maxMeleeComboExpireTimer;
            _meleeAttackComboCount =
                _meleeAttackComboCount + 1 > maxMeleeAttackComboCount ? 0 : _meleeAttackComboCount + 1;
        }

        /**
         * This method simulates the character taking damage behavior,
         * it's only used to showcase the damage animation and should not be used in the final game
         */
        private void HandleSimulateDamage()
        {
            if (!_inputSimulateDamage) return;
            
            _rigidbody.linearVelocity = new Vector2(0, _rigidbody.linearVelocity.y);
            _disableMovementTimer = damageRecoveringTimer;

            _simulateDamage = true;
        }

        /**
         * This method simulates the character taking defeated behavior,
         * it's only used to showcase the damage animation and should not be used in the final game
         */
        private void HandleSimulateDefeat()
        {
            if (!_inputSimulateDefeat) return;
            
            _rigidbody.linearVelocity = new Vector2(0, _rigidbody.linearVelocity.y);
            _disableMovementTimer = defeatRecoveringTimer;

            _simulateDefeat = true;
        }
    }
}
