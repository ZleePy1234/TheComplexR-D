using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int enemiesKilled = 0;
    public int totalEnemies = 0;

    private bool exitEnabled = false;
    private bool gameOver = false;

    public GameObject exitDoor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        exitDoor.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(enemiesKilled >= totalEnemies && !exitEnabled)
        {
            EnableExit();
        }
    }
    void EnableExit()
    {
        exitEnabled = true;
        Debug.Log("Exit is now enabled!");
        exitDoor.SetActive(true);
    }
}
