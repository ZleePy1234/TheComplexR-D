using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using com.cyborgAssets.inspectorButtonPro;


[ExecuteAlways]
public class UI_PostProcesser : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [Header("hud Post")]
    [SerializeField] private Camera hudCamera;
    [SerializeField] private GameObject hudVol;
    [SerializeField] private GameObject hudCanvas;
    [Header("shop Post")]
    [SerializeField] private Camera shopCamera;
    [SerializeField] private GameObject shopVol;
    [SerializeField] private GameObject shopCanvas;

    [Header("pause Post")]
    [SerializeField] private Camera pauseCamera;
    [SerializeField] private GameObject pauseVol;
    [SerializeField] private GameObject pauseCanvas;

    [Header("Screen Overlay Post")]
    [SerializeField] private Camera screenOverlayCamera;
    [SerializeField] private GameObject screenOverlayVol;
    [SerializeField] private GameObject screenOverlayCanvas;


    [Header("game Post")]
    [SerializeField] private GameObject gameVol;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [ProButton] public void HudToggle()
    {
        hudCamera.gameObject.SetActive(!hudCamera.gameObject.activeSelf);
        hudVol.SetActive(!hudVol.activeSelf);
        hudCanvas.SetActive(!hudCanvas.activeSelf);
    }
    [ProButton] public void ShopToggle()
    {
        shopCamera.gameObject.SetActive(!shopCamera.gameObject.activeSelf);
        shopVol.SetActive(!shopVol.activeSelf);
        shopCanvas.SetActive(!shopCanvas.activeSelf);
    }
    [ProButton] public void PauseToggle()
    {
        pauseCamera.gameObject.SetActive(!pauseCamera.gameObject.activeSelf);
        pauseVol.SetActive(!pauseVol.activeSelf);
        pauseCanvas.SetActive(!pauseCanvas.activeSelf);
        ScreenOverlayToggle();
    }

    [ProButton] public void ScreenOverlayToggle()
    {
        screenOverlayCamera.gameObject.SetActive(!screenOverlayCamera.gameObject.activeSelf);
        screenOverlayVol.SetActive(!screenOverlayVol.activeSelf);
        screenOverlayCanvas.SetActive(!screenOverlayCanvas.activeSelf);
    }
    [ProButton]
    public void GameToggle()
    {
        gameVol.SetActive(!gameVol.activeSelf);
    }


    [ProButton]
    public void PauseMenu()
    {
        PauseToggle();
        HudToggle();
        player.SetActive(!player.activeSelf);
        Time.timeScale = Time.timeScale == 1 ? 0 : 1;
    }
    [ProButton]public void CreditsMenu()
    {
        PauseToggle();
    }

    public void QuitGame()
    {
        Application.Quit();
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

    public void StartLoadNextLevel()
	{
		_ = LoadNextLevelAsync();
	}
}
