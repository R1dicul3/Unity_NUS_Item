using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlatformerPlayerController : MonoBehaviour
{
    //移动速度和加速度
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 70f;
    [SerializeField] private float deceleration = 85f;

    //跳跃
    [Header("Jump")]
    [SerializeField] private float jumpVelocity = 14f;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private bool canDoubleJump = true;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallGravityMultiplier = 1.55f;
    [SerializeField] private float shortHopGravityMultiplier = 2.2f;

    //冲刺
    [Header("Dash")]
    [SerializeField] private bool canDash = true;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.16f;
    [SerializeField] private float dashCooldown = 0.55f;

    //判断触地
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.78f, 0.12f);
    [SerializeField] private float groundCheckOffset = 0.53f;
    [SerializeField] private float groundCheckLockoutAfterJump = 0.08f;

    [Header("Control State")]
    [SerializeField] private bool isControlled = true;
    [SerializeField] private Color controlledColor = new Color(1f, 0.82f, 0.32f);
    [SerializeField] private Color inactiveColor = new Color(0.38f, 0.46f, 0.55f);

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer dashTrail;

    private float moveInput;
    private float facingDirection = 1f;
    private int jumpsUsed;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isDashing;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float groundCheckLockoutTimer;
    private float defaultGravityScale;

    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsControlled => isControlled;
    public bool CanDoubleJump => canDoubleJump;
    public bool CanDash => canDash;
    public Collider2D BodyCollider => boxCollider;

    public void Initialize(LayerMask platformMask)
    {
        groundMask = platformMask;
    }

    public void Initialize(LayerMask platformMask, Color activeColor, Color idleColor)
    {
        groundMask = platformMask;
        controlledColor = activeColor;
        inactiveColor = idleColor;
    }

    public void Initialize(LayerMask platformMask, Color activeColor, Color idleColor, bool allowDoubleJump, bool allowDash)
    {
        groundMask = platformMask;
        controlledColor = activeColor;
        inactiveColor = idleColor;
        SetAbilities(allowDoubleJump, allowDash, activeColor);
    }

    public void SetAbilities(bool allowDoubleJump, bool allowDash, Color activeColor)
    {
        canDoubleJump = allowDoubleJump;
        canDash = allowDash;
        controlledColor = activeColor;
        maxJumpCount = canDoubleJump ? 2 : 1;

        if (!canDash && isDashing)
        {
            isDashing = false;
            rb.gravityScale = defaultGravityScale;
        }

        if (!canDoubleJump && jumpsUsed > 1)
        {
            jumpsUsed = 1;
        }

        UpdateVisuals();
    }

    public void SetControlled(bool value)
    {
        isControlled = value;

        if (!isControlled)
        {
            moveInput = 0f;
            jumpBufferTimer = 0f;
            isDashing = false;
            rb.gravityScale = defaultGravityScale;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        UpdateVisuals();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        dashTrail = GetComponent<TrailRenderer>();
        defaultGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        UpdateGroundState();
        UpdateTimers();

        if (isControlled)
        {
            ReadInput();
            TryJump();
            TryDash();
        }
        else
        {
            moveInput = 0f;
            jumpBufferTimer = 0f;
        }

        UpdateVisuals();
    }

    //处理物理逻辑
    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);
            return;
        }

        //左右移动手感优化
        ApplyHorizontalMovement();
        //跳跃手感优化
        ApplyBetterJumpGravity();
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = 0f;
            return;
        }

        float left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f;
        float right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f;
        moveInput = right - left;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            facingDirection = Mathf.Sign(moveInput);
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpBufferTimer = jumpBufferTime;
        }
    }

    //判断触地
    private void UpdateGroundState()
    {
        wasGrounded = isGrounded;
        if (groundCheckLockoutTimer > 0f || rb.linearVelocity.y > 0.05f)
        {
            isGrounded = false;
            return;
        }

        Vector2 checkCenter = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        isGrounded = false;

        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, groundCheckSize, 0f, groundMask);
        foreach (Collider2D hit in hits)
        {
            if (IsGroundCollider(hit))
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            jumpsUsed = 0;
        }

        if (!wasGrounded && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, 0f));
        }
    }

    private void UpdateTimers()
    {
        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }

        jumpBufferTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        groundCheckLockoutTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.gravityScale = defaultGravityScale;
            }
        }
    }

    private void TryJump()
    {
        if (jumpBufferTimer <= 0f || isDashing)
        {
            return;
        }

        bool canGroundJump = isGrounded || coyoteTimer > 0f;
        bool canAirJump = canDoubleJump && !canGroundJump && jumpsUsed < maxJumpCount;

        if (!canGroundJump && !canAirJump)
        {
            return;
        }

        if (canGroundJump)
        {
            jumpsUsed = 1;
        }
        else
        {
            jumpsUsed++;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isGrounded = false;
        groundCheckLockoutTimer = groundCheckLockoutAfterJump;
    }

    private void TryDash()
    {
        Keyboard keyboard = Keyboard.current;
        if (!canDash || keyboard == null || !keyboard.leftShiftKey.wasPressedThisFrame || dashCooldownTimer > 0f || isDashing)
        {
            return;
        }

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);

        if (dashTrail != null)
        {
            dashTrail.Clear();
        }
    }

    private void ApplyHorizontalMovement()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float movement = Mathf.Clamp(speedDifference, -rate * Time.fixedDeltaTime, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    private void ApplyBetterJumpGravity()
    {
        rb.gravityScale = defaultGravityScale;
        Keyboard keyboard = Keyboard.current;
        bool jumpHeld = keyboard != null && keyboard.spaceKey.isPressed;

        if (rb.linearVelocity.y < -0.01f)
        {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0.01f && !jumpHeld)
        {
            rb.gravityScale = defaultGravityScale * shortHopGravityMultiplier;
        }
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingDirection < 0f;
            if (isDashing && isControlled)
            {
                spriteRenderer.color = new Color(0.55f, 0.92f, 1f);
            }
            else
            {
                spriteRenderer.color = isControlled ? controlledColor : inactiveColor;
            }
        }

        if (dashTrail != null)
        {
            dashTrail.emitting = isDashing;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 checkCenter = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Gizmos.DrawWireCube(checkCenter, groundCheckSize);
    }

    private bool IsGroundCollider(Collider2D hit)
    {
        if (hit == null || hit == boxCollider)
        {
            return false;
        }

        return hit.GetComponentInParent<PlatformerPlayerController>() == null;
    }
}
