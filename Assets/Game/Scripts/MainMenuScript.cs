using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuScript : MonoBehaviour
{
	[Tooltip("Optional UI slider to show loading progress")]
	public Slider progressSlider;

	[Tooltip("Optional text to show loading percent")]
	public TextMeshProUGUI progressText;
	public void StartLoadNextLevel()
	{
		_ = LoadNextLevelAsync();
	}
    public void StartLoadTutorial()
	{
		_ = LoadTutorialAsync();
	}

	// Asynchronously loads the next scene in Build Settings.
	public async Task LoadNextLevelAsync()
	{
		int current = SceneManager.GetActiveScene().buildIndex;
		int next = current + 1;
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
			float progress = Mathf.Clamp01(op.progress);
			if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(true);
                progressSlider.value = progress;
            } 
			if (progressText != null) 
            {
                progressText.gameObject.SetActive(true);
                progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }

			await Task.Yield();
		}
	}
    public async Task LoadTutorialAsync()
	{
		int next = 0;

		AsyncOperation op = SceneManager.LoadSceneAsync(next);
		op.allowSceneActivation = true;

		while (!op.isDone)
		{
			float progress = Mathf.Clamp01(op.progress);
			if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(true);
                progressSlider.value = progress;
            } 
			if (progressText != null) 
            {
                progressText.gameObject.SetActive(true);
                progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }

			await Task.Yield();
		}
	}
}
