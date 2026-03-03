using UnityEngine;

public class FierceTooth_Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f; // FierceTooth is slightly faster than Crab
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private string currentAnimState;
    private float lockAnimationTimer;

    public bool IsGrounded => isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        CheckGround();
        UpdateAnimations();
    }

    private void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            PlayAnimation("FierceTooth_Ground", 0.1f);
        }
    }

    public void Move(float direction)
    {
        if (lockAnimationTimer > 0) return;

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        if (direction > 0.1f) spriteRenderer.flipX = true; // Flips depend on sprite direction
        else if (direction < -0.1f) spriteRenderer.flipX = false;
    }

    public void Stop()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void UpdateAnimations()
    {
        if (lockAnimationTimer > 0)
        {
            lockAnimationTimer -= Time.deltaTime;
            return;
        }

        string newState = "";

        if (isGrounded)
        {
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                newState = "FierceTooth_Run";
            else
                newState = "FierceTooth_Idle";
        }
        else
        {
            if (rb.linearVelocity.y > 0.1f)
                newState = "FierceTooth_Jump";
            else
                newState = "FierceTooth_Fall";
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

    public bool IsDead()
    {
        return currentAnimState == "FierceTooth_DeadHit" || currentAnimState == "FierceTooth_DeadGround";
    }
}
