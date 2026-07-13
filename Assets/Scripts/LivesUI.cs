using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LivesUI : MonoBehaviour
{
    private TextMeshProUGUI livesLabel;

    private void Awake()
    {
        livesLabel = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        TryBind();
    }

    private void Start()
    {
        TryBind();
    }

    private void TryBind()
    {
        if (livesLabel == null)
            livesLabel = GetComponent<TextMeshProUGUI>();

        if (livesLabel == null || GameManager.Instance == null)
            return;

        GameManager.Instance.RegisterLivesUI(livesLabel);
    }

    public void BindToGameManager()
    {
        TryBind();
    }
}
