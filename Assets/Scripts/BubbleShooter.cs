using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleShooter : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public BubbleGrid bubbleGrid;
    public GameObject bubblePrefab;
    public LineRenderer aimLineRenderer;

    [Header("Shooter Settings")]
    public Transform shooterPosition;
    [Tooltip("Minimum angle in degrees from the horizontal right")]
    public float minAngle = 15f;
    [Tooltip("Maximum angle in degrees from the horizontal right")]
    public float maxAngle = 165f;
    public float aimLineLength = 3f;

    private GameObject currentBubbleInstance;
    private GameObject nextBubbleInstance;
    private Color currentBubbleColor;
    private Color nextBubbleColor;

    [Header("Preview Settings")]
    public Transform nextBubblePosition;

    private bool isDragging = false;
    private Vector3 lastClampedDir = Vector3.up;
    private bool canShoot = true;

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (bubbleGrid == null)
        {
            bubbleGrid = FindAnyObjectByType<BubbleGrid>();
        }

        if (gameManager != null)
        {
            gameManager.OnQueueUpdated += HandleQueueUpdated;
            currentBubbleColor = gameManager.currentBubbleColor;
            nextBubbleColor = gameManager.nextBubbleColor;
        }

        SetupAimLine();
        SpawnCurrentBubble();
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnQueueUpdated -= HandleQueueUpdated;
        }
    }

    void Update()
    {
        if (gameManager != null && (gameManager.isGameOver || gameManager.isPaused))
        {
            if (aimLineRenderer != null) aimLineRenderer.enabled = false;
            return;
        }

        if (canShoot)
        {
            HandleAimingAndFiring();
        }
        else
        {
            if (aimLineRenderer != null) aimLineRenderer.enabled = false;
        }
    }

    void SetupAimLine()
    {
        if (aimLineRenderer != null)
        {
            aimLineRenderer.positionCount = 2;
            aimLineRenderer.startWidth = 0.05f;
            aimLineRenderer.endWidth = 0.05f;
            aimLineRenderer.useWorldSpace = true;
            aimLineRenderer.textureMode = LineTextureMode.Tile;
        }
    }

    void HandleQueueUpdated(Color current, Color next)
    {
        currentBubbleColor = current;
        nextBubbleColor = next;

        if (currentBubbleInstance != null && canShoot)
        {
            SpriteRenderer sr = currentBubbleInstance.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = currentBubbleColor;

            Bubble bubbleComp = currentBubbleInstance.GetComponent<Bubble>();
            if (bubbleComp != null) bubbleComp.assignedColor = currentBubbleColor;
        }

        UpdateNextBubblePreview();
    }

    void UpdateNextBubblePreview()
    {
        if (bubblePrefab == null) return;

        Vector3 previewPos = nextBubblePosition != null ? nextBubblePosition.position : (shooterPosition != null ? shooterPosition.position + new Vector3(-1.5f, 0f, 0f) : new Vector3(-1.5f, -4.0f, 0f));

        if (nextBubbleInstance == null)
        {
            nextBubbleInstance = Instantiate(bubblePrefab, previewPos, Quaternion.identity);
            // Disable collider/projectile scripts on preview so it doesn't interfere with gameplay
            BubbleProjectile proj = nextBubbleInstance.GetComponent<BubbleProjectile>();
            if (proj != null) Destroy(proj);
            Collider2D col = nextBubbleInstance.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
        else
        {
            nextBubbleInstance.transform.position = previewPos;
        }

        if (bubbleGrid != null)
        {
            nextBubbleInstance.transform.localScale = Vector3.one * (bubbleGrid.bubbleDiameter * 0.75f);
        }

        SpriteRenderer sr = nextBubbleInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = nextBubbleColor;
        }

        Bubble bubbleComp = nextBubbleInstance.GetComponent<Bubble>();
        if (bubbleComp != null)
        {
            bubbleComp.assignedColor = nextBubbleColor;
        }
    }

    void SpawnCurrentBubble()
    {
        if (bubblePrefab == null) return;

        Vector3 spawnPos = shooterPosition != null ? shooterPosition.position : new Vector3(0f, -4.0f, 0f);

        if (currentBubbleInstance != null)
        {
            Destroy(currentBubbleInstance);
        }

        currentBubbleInstance = Instantiate(bubblePrefab, spawnPos, Quaternion.identity);

        if (bubbleGrid != null)
        {
            currentBubbleInstance.transform.localScale = Vector3.one * (bubbleGrid.bubbleDiameter * 0.9f);
        }

        SpriteRenderer sr = currentBubbleInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = currentBubbleColor;
        }

        canShoot = true;
    }

    void HandleAimingAndFiring()
    {
        // Ignore input only if pointer/touch is over an interactive UI element (such as the pause button),
        // but do not block non-interactive UI (like the score text) and do not hide the aim line.
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);

            if (Mouse.current != null)
            {
                eventData.position = Mouse.current.position.ReadValue();
                var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

                foreach (var result in results)
                {
                    if (result.gameObject != null)
                    {
                        // Check if the hit UI element is an interactive button or specifically a pause element/button
                        if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null ||
                            result.gameObject.name.ToLower().Contains("pause") ||
                            result.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null)
                        {
                            isDragging = false;
                            return; // Ignore click on interactive buttons only, allow aim line to remain visible
                        }
                    }
                }
            }

            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                var touch = Touchscreen.current.touches[0];
                eventData.position = touch.position.ReadValue();
                var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

                foreach (var result in results)
                {
                    if (result.gameObject != null)
                    {
                        if (result.gameObject.GetComponent<UnityEngine.UI.Button>() != null ||
                            result.gameObject.name.ToLower().Contains("pause") ||
                            result.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null)
                        {
                            isDragging = false;
                            return;
                        }
                    }
                }
            }
        }

        bool isPressed = false;
        bool wasReleased = false;
        Vector2 screenPos = Vector2.zero;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                isPressed = true;
                screenPos = Mouse.current.position.ReadValue();
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                wasReleased = true;
            }
        }

        if (!isPressed && Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            var phase = touch.phase.ReadValue();

            if (phase == UnityEngine.InputSystem.TouchPhase.Began ||
                phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                isPressed = true;
                screenPos = touch.position.ReadValue();
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                wasReleased = true;
            }
        }

        Vector3 origin = shooterPosition != null ? shooterPosition.position : new Vector3(0f, -4.0f, 0f);

        if (isPressed)
        {
            isDragging = true;
            if (Camera.main != null)
            {
                Vector3 inputWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                inputWorldPos.z = 0f;

                Vector3 direction = inputWorldPos - origin;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;

                angle = Mathf.Clamp(angle, minAngle, maxAngle);

                float clampedRad = angle * Mathf.Deg2Rad;
                lastClampedDir = new Vector3(Mathf.Cos(clampedRad), Mathf.Sin(clampedRad), 0f);
            }
        }

        if (aimLineRenderer != null)
        {
            aimLineRenderer.enabled = true;
            float offsetRadius = (bubbleGrid != null) ? bubbleGrid.bubbleDiameter * 0.5f : 0.3f;
            aimLineRenderer.SetPosition(0, origin + (lastClampedDir * offsetRadius));
            aimLineRenderer.SetPosition(1, origin + (lastClampedDir * aimLineLength));
        }

        if (wasReleased && isDragging)
        {
            isDragging = false;
            FireBubble(lastClampedDir);
        }
    }

    void FireBubble(Vector3 direction)
    {
        if (currentBubbleInstance == null || !canShoot) return;

        canShoot = false; // Lock shooting until the projectile lands

        BubbleProjectile projectile = currentBubbleInstance.AddComponent<BubbleProjectile>();
        SpriteRenderer sr = currentBubbleInstance.GetComponent<SpriteRenderer>();
        Color firedColor = sr != null ? sr.color : currentBubbleColor;

        Bubble bubbleComp = currentBubbleInstance.GetComponent<Bubble>();
        if (bubbleComp != null)
        {
            bubbleComp.assignedColor = firedColor;
        }

        projectile.Launch(direction, 18f, bubbleGrid, gameManager, () => {
            if (gameManager != null)
            {
                gameManager.OnBubbleFinishedProcessing();
            }
            SpawnCurrentBubble();
        });

        currentBubbleInstance = null;
    }
}
