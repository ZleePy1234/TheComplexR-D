using TMPro;
using UnityEngine;
[ExecuteAlways]
public class TutorialBoxScript : MonoBehaviour
{
    [SerializeField] private string tutorialTitle;
    [SerializeField] private string tutorialText;

    [SerializeField] private TextMeshProUGUI titleBox;
    [SerializeField] private TextMeshProUGUI textBox;
    private GameObject canvasGameObject;
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        canvasGameObject = GetComponentInChildren<Canvas>().gameObject;
        animator = GetComponent<Animator>();
        titleBox.text = tutorialTitle;
        textBox.text = tutorialText;
    }

    // Update is called once per frame
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetTrigger("ShowTutorial");
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetTrigger("HideTutorial");
        }
    }
}
