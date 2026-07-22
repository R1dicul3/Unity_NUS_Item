using UnityEngine;
using UnityEngine.InputSystem;

[SelectionBase]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlatformerPlayerController : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 70f;
    [SerializeField] private float deceleration = 85f;

    [Header("Jump")]
    [SerializeField] private float jumpVelocity = 14f;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private bool canDoubleJump = true;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallGravityMultiplier = 1.55f;
    [SerializeField] private float shortHopGravityMultiplier = 2.2f;

    [Header("Dash")]
    [SerializeField] private bool canDash = true;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.16f;
    [SerializeField] private float dashCooldown = 0.55f;

    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.78f, 0.12f);
    [SerializeField] private float groundCheckOffset = 0.53f;
    [SerializeField] private float groundCheckLockoutAfterJump = 0.08f;

    [Header("Physics")]
    [SerializeField] private bool useFrictionlessMaterial = true;

    [Header("Collider")]
    [SerializeField] private bool autoAlignCollider = true;
    [SerializeField] private Vector2 bodySize = new Vector2(0.75f, 1.05f);
    [SerializeField] private Vector2 bodyOffset;

    [Header("Control State")]
    [SerializeField] private bool isControlled = true;

    [Header("Animation")]
    [SerializeField] private float walkAnimationThreshold = 0.05f;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private TrailRenderer dashTrail;
    private PhysicsMaterial2D frictionlessMaterial;

    private float moveInput;
    private float facingDirection = 1f;
    private int jumpsUsed;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isDashing;
    private bool isInDialogue;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float groundCheckLockoutTimer;
    private float defaultGravityScale;

    private PlayerInputActions inputActions;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsInDialogueHash = Animator.StringToHash("IsInDialogue");

    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsControlled => isControlled;
    public bool IsInDialogue => isInDialogue;
    public bool CanDoubleJump => canDoubleJump;
    public bool CanDash => canDash;
    public bool IsWeakerCharacter => !canDoubleJump && !canDash;
    public Collider2D BodyCollider => boxCollider;

    public void Initialize(LayerMask platformMask) {
    }

    public void Initialize(LayerMask platformMask, bool allowDoubleJump, bool allowDash) {
        SetAbilities(allowDoubleJump, allowDash);
    }

    public void SetAbilities(bool allowDoubleJump, bool allowDash) {
        canDoubleJump = allowDoubleJump;
        canDash = allowDash;
        maxJumpCount = canDoubleJump ? 2 : 1;

        if (!canDash && isDashing) {
            isDashing = false;
            rb.gravityScale = defaultGravityScale;
        }

        if (!canDoubleJump && jumpsUsed > 1) {
            jumpsUsed = 1;
        }
    }

    public void SetControlled(bool value) {
        isControlled = value;

        if (!isControlled) {
            moveInput = 0f;
            jumpBufferTimer = 0f;
            isDashing = false;
            rb.gravityScale = defaultGravityScale;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    public void SetDialogueState(bool inDialogue) {
        isInDialogue = inDialogue;
        SetControlled(!inDialogue);
    }

    private void Awake() {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        dashTrail = GetComponent<TrailRenderer>();
        defaultGravityScale = rb.gravityScale;
        AlignCollider();
        ApplyFrictionlessMaterial();
    }

    private void OnEnable() {
        inputActions?.Enable();
    }

    private void OnDisable() {
        inputActions?.Disable();
    }

    private void OnDestroy() {
        inputActions?.Dispose();
    }

    private void Update() {
        UpdateGroundState();
        UpdateTimers();

        if (isControlled) {
            ReadInput();
            TryJump();
            TryDash();
        }
        else {
            moveInput = 0f;
            jumpBufferTimer = 0f;
        }

        UpdateVisuals();
    }

    private void FixedUpdate() {
        if (isDashing) {
            rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);
            return;
        }

        ApplyHorizontalMovement();
        ApplyBetterJumpGravity();
    }

    private void ReadInput() {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>().x;

        if (Mathf.Abs(moveInput) > 0.01f) {
            facingDirection = Mathf.Sign(moveInput);
        }

        if (inputActions.Player.Jump.WasPressedThisFrame()) {
            jumpBufferTimer = jumpBufferTime;
        }
    }

    private void UpdateGroundState() {
        wasGrounded = isGrounded;

        if (groundCheckLockoutTimer > 0f || rb.linearVelocity.y > 0.5f) {
            isGrounded = false;
            return;
        }

        Vector2 checkCenter = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        isGrounded = false;

        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, groundCheckSize, 0f);
        foreach (Collider2D hit in hits) {
            if (IsGroundCollider(hit)) {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded) {
            coyoteTimer = coyoteTime;
            jumpsUsed = 0;
        }

        if (!wasGrounded && isGrounded) {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, 0f));
        }
    }

    private void UpdateTimers() {
        if (!isGrounded) {
            coyoteTimer -= Time.deltaTime;
        }

        jumpBufferTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        groundCheckLockoutTimer -= Time.deltaTime;

        if (isDashing) {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) {
                isDashing = false;
                rb.gravityScale = defaultGravityScale;
            }
        }
    }

    private void TryJump() {
        if (jumpBufferTimer <= 0f || isDashing) {
            return;
        }

        bool canGroundJump = isGrounded || coyoteTimer > 0f;
        bool canAirJump = canDoubleJump && !canGroundJump && jumpsUsed < maxJumpCount;

        if (!canGroundJump && !canAirJump) {
            return;
        }

        if (canGroundJump) {
            jumpsUsed = 1;
        }
        else {
            jumpsUsed++;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isGrounded = false;
        groundCheckLockoutTimer = groundCheckLockoutAfterJump;
    }

    private void TryDash() {
        if (!canDash || !inputActions.Player.Dash.WasPressedThisFrame() || dashCooldownTimer > 0f || isDashing) {
            return;
        }

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);

        if (dashTrail != null) {
            dashTrail.Clear();
        }
    }

    private void ApplyHorizontalMovement() {
        float targetSpeed = moveInput * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float movement = Mathf.Clamp(speedDifference, -rate * Time.fixedDeltaTime, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    private void ApplyBetterJumpGravity() {
        rb.gravityScale = defaultGravityScale;
        bool jumpHeld = inputActions.Player.Jump.IsPressed();

        if (rb.linearVelocity.y < -0.01f) {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0.01f && !jumpHeld) {
            rb.gravityScale = defaultGravityScale * shortHopGravityMultiplier;
        }
    }

    private void UpdateVisuals() {
        if (spriteRenderer != null) {
            spriteRenderer.flipX = facingDirection < 0f;
        }

        if (dashTrail != null) {
            dashTrail.emitting = isDashing;
        }

        UpdateAnimator();
    }

    private void UpdateAnimator() {
        if (animator == null) {
            return;
        }
        bool isWalking = !isInDialogue && Mathf.Abs(moveInput) > 0.01f;

        animator.SetBool(IsWalkingHash, isWalking);
        animator.SetBool(IsInDialogueHash, isInDialogue);
    }

    private void AlignCollider() {
        if (!autoAlignCollider) {
            return;
        }

        if (boxCollider == null) {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        if (boxCollider != null) {
            boxCollider.size = bodySize;
            boxCollider.offset = bodyOffset;
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Vector2 checkCenter = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Gizmos.DrawWireCube(checkCenter, groundCheckSize);
    }

    private void OnValidate() {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        AlignCollider();
    }

    private void ApplyFrictionlessMaterial() {
        if (!useFrictionlessMaterial || boxCollider == null) {
            return;
        }

        frictionlessMaterial = new PhysicsMaterial2D("Player Frictionless") {
            friction = 0f,
            bounciness = 0f
        };
        boxCollider.sharedMaterial = frictionlessMaterial;
    }

    private bool IsGroundCollider(Collider2D hit) {
        if (hit == null || hit == boxCollider || hit.isTrigger) {
            return false;
        }

        if (hit.GetComponentInParent<PlatformerPlayerController>() != null) {
            return false;
        }

        return true;
    }

    public void SyncStateFrom(PlatformerPlayerController oldPlayer) {
        if (oldPlayer == null) return;

        this.facingDirection = oldPlayer.facingDirection;
        this.jumpsUsed = oldPlayer.jumpsUsed;
        this.coyoteTimer = oldPlayer.coyoteTimer;
        this.isGrounded = oldPlayer.isGrounded;
        this.jumpBufferTimer = oldPlayer.jumpBufferTimer;
        this.dashCooldownTimer = oldPlayer.dashCooldownTimer;

        if (this.rb != null && oldPlayer.rb != null) {
            this.rb.linearVelocity = oldPlayer.rb.linearVelocity;
        }
    }
}