using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CreditsScript : MonoBehaviour
{
    private Animator animator;
    public UI_PostProcesser uI_PostProcesser;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ToggleCredits()
    {
        uI_PostProcesser.CreditsMenu();
    }

    public void TurnOffCreditsButton()
    {
        animator = GetComponent<Animator>();
        animator.SetTrigger("Boot Off");
    }
}
