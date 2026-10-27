using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
 
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
 
    public TextMeshProUGUI coinText;
    public GameObject winPanel;            // shown when the player reaches the goal
    public int totalCoins = 6;
 
    private int coins = 0;
 
    void Awake()
    {
        instance = this;
    }
 
    void Start()
    {
        Time.timeScale = 1f;
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        UpdateUI();
    }
 
    public void AddCoin()
    {
        coins++;
        UpdateUI();
    }
 
    void UpdateUI()
    {
        coinText.text = "Coins: " + coins + " / " + totalCoins;
    }
 
    public void LevelComplete()
    {
        Debug.Log("LEVEL COMPLETE! Coins: " + coins);
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
        Time.timeScale = 0f;               // freeze the game
    }
 
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
