using UnityEngine;
using com.cyborgAssets.inspectorButtonPro;


[ExecuteAlways]
public class UI_PostProcesser : MonoBehaviour
{
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
    }

    [ProButton] public void ScreenOverlayToggle()
    {
        screenOverlayCamera.gameObject.SetActive(!screenOverlayCamera.gameObject.activeSelf);
        screenOverlayVol.SetActive(!screenOverlayVol.activeSelf);
        screenOverlayCanvas.SetActive(!screenOverlayCanvas.activeSelf);
    }
    [ProButton] public void GameToggle()
    {
        gameVol.SetActive(!gameVol.activeSelf);
    }
}
