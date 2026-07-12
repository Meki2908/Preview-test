using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecordManager : MonoBehaviour
{
    [Serializable]
    public class RecordList
    {
        public List<int> scores = new List<int>();
    }

    public static RecordManager Instance;
    private string savePath;
    private RecordList recordList = new RecordList();
    private const int MaxRecords = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            savePath = Path.Combine(Application.persistentDataPath, "records.json");
            LoadRecords();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<int> GetRecords()
    {
        return recordList.scores;
    }

    public void AddRecord(int score)
    {
        recordList.scores.Add(score);

        // Sắp xếp giảm dần
        recordList.scores.Sort((a, b) => b.CompareTo(a));

        // Giữ lại top 10
        if (recordList.scores.Count > MaxRecords)
        {
            recordList.scores = recordList.scores.GetRange(0, MaxRecords);
        }

        SaveRecords();
    }
    
    public void SaveRecords()
    {
        string json = JsonUtility.ToJson(recordList, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadRecords()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            recordList = JsonUtility.FromJson<RecordList>(json);
        }
        else
        {
            recordList = new RecordList();
        }
    }

    public void ClearRecords()
    {
        recordList.scores.Clear();
        SaveRecords();
    }
}
