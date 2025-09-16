using UnityEngine;

public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance;
    private float gameStartTime;
    public bool isPlaying = false;
    
    public float gameTime => isPlaying ? (Time.realtimeSinceStartup - gameStartTime) : 0f;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    public void StartGame()
    {
        gameStartTime = Time.realtimeSinceStartup; // LMJ: Record exact start time
        isPlaying = true;
        Debug.Log("Game Started at: " + gameStartTime);
    }
    
    public void StopGame()
    {
        isPlaying = false;
    }
}