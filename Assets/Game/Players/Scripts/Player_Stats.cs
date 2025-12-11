using UnityEngine;
using com.cyborgAssets.inspectorButtonPro;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerStats : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int plating;
    public int platingMax = 3;
    public int playerResin;
    private PlayerMovement playerMovement;

    public VolumeProfile HUDprofile;

    public float speedMultiplier = 1.0f;

    // chromatic aberration settings
    public float chromaMaxIntensity = 1.0f;
    public float chromaReturnDuration = 1.5f;
    private Coroutine chromaCoroutine;
    private float chromaOriginalIntensity = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        currentHealth = maxHealth;
        plating = 0;

        
        if (HUDprofile != null && HUDprofile.TryGet(out ChromaticAberration chroma))
        {
            chroma.intensity.value = 0.1f;
            chromaOriginalIntensity = chroma.intensity.value;
        }
    }

    [ProButton]public void DamagePlayer(int damage)
    {
        if (plating > 0)
        {
            plating--;
            return;
        }
        currentHealth -= damage;

        
        if (HUDprofile != null && HUDprofile.TryGet(out ChromaticAberration chromaComp))
        {
            
            if (chromaCoroutine != null)
            {
                StopCoroutine(chromaCoroutine);
                chromaCoroutine = null;
            }

            
            chromaComp.intensity.value = chromaMaxIntensity;
            chromaCoroutine = StartCoroutine(LerpChromaticAberration(chromaComp));
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator LerpChromaticAberration(ChromaticAberration comp)
    {
        float start = comp.intensity.value;
        float elapsed = 0f;

        
        if (HUDprofile != null && HUDprofile.TryGet(out ChromaticAberration check))
        {
            
        }

        while (elapsed < chromaReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / chromaReturnDuration);
            comp.intensity.value = Mathf.Lerp(chromaMaxIntensity, chromaOriginalIntensity, t);
            yield return null;
        }

        comp.intensity.value = chromaOriginalIntensity;
        chromaCoroutine = null;
    }
    void Die()
    {
        Debug.Log("Player Died");
        playerMovement.enabled = false;
        StartCoroutine(DeathRoutine());
    }
    private IEnumerator DeathRoutine()
    {
        // poner cosas de animacion de muerte aqui
        yield return new WaitForSeconds(2f);
        StartLoadGameOverevel();
    }
    public void HealPlayer(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public async Task LoadGameOverAsync()
	{
		int current = SceneManager.GetActiveScene().buildIndex;
		int next = 3;
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

    [ProButton] public void StartLoadGameOverevel()
	{
		_ = LoadGameOverAsync();
	}
}
