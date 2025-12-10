using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTest : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartLoadNextLevel();
        }
    }

    public void StartLoadNextLevel()
	{
		_ = LoadNextLevelAsync();
	}
    public async Task LoadNextLevelAsync()
	{
		int current = SceneManager.GetActiveScene().buildIndex;
		int next = 0;
		int total = SceneManager.sceneCountInBuildSettings;

		if (next >= total)
		{
			Debug.LogWarning("LoadNextLevelAsync: No next scene in build settings.");
			return;
		}

		AsyncOperation op = SceneManager.LoadSceneAsync(next);
		op.allowSceneActivation = true;

		while (!op.isDone)
		{
			await Task.Yield();
		}
	}
}
