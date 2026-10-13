using UnityEngine;
using System.Collections.Generic;

public class BubbleGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int columns = 8;
    public int initialRows = 6;

    [Header("Effects")]
    public GameObject bubblePopEffectPrefab;

    [Header("Layout & Borders")]
    public Camera targetCamera;
    [Header("Danger Line")]
    public float dangerLineY = -4.0f;

    [HideInInspector]
    public float bubbleDiameter = 1.0f;
    public float rowHeightMultiplier = 0.866f; // sqrt(3) / 2 for tight hexagonal packing
    [HideInInspector]
    public float startY = 4.0f;

    // Dictionary to store bubbles by their grid coordinates (row, col)
    private Dictionary<Vector2Int, GameObject> gridMap = new Dictionary<Vector2Int, GameObject>();

    // Available bubble colors/types
    public Color[] bubbleColors = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    public delegate void GameOverAction();
    public event GameOverAction OnGameOver;

    void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        CalculateLayout();
    }

    public void CalculateLayout()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        float screenHeight = targetCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * targetCamera.aspect;

        bubbleDiameter = screenWidth / (columns + 0.5f);
        startY = screenHeight / 2 - bubbleDiameter / 2;
    }

    public Vector3 GetWorldPosition(int row, int col)
    {
        CalculateLayout();

        float screenHeight = targetCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * targetCamera.aspect;
        float leftBoundary = -screenWidth / 2f;

        float xOffset = (row % 2 != 0) ? (bubbleDiameter / 2f) : 0f;

        float x = leftBoundary + (col * bubbleDiameter) + xOffset + (bubbleDiameter / 2f);
        float y = startY - (row * (bubbleDiameter * rowHeightMultiplier));

        return new Vector3(x, y, 0f);
    }

    public void GenerateInitialGrid(GameObject bubblePrefab)
    {
        ClearGrid();
        CalculateLayout();

        for (int row = 0; row < initialRows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                SpawnBubbleAt(row, col, bubblePrefab);
            }
        }
    }

    private GameObject SpawnBubbleAt(int row, int col, GameObject bubblePrefab)
    {
        Vector3 pos = GetWorldPosition(row, col);
        GameObject bubbleObj = Instantiate(bubblePrefab, pos, Quaternion.identity, transform);

        // Make bubbles slightly smaller (e.g. 0.9f of bubbleDiameter) so they have gaps between them while fitting the screen perfectly
        bubbleObj.transform.localScale = Vector3.one * (bubbleDiameter * 0.9f);

        CircleCollider2D col2d = bubbleObj.GetComponent<CircleCollider2D>();
        if (col2d != null) col2d.isTrigger = false;

        Rigidbody2D rb = bubbleObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = bubbleObj.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        SpriteRenderer sr = bubbleObj.GetComponent<SpriteRenderer>();
        if (sr != null && bubbleColors.Length > 0)
        {
            Color randomColor = bubbleColors[Random.Range(0, bubbleColors.Length)];
            sr.color = randomColor;
        }

        Bubble bubbleComp = bubbleObj.GetComponent<Bubble>();
        if (bubbleComp != null)
        {
            bubbleComp.row = row;
            bubbleComp.col = col;
            bubbleComp.assignedColor = sr != null ? sr.color : Color.white;
        }

        bubbleObj.tag = "Bubble";
        gridMap[new Vector2Int(row, col)] = bubbleObj;
        return bubbleObj;
    }

    public List<Vector2Int> GetNeighbors(int row, int col)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        bool isOddRow = (row % 2 != 0);

        Vector2Int[] directionsEven = new Vector2Int[]
        {
            new Vector2Int(-1, -1), new Vector2Int(-1, 0),
            new Vector2Int(0, -1),  new Vector2Int(0, 1),
            new Vector2Int(1, -1),  new Vector2Int(1, 0)
        };

        Vector2Int[] directionsOdd = new Vector2Int[]
        {
            new Vector2Int(-1, 0),  new Vector2Int(-1, 1),
            new Vector2Int(0, -1),  new Vector2Int(0, 1),
            new Vector2Int(1, 0),   new Vector2Int(1, 1)
        };

        Vector2Int[] directions = isOddRow ? directionsOdd : directionsEven;

        foreach (var dir in directions)
        {
            int nRow = row + dir.x;
            int nCol = col + dir.y;

            if (nRow >= 0 && nCol >= 0 && nCol < columns)
            {
                neighbors.Add(new Vector2Int(nRow, nCol));
            }
        }

        return neighbors;
    }

    public void SnapAndRegisterBubble(GameObject bubbleObj)
    {
        Bubble bubbleComp = bubbleObj.GetComponent<Bubble>();
        Vector3 worldPos = bubbleObj.transform.position;

        int bestRow = 0;
        int bestCol = 0;
        float minDistance = float.MaxValue;

        // Find max existing row in grid to bound our search reasonably
        int maxFoundRow = 0;
        foreach (var coord in gridMap.Keys)
        {
            if (coord.x > maxFoundRow) maxFoundRow = coord.x;
        }

        for (int row = 0; row <= maxFoundRow + 3; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (gridMap.ContainsKey(new Vector2Int(row, col))) continue;

                Vector3 slotPos = GetWorldPosition(row, col);
                float dist = Vector3.Distance(worldPos, slotPos);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestRow = row;
                    bestCol = col;
                }
            }
        }

        Vector3 finalPos = GetWorldPosition(bestRow, bestCol);
        bubbleObj.transform.position = finalPos;
        bubbleObj.transform.localScale = Vector3.one * (bubbleDiameter * 0.9f);

        if (finalPos.y <= dangerLineY)
        {
            TriggerGameOver();
        }

        if (finalPos.y <= dangerLineY)
        {
            TriggerGameOver();
        }

        if (bubbleComp != null)
        {
            bubbleComp.row = bestRow;
            bubbleComp.col = bestCol;
        }

        gridMap[new Vector2Int(bestRow, bestCol)] = bubbleObj;
        bubbleObj.transform.SetParent(transform);
        bubbleObj.tag = "Bubble";

        Rigidbody2D rb = bubbleObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        ProcessBubbleLanding(bestRow, bestCol);
    }

    public void ProcessBubbleLanding(int snappedRow, int snappedCol)
    {
        Vector2Int snappedCoord = new Vector2Int(snappedRow, snappedCol);
        if (!gridMap.ContainsKey(snappedCoord)) return;

        GameObject landedBubble = gridMap[snappedCoord];
        Bubble landedComp = landedBubble.GetComponent<Bubble>();
        if (landedComp == null) return;

        Color targetColor = landedComp.assignedColor;

        List<Vector2Int> sameColorCluster = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(snappedCoord);
        visited.Add(snappedCoord);
        sameColorCluster.Add(snappedCoord);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (var neighborCoord in GetNeighbors(current.x, current.y))
            {
                if (!visited.Contains(neighborCoord) && gridMap.ContainsKey(neighborCoord))
                {
                    GameObject neighborObj = gridMap[neighborCoord];
                    Bubble neighborComp = neighborObj.GetComponent<Bubble>();
                    if (neighborComp != null && neighborComp.assignedColor == targetColor)
                    {
                        visited.Add(neighborCoord);
                        queue.Enqueue(neighborCoord);
                        sameColorCluster.Add(neighborCoord);
                    }
                }
            }
        }

        if (sameColorCluster.Count >= 3)
        {
            Debug.Log($"Popping cluster of {sameColorCluster.Count} bubbles!");
            foreach (var coord in sameColorCluster)
            {
                if (gridMap.ContainsKey(coord))
                {
                    GameObject bObj = gridMap[coord];
                    if (bObj != null)
                    {
                        Bubble bComp = bObj.GetComponent<Bubble>();
                        Color bColor = bComp != null ? bComp.assignedColor : targetColor;
                        SpawnPopEffect(bObj.transform.position, bColor);
                        Destroy(bObj);
                    }
                    gridMap.Remove(coord);
                }
            }

            // Calculate score for popped cluster (scaling with count, e.g. count * count * 10)
            int clusterPoints = sameColorCluster.Count * sameColorCluster.Count * 10;
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AddScore(clusterPoints);
            }

            ProcessFloatingBubbles();
        }
    }

    private void ProcessFloatingBubbles()
    {
        HashSet<Vector2Int> connectedToCeiling = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int col = 0; col < columns; col++)
        {
            Vector2Int topCoord = new Vector2Int(0, col);
            if (gridMap.ContainsKey(topCoord))
            {
                queue.Enqueue(topCoord);
                connectedToCeiling.Add(topCoord);
            }
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (var neighborCoord in GetNeighbors(current.x, current.y))
            {
                if (!connectedToCeiling.Contains(neighborCoord) && gridMap.ContainsKey(neighborCoord))
                {
                    connectedToCeiling.Add(neighborCoord);
                    queue.Enqueue(neighborCoord);
                }
            }
        }

        List<Vector2Int> floatingBubbles = new List<Vector2Int>();
        foreach (var kvp in gridMap)
        {
            if (!connectedToCeiling.Contains(kvp.Key))
            {
                floatingBubbles.Add(kvp.Key);
            }
        }

        if (floatingBubbles.Count > 0)
        {
            Debug.Log($"Dropping {floatingBubbles.Count} floating bubbles!");
            foreach (var coord in floatingBubbles)
            {
                if (gridMap.ContainsKey(coord))
                {
                    GameObject bObj = gridMap[coord];
                    if (bObj != null)
                    {
                        Bubble bComp = bObj.GetComponent<Bubble>();
                        Color bColor = bComp != null ? bComp.assignedColor : Color.white;
                        SpawnPopEffect(bObj.transform.position, bColor);
                        Destroy(bObj);
                    }
                    gridMap.Remove(coord);
                }
            }

            // Award bonus points for dropped floating bubbles
            int floatingPoints = floatingBubbles.Count * 20;
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AddScore(floatingPoints);
            }
        }
    }

    private void SpawnPopEffect(Vector3 position, Color color)
    {
        if (bubblePopEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(bubblePopEffectPrefab, position, Quaternion.identity);
            ParticleSystem ps = effectObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
                ps.Play();
            }
            Destroy(effectObj, 1.5f);
            return;
        }

        GameObject fallbackObj = new GameObject("BubblePopEffect");
        fallbackObj.transform.position = position;

        ParticleSystem fallbackPs = fallbackObj.AddComponent<ParticleSystem>();
        fallbackPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var fallbackMain = fallbackPs.main;
        fallbackMain.playOnAwake = false;
        fallbackMain.stopAction = ParticleSystemStopAction.Destroy;
        fallbackMain.duration = 0.3f;
        fallbackMain.loop = false;
        fallbackMain.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        fallbackMain.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        fallbackMain.startSize = new ParticleSystem.MinMaxCurve(bubbleDiameter * 0.15f, bubbleDiameter * 0.3f);
        fallbackMain.startColor = color;

        var emission = fallbackPs.emission;
        emission.enabled = true;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });

        var shape = fallbackPs.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = bubbleDiameter * 0.2f;

        fallbackPs.Play();
    }

    public void PushRowsDown(GameObject bubblePrefab)
    {
        Dictionary<Vector2Int, GameObject> newGridMap = new Dictionary<Vector2Int, GameObject>();

        // Find max row currently present
        int maxRow = 0;
        foreach (var coord in gridMap.Keys)
        {
            if (coord.x > maxRow) maxRow = coord.x;
        }

        // Iterate from bottom row up to top row, moving down by 2 rows to preserve row parity (even/odd staggering)
        for (int row = maxRow; row >= 0; row--)
        {
            for (int col = 0; col < columns; col++)
        {
                Vector2Int oldCoord = new Vector2Int(row, col);
                if (gridMap.ContainsKey(oldCoord))
                {
                    GameObject bubbleObj = gridMap[oldCoord];
                    if (bubbleObj == null) continue;

                    int newRow = row + 2;
                    int newCol = col;

                    Bubble bubbleComp = bubbleObj.GetComponent<Bubble>();
                    if (bubbleComp != null)
                    {
                        bubbleComp.row = newRow;
                        bubbleComp.col = newCol;
                    }

                    bubbleObj.transform.position = GetWorldPosition(newRow, newCol);

                    if (bubbleObj.transform.position.y <= dangerLineY)
                    {
                        TriggerGameOver();
                    }

                    newGridMap[new Vector2Int(newRow, newCol)] = bubbleObj;
                }
            }
        }

        gridMap = newGridMap;

        // Spawn new top rows (row 0 and row 1) to keep the top filled
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                SpawnBubbleAt(row, col, bubblePrefab);
            }
        }
    }

    public void ClearGrid()
    {
        foreach (var kvp in gridMap)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        gridMap.Clear();
    }

    void TriggerGameOver()
    {
        Debug.Log("Game Over! Bubbles passed the danger line.");
        OnGameOver?.Invoke();
    }
}
