using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LostPanel : MonoBehaviour
{
    public void BindToGameManager()
    {
        if (GameManager.Instance == null)
            return;

        TextMeshProUGUI scoreLabel = FindScoreText();
        GameManager.Instance.RegisterGameOverUI(gameObject, scoreLabel);
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Menu()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.MainMenu();
        else
            SceneManager.LoadScene(GameSceneIndex.Menu);
    }

    private TextMeshProUGUI FindScoreText()
    {
        string[] names = { "Score Text", "Final Score", "Score" };
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int n = 0; n < names.Length; n++)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (!children[i].name.Equals(names[n], System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (children[i].TryGetComponent(out TextMeshProUGUI label))
                    return label;
            }
        }

        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].transform == transform)
                continue;

            return labels[i];
        }

        return null;
    }
}
