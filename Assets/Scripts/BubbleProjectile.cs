using UnityEngine;
using System;

public class BubbleProjectile : MonoBehaviour
{
    private Vector2 moveDirection;
    private float moveSpeed = 18f;
    private bool isFired = false;
    private BubbleGrid bubbleGrid;
    private GameManager gameManager;
    private Action onSnappedCallback;

    private float leftBoundary;
    private float rightBoundary;
    private float bubbleRadius;
    private Rigidbody2D rb2d;

    public void Launch(Vector2 direction, float speed, BubbleGrid grid, GameManager gm, Action onSnapped)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        bubbleGrid = grid;
        gameManager = gm;
        onSnappedCallback = onSnapped;

        if (bubbleGrid != null && bubbleGrid.targetCamera != null)
        {
            float screenHeight = bubbleGrid.targetCamera.orthographicSize * 2f;
            float screenWidth = screenHeight * bubbleGrid.targetCamera.aspect;
            leftBoundary = -screenWidth / 2f;
            rightBoundary = screenWidth / 2f;
            bubbleRadius = bubbleGrid.bubbleDiameter / 2f;
        }

        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }
        rb2d.bodyType = RigidbodyType2D.Dynamic;
        rb2d.gravityScale = 0f;
        rb2d.linearVelocity = moveDirection * moveSpeed;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) col.isTrigger = false;

        isFired = true;
    }

    void FixedUpdate()
    {
        if (!isFired || rb2d == null) return;

        rb2d.linearVelocity = moveDirection * moveSpeed;

        // Check Left/Right Wall Bounces
        float currentX = transform.position.x;
        if (currentX <= leftBoundary + bubbleRadius)
        {
            transform.position = new Vector3(leftBoundary + bubbleRadius, transform.position.y, transform.position.z);
            moveDirection.x = -moveDirection.x;
            rb2d.linearVelocity = moveDirection * moveSpeed;
        }
        else if (currentX >= rightBoundary - bubbleRadius)
        {
            transform.position = new Vector3(rightBoundary - bubbleRadius, transform.position.y, transform.position.z);
            moveDirection.x = -moveDirection.x;
            rb2d.linearVelocity = moveDirection * moveSpeed;
        }

        // Check Top Ceiling Collision
        if (bubbleGrid != null && transform.position.y >= bubbleGrid.startY)
        {
            SnapToGrid();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFired) return;

        if (collision.gameObject.GetComponent<Bubble>() != null || collision.transform.parent == bubbleGrid?.transform)
        {
            SnapToGrid();
        }
    }

    void SnapToGrid()
    {
        if (!isFired) return;
        isFired = false;

        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.bodyType = RigidbodyType2D.Kinematic;
        }

        if (bubbleGrid != null)
        {
            bubbleGrid.SnapAndRegisterBubble(gameObject);
        }

        // Trigger callback to spawn the next bubble
        onSnappedCallback?.Invoke();
    }
}
