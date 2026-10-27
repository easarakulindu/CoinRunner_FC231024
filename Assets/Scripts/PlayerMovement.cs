using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;
    public Transform facingMarker;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private bool jumpRequested;
    private bool jumpReleased;

    public bool IsGrounded => groundCheck != null && rb != null && rb.linearVelocity.y <= 0.1f &&
        Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer) != null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        ReadInput(Input.GetAxisRaw("Horizontal"), Input.GetButtonDown("Jump"), Input.GetButtonUp("Jump"));
    }

    // Read every frame; apply velocity changes in the physics step.
    void ReadInput(float horizontal, bool pressed, bool released)
    {
        moveInput = horizontal;
        if (pressed && IsGrounded) jumpRequested = true;
        if (released) jumpReleased = true;
        if (horizontal != 0)
        {
            spriteRenderer.flipX = horizontal < 0;
            if (facingMarker != null)
            {
                Vector3 position = facingMarker.localPosition;
                position.x = Mathf.Abs(position.x) * Mathf.Sign(horizontal);
                facingMarker.localPosition = position;
            }
        }
    }

    void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        if (IsGrounded)
        {
            var support = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
            var platform = support.GetComponent<MovingPlatform>();
            if (platform != null) velocity.x += platform.Velocity.x;
        }
        if (jumpRequested && IsGrounded) velocity.y = jumpForce;
        if (jumpReleased && velocity.y > 0) velocity.y *= 0.5f;
        rb.linearVelocity = velocity;
        jumpRequested = false;
        jumpReleased = false;
    }

    public void ResetMotion()
    {
        moveInput = 0;
        jumpRequested = false;
        jumpReleased = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
