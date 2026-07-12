using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Recorđíplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] recordSlots;

    private void OnEnable() // Khi panel bật lên sẽ auto load dữ liệu
    {
        ShowRecords();
    }

    private void ShowRecords()
    {
        List<int> scores = RecordManager.Instance.GetRecords();
        string display = "";

        for (int i = 0; i < recordSlots.Length; i++)
        {
            if (i < scores.Count)
                recordSlots[i].text = $"Top {i + 1}: {scores[i]}";
            else
                recordSlots[i].text = $"Top {i + 1}: ---"; // Nếu chưa có điểm
        }
    }

    public void ClearRecords()
    {
        RecordManager.Instance.ClearRecords();
        ShowRecords();
    }
}
