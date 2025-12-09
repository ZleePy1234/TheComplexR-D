using UnityEngine;

public class GameOver : MonoBehaviour
{
    public GameObject gameOverUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void showGameOver()
    {
        gameOverUI.SetActive(true);
    }
}
