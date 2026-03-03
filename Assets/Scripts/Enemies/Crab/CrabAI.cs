using UnityEngine;

public class CrabAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float edgeCheckOffset = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    private CrabMovement movement;
    private EnemyData enemyData;
    private int direction = 1;

    void Awake()
    {
        movement = GetComponent<CrabMovement>();
        enemyData = GetComponent<EnemyData>();
    }

    void Update()
    {
        if (movement.IsDead()) return;

        if (movement.IsGrounded)
        {
            HandlePatrol();
        }
        else
        {
            movement.Stop();
        }
    }

    private void HandlePatrol()
    {
        // Check for walls
        Vector2 wallOrigin = (Vector2)transform.position + Vector2.up * 0.5f;
        RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * direction, wallCheckDistance, groundLayer);

        // Check for edges
        Vector2 edgeOrigin = (Vector2)transform.position + new Vector2(edgeCheckOffset * direction, 0.1f);
        RaycastHit2D edgeHit = Physics2D.Raycast(edgeOrigin, Vector2.down, 1f, groundLayer);

        // Check for tile boundaries
        bool boundaryHit = false;
        if (enemyData != null && enemyData.ParentTile != null)
        {
            Bounds tileBounds = enemyData.ParentTile.GetBounds();
            if (direction > 0 && transform.position.x > tileBounds.max.x - 0.5f) boundaryHit = true;
            else if (direction < 0 && transform.position.x < tileBounds.min.x + 0.5f) boundaryHit = true;
        }

        if (wallHit.collider != null || edgeHit.collider == null || boundaryHit)
        {
            FlipDirection();
        }

        movement.Move(direction);
    }

    private void FlipDirection()
    {
        direction *= -1;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Se colidir com outro inimigo, inverte a direção
        if (collision.gameObject.CompareTag("Enemy"))
        {
            FlipDirection();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 wallOrigin = (Vector2)transform.position + Vector2.up * 0.5f;
        Gizmos.DrawRay(wallOrigin, Vector2.right * direction * wallCheckDistance);

        Gizmos.color = Color.yellow;
        Vector2 edgeOrigin = (Vector2)transform.position + new Vector2(edgeCheckOffset * direction, 0.1f);
        Gizmos.DrawRay(edgeOrigin, Vector2.down * 1f);
    }
}