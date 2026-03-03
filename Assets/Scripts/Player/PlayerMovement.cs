using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float bonusSpeed = 2f;
    [SerializeField] private float curseSlowFactor = 1.0f;
    [SerializeField] private CurseSystem curseSystem;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    public bool IsKnocked { get; private set; }
    private float knockbackTimer;
    
    private float currentMoveSpeed;
    private Vector2 moveInput;

    [Header("Air Physics")]
    [SerializeField] private float acceleration = 90f;
    [SerializeField] private float deceleration = 60f;
    [SerializeField] private float airAcceleration = 60f;
    [SerializeField] private float airDeceleration = 30f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [SerializeField] private float jumpCutMultiplier = 2f;
    [Range(0f, 1f)] [SerializeField] private float jumpApexThreshold = 2f;
    [SerializeField] private float jumpApexGravityMultiplier = 0.5f;

    [Header("Jump Forgiveness")]
    [SerializeField] private float coyoteTimeThreshold = 0.1f;
    [SerializeField] private float jumpBufferThreshold = 0.1f;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isJumping;
    private bool jumpButtonHeld;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Abilities")]
    [SerializeField] private bool hasDoubleJump = false;
    [SerializeField] private bool hasDash = false;
    private bool canDoubleJump;

    [Header("Dash Settings")]
    [SerializeField] private float dashPower = 20f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    [Header("Curse")]
    [SerializeField] private float curseIncreasePerHit = 5f;

    [Header("Corner Correction")]
    [SerializeField] private float ledgeDetectionRayLength = 0.5f;
    [SerializeField] private float cornerCorrectionAmount = 0.1f;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Camera mainCamera;

    public bool IsGrounded { get; private set; }
    private float distanceTravelled;

    public event Action<int, int> OnHealthChanged;
    public event Action<string> OnAbilityUnlocked;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentMoveSpeed = baseMoveSpeed;
        rb.gravityScale = gravityScale;
        currentHealth = maxHealth;
    }

    void Start()
    {
        mainCamera = Camera.main;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (isDashing) return;

        TrackDistance();
        CheckGround();
        CheckIfOutOfCamera();
        HandleKnockbackTimer();
        UpdateAnimations();
        
        // Timers
        if (IsGrounded)
        {
            coyoteTimer = coyoteTimeThreshold;
            canDoubleJump = true;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        jumpBufferTimer -= Time.deltaTime;

        // Jump Execution
        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                ExecuteJump();
            }
            else if (hasDoubleJump && canDoubleJump)
            {
                ExecuteJump(true);
            }
        }

        HandleGravityScale();
    }

    private string currentAnimState;
    private float lockAnimationTimer;

    private void UpdateAnimations()
    {
        if (animator == null || !enabled) return;

        if (lockAnimationTimer > 0)
        {
            lockAnimationTimer -= Time.deltaTime;
            return;
        }

        if (IsKnocked || isDashing) return;

        string newState = "";

        if (IsGrounded)
        {
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                newState = "Run";
            else
                newState = "Idle";
        }
        else
        {
            if (rb.linearVelocity.y > 0.1f)
                newState = "Jump";
            else if (rb.linearVelocity.y < -0.1f)
                newState = "Fall";
        }

        PlayAnimation(newState);
    }

    public void PlayAnimation(string newState, float lockDuration = 0f)
    {
        if (newState == "" || newState == currentAnimState) return;

        currentAnimState = newState;
        animator.Play(newState);
        
        if (lockDuration > 0)
        {
            lockAnimationTimer = lockDuration;
        }
    }

    private void HandleKnockbackTimer()
    {
        if (!IsKnocked) return;
        
        knockbackTimer -= Time.deltaTime;

        if (knockbackTimer <= 0f)
        {
            IsKnocked = false;
        }
    }

    private void CheckIfOutOfCamera()
    {
        if (!GameManager.Instance.GameStarted) return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        if (viewportPos.x < -1.5f)
        {
            Die();
        }
    }

    #region Movement

    private void HandleGravityScale()
    {
        if (IsKnocked || isDashing) return;

        if (rb.linearVelocity.y < 0) // Falling
        {
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0 && !jumpButtonHeld) // Jump Cut
        {
            rb.gravityScale = gravityScale * jumpCutMultiplier;
        }
        else if (Mathf.Abs(rb.linearVelocity.y) < jumpApexThreshold) // Apex
        {
            rb.gravityScale = gravityScale * jumpApexGravityMultiplier;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        if (!GameManager.Instance.GameStarted)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        if (!IsKnocked)
        {
            HandleMovement();
            ApplyCornerCorrection();
        }

        ApplySpriteFlip();
    }

    private void ApplyCornerCorrection()
    {
        if (!IsGrounded && Mathf.Abs(moveInput.x) > 0.1f && rb.linearVelocity.y < 2f)
        {
            float sideSign = Mathf.Sign(moveInput.x);
            Vector2 sideDirection = Vector2.right * sideSign;

            Vector2 rayOriginBase = (Vector2)transform.position + new Vector2(0, -0.4f);
            Vector2 rayOriginMid = (Vector2)transform.position + new Vector2(0, 0.2f);
            Vector2 rayOriginHigh = (Vector2)transform.position + new Vector2(0, 0.6f);

            RaycastHit2D hitLow = Physics2D.Raycast(rayOriginBase, sideDirection, ledgeDetectionRayLength, groundLayer);
            RaycastHit2D hitMid = Physics2D.Raycast(rayOriginMid, sideDirection, ledgeDetectionRayLength, groundLayer);
            RaycastHit2D hitHigh = Physics2D.Raycast(rayOriginHigh, sideDirection, ledgeDetectionRayLength, groundLayer);

            if (!hitHigh.collider && (hitLow.collider || hitMid.collider))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 6f);
            }
        }

        if (rb.linearVelocity.y > 0)
        {
            Vector2 headLeft = (Vector2)transform.position + new Vector2(-0.35f, 0.9f);
            Vector2 headRight = (Vector2)transform.position + new Vector2(0.35f, 0.9f);
            Vector2 headCenter = (Vector2)transform.position + new Vector2(0, 0.9f);

            RaycastHit2D hitEdgeLeft = Physics2D.Raycast(headLeft, Vector2.up, 0.25f, groundLayer);
            RaycastHit2D hitEdgeRight = Physics2D.Raycast(headRight, Vector2.up, 0.25f, groundLayer);
            RaycastHit2D hitCenter = Physics2D.Raycast(headCenter, Vector2.up, 0.25f, groundLayer);

            if (!hitCenter.collider)
            {
                if (hitEdgeLeft.collider && !hitEdgeRight.collider)
                {
                    rb.MovePosition(rb.position + Vector2.right * cornerCorrectionAmount);
                }
                else if (hitEdgeRight.collider && !hitEdgeLeft.collider)
                {
                    rb.MovePosition(rb.position + Vector2.left * cornerCorrectionAmount);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundLayer == 0) return;

        Gizmos.color = Color.red;
        float sideSign = (spriteRenderer != null && spriteRenderer.flipX) ? -1 : 1;
        Vector2 sideDir = Vector2.right * sideSign;

        Gizmos.DrawRay((Vector2)transform.position + new Vector2(0, -0.4f), sideDir * ledgeDetectionRayLength);
        Gizmos.DrawRay((Vector2)transform.position + new Vector2(0, 0.2f), sideDir * ledgeDetectionRayLength);
        Gizmos.DrawRay((Vector2)transform.position + new Vector2(0, 0.6f), sideDir * ledgeDetectionRayLength);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay((Vector2)transform.position + new Vector2(-0.35f, 0.9f), Vector2.up * 0.25f);
        Gizmos.DrawRay((Vector2)transform.position + new Vector2(0.35f, 0.9f), Vector2.up * 0.25f);
        Gizmos.DrawRay((Vector2)transform.position + new Vector2(0, 0.9f), Vector2.up * 0.25f);
    }

    private void ApplySpriteFlip()
    {
        if (moveInput.x > 0.1f) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput.x < -0.1f) transform.localScale = new Vector3(-1, 1, 1);
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        jumpButtonHeld = value.isPressed;
        if (value.isPressed) jumpBufferTimer = jumpBufferThreshold;
    }

    void OnDash(InputValue value)
    {
        if (value.isPressed && hasDash && canDash && !isDashing)
        {
            StartCoroutine(Dash());
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        
        float dashDir = spriteRenderer.flipX ? -1f : 1f;
        rb.linearVelocity = new Vector2(dashDir * dashPower, 0f);
        AudioManagerController.Instance?.PlaySFXRandomPitch(AudioManagerController.Instance.playerDashSFX);

        PlayAnimation("Run"); // Dash visual logic can be expanded

        yield return new WaitForSeconds(dashTime);
        
        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void ExecuteJump(bool isDoubleJump = false)
    {
        isJumping = true;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        
        if (isDoubleJump)
        {
            canDoubleJump = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.9f); // Slightly weaker double jump
            PlayAnimation("Jump", 0.1f);
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            PlayAnimation("Jump");
        }
    }

    private void CheckGround()
    {
        bool wasGrounded = IsGrounded;
        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (IsGrounded)
        {
            isJumping = false;
            if (!wasGrounded) PlayAnimation("Ground", 0.1f);
        }
    }

    private void HandleMovement()
    {
        float cursePercent = curseSystem.GetCurseNormalized();
        float cursePenalty = Mathf.Lerp(1f, 0.4f, cursePercent * curseSlowFactor);
        float targetMaxSpeed = (baseMoveSpeed * cursePenalty + bonusSpeed) * moveInput.x;

        float accelRate = 0f;
        if (IsGrounded)
            accelRate = Mathf.Abs(targetMaxSpeed) > 0.01f ? acceleration : deceleration;
        else
            accelRate = Mathf.Abs(targetMaxSpeed) > 0.01f ? airAcceleration : airDeceleration;

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetMaxSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        currentMoveSpeed = Mathf.Abs(rb.linearVelocity.x);
    }

    public void UnlockDash() 
    { 
        hasDash = true; 
        OnAbilityUnlocked?.Invoke("Dash");
    }
    public void UnlockDoubleJump() 
    { 
        hasDoubleJump = true; 
        OnAbilityUnlocked?.Invoke("DoubleJump");
    }

    public bool HasDash() => hasDash;
    public bool HasDoubleJump() => hasDoubleJump;

    public void TakeDamage(int damage, Vector2 knockbackDir)
    {
        if (IsKnocked || isDashing) return;
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        curseSystem.IncreaseCurseRate(curseIncreasePerHit * 2f);
        ApplyKnockback(knockbackDir);
        if (currentHealth <= 0) Die();
    }

    public void ApplyKnockback(Vector2 direction)
    {
        IsKnocked = true;
        knockbackTimer = knockbackDuration;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction.normalized * knockbackForce, ForceMode2D.Impulse);
        PlayAnimation("Hit", 0.2f);
    }

    #endregion

    #region Distance Tracking
    private void TrackDistance()
    {
        distanceTravelled += Mathf.Abs(rb.linearVelocity.x) * Time.deltaTime;
    }

    public float GetDistanceTravelled() { return distanceTravelled; }
    #endregion

    #region Difficulty Control
    public void IncreaseSpeed(float amount) { currentMoveSpeed += amount; }
    public float GetCurrentSpeed() { return currentMoveSpeed; }
    #endregion

    #region Player States
    public void Die()
    {
        CameraBounds.Instance?.StopShake();
        Debug.Log("Player morreu!");
        enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (IsGrounded) PlayAnimation("DeadGround");
        else PlayAnimation("DeadHit");
        GameManager.Instance.EndGame();
    }

    public void Heal(int amount)
    {
        if (IsFullHealth()) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        AudioManagerController.Instance?.PlaySFX(AudioManagerController.Instance.playerHealSFX);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log("Player healed by " + amount + " HP. Current HP: " + currentHealth);
    }

    public bool IsFullHealth() => currentHealth >= maxHealth;
    #endregion

    #region Collision
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            curseSystem.IncreaseCurseRate(curseIncreasePerHit);
        }
    }
    #endregion

    // #region GUI
    // private void OnGUI()
    // {
    //     if (!Camera.main || !enabled) return;

    //     Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
    //     if (screenPos.z < 0) return;

    //     float speed = currentMoveSpeed;
    //     float curseValue = curseSystem.GetCurseNormalized() * 100f;
    //     float distance = distanceTravelled;
    //     float toBoss = Mathf.Max(0, ChunckLooper.Instance.GetDistanceToBoss());
    //     string anim = string.IsNullOrEmpty(currentAnimState) ? "None" : currentAnimState;
    //     string hpColor = currentHealth <= 1 ? "#FF3333" : "#00FF00";

    //     GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
    //     boxStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.8f));

    //     GUIStyle baseLabel = new GUIStyle();
    //     baseLabel.fontSize = 24;
    //     baseLabel.normal.textColor = Color.white;
    //     baseLabel.alignment = TextAnchor.MiddleLeft;
    //     baseLabel.padding = new RectOffset(15, 15, 5, 5);
    //     baseLabel.richText = true;

    //     GUIStyle headerStyle = new GUIStyle(baseLabel);
    //     headerStyle.fontSize = 20;
    //     headerStyle.alignment = TextAnchor.MiddleCenter;
    //     headerStyle.fontStyle = FontStyle.Bold;

    //     float width = 350;
    //     float height = 320;
    //     Rect rect = new Rect(screenPos.x - (width / 2), Screen.height - screenPos.y - (height + 60), width, height);

    //     GUI.Box(rect, "", boxStyle);

    //     GUILayout.BeginArea(rect);
    //     GUILayout.BeginVertical();
    //     GUILayout.Space(10);
        
    //     GUILayout.Label("STATS", headerStyle);
    //     GUILayout.Label($"HP: <color={hpColor}>{currentHealth}/{maxHealth}</color> | Spd: <color=yellow>{speed:F1}</color>", baseLabel);
    //     GUILayout.Label($"Curse: <color=#FF3333>{curseValue:F0}%</color> | Boss: <color=orange>{toBoss:F0}m</color>", baseLabel);
        
    //     GUILayout.Space(10);
    //     GUILayout.Label("SKILLS", headerStyle);
    //     string dashTxt = hasDash ? "<color=#00FFFF>Dash [SHIFT]</color>" : "<color=#444444>Dash [Locked]</color>";
    //     string djTxt = hasDoubleJump ? "<color=#FFFF00>Double Jump</color>" : "<color=#444444>Double Jump [Locked]</color>";
    //     GUILayout.Label($"{dashTxt} | {djTxt}", baseLabel);

    //     GUILayout.Space(10);
    //     GUILayout.Label("STATES", headerStyle);
        
    //     string groundColor = IsGrounded ? "#00FF00" : "#FF3333";
    //     string jumpTxt = isJumping ? "<color=yellow>JUMPING</color>" : "<color=#CCCCCC>Floor</color>";
    //     string statusTxt = isDashing ? "<color=#00FFFF>DASHING</color>" : (IsKnocked ? "<color=red>KNOCKED</color>" : "Normal");

    //     GUILayout.Label($"Grounded: <color={groundColor}>{IsGrounded}</color> | {jumpTxt}", baseLabel);
    //     GUILayout.Label($"Status: {statusTxt} | Anim: {anim}", baseLabel);

    //     GUILayout.EndVertical();
    //     GUILayout.EndArea();
    // }

    // private Texture2D MakeTex(int width, int height, Color col)
    // {
    //     Color[] pix = new Color[width * height];
    //     for (int i = 0; i < pix.Length; ++i) pix[i] = col;
    //     Texture2D result = new Texture2D(width, height);
    //     result.SetPixels(pix);
    //     result.Apply();
    //     return result;
    // }
    // #endregion
}
