using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public BubbleGrid bubbleGrid;
    public GameObject bubblePrefab;
    public TextMeshProUGUI scoreText;
    public GameObject scoreCanvas;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverScoreText;

    [Header("Pause Menu UI")]
    public GameObject pauseMenuPanel;
    public bool isPaused { get; private set; } = false;

    [Header("Game Flow Settings")]
    private const int INIT_SHOTS_BEFORE_PUSH = 9;
    private int currentShotCount = 0;

    public int GetDynamicShotsBeforePush()
    {
        int digits = score.ToString().Length;
        int calculated = INIT_SHOTS_BEFORE_PUSH - Mathf.Max(digits - 3, 0);
        return Mathf.Max(5, calculated);
    }

    [Header("Score")]
    public int score { get; private set; } = 0;

    [Header("Current & Next Bubble Queue")]
    public Color currentBubbleColor;
    public Color nextBubbleColor;

    public delegate void QueueUpdatedAction(Color current, Color next);
    public event QueueUpdatedAction OnQueueUpdated;

    public delegate void GameStateAction();
    public event GameStateAction OnGameStarted;
    public event GameStateAction OnGameEnded;

    public bool isGameOver { get; private set; } = false;

    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        if (bubbleGrid == null)
        {
            bubbleGrid = FindAnyObjectByType<BubbleGrid>();
        }

        if (bubbleGrid != null)
        {
            bubbleGrid.OnGameOver += HandleGameOver;
        }

        UpdateScoreUI();
        StartGame();
    }

    void OnDestroy()
    {
        if (bubbleGrid != null)
        {
            bubbleGrid.OnGameOver -= HandleGameOver;
        }
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void StartGame()
    {
        isGameOver = false;
        currentShotCount = 0;

        if (bubbleGrid != null && bubblePrefab != null)
        {
            bubbleGrid.GenerateInitialGrid(bubblePrefab);
        }

        InitializeBubbleQueue();
        OnGameStarted?.Invoke();
        Debug.Log("Game Started. Initial grid generated and bubble queue ready.");
    }

    private void InitializeBubbleQueue()
    {
        if (bubbleGrid == null || bubbleGrid.bubbleColors.Length == 0) return;

        nextBubbleColor = bubbleGrid.bubbleColors[Random.Range(0, bubbleGrid.bubbleColors.Length)];
        CycleBubble();
    }

    /// <summary>
    /// Called when the player's fired bubble finishes snapping, popping, and dropping.
    /// Safely handles row pushing as a deferred coroutine step to avoid race conditions.
    /// </summary>
    public void OnBubbleFinishedProcessing()
    {
        if (isGameOver) return;

        StartCoroutine(ProcessTurnCompletionRoutine());
    }

    private IEnumerator ProcessTurnCompletionRoutine()
    {
        // Wait a tiny fraction of a frame to ensure all destroy events and physics updates settle
        yield return null;

        currentShotCount++;
        int dynamicShotsBeforePush = GetDynamicShotsBeforePush();
        Debug.Log($"Turn completed. Shot count: {currentShotCount}/{dynamicShotsBeforePush}");

        if (currentShotCount >= dynamicShotsBeforePush)
        {
            PushRows();
            currentShotCount = 0;
            // Brief yield after pushing rows to prevent back-to-back stacking
            yield return new WaitForSeconds(0.05f);
        }

        CycleBubble();
    }

    private void CycleBubble()
    {
        currentBubbleColor = nextBubbleColor;
        if (bubbleGrid != null && bubbleGrid.bubbleColors.Length > 0)
        {
            nextBubbleColor = bubbleGrid.bubbleColors[Random.Range(0, bubbleGrid.bubbleColors.Length)];
        }

        OnQueueUpdated?.Invoke(currentBubbleColor, nextBubbleColor);
    }

    private void PushRows()
    {
        if (bubbleGrid != null && bubblePrefab != null)
        {
            Debug.Log("Pushing rows down safely!");
            bubbleGrid.PushRowsDown(bubblePrefab);
        }
    }

    private void HandleGameOver()
    {
        isGameOver = true;
        OnGameEnded?.Invoke();
        Debug.Log("Game Over triggered in GameManager.");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (scoreCanvas != null) 
        {
            scoreCanvas.SetActive(false); 
        }
        
        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = $"Score: {score}";
        }
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PauseGame()
    {
        if (isGameOver) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        if (scoreCanvas != null)
        {
            scoreCanvas.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (scoreCanvas != null)
        {
            scoreCanvas.SetActive(true);
        }
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
