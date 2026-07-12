#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FootstepSetup
{
    private static readonly string[] DefaultConcreteClipPaths =
    {
        "Assets/Animated FPS Weapons Part 2/Audio/Footsteps/Concrete/concrete01.wav",
        "Assets/Animated FPS Weapons Part 2/Audio/Footsteps/Concrete/concrete02.wav",
        "Assets/Animated FPS Weapons Part 2/Audio/Footsteps/Concrete/concrete03.wav",
        "Assets/Animated FPS Weapons Part 2/Audio/Footsteps/Concrete/concrete04.wav"
    };

    [MenuItem("Tools/Audio/Assign Default Footstep Clips")]
    public static void AssignDefaultFootstepClips()
    {
        AudioClip[] clips = LoadDefaultClips();
        if (clips.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Footsteps",
                "Không tìm thấy clip concrete trong pack FPS.",
                "OK");
            return;
        }

        int updated = 0;
        foreach (FootstepController controller in Object.FindObjectsByType<FootstepController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            SerializedObject serializedObject = new SerializedObject(controller);
            SerializedProperty clipsProperty = serializedObject.FindProperty("footstepClips");
            clipsProperty.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
                clipsProperty.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            updated++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Footsteps",
            $"Đã gán {clips.Length} clip concrete cho {updated} FootstepController trong scene.",
            "OK");
    }

    [MenuItem("Tools/Audio/Add Footstep Controller To Scene Characters")]
    public static void AddFootstepControllerToSceneCharacters()
    {
        int added = 0;

        SoldierController[] soldiers = Object.FindObjectsByType<SoldierController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (SoldierController soldier in soldiers)
        {
            if (soldier.GetComponent<FootstepController>() != null)
                continue;

            Undo.AddComponent<FootstepController>(soldier.gameObject);
            added++;
        }

        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (EnemyBase enemy in enemies)
        {
            if (enemy.GetComponent<FootstepController>() != null)
                continue;

            Undo.AddComponent<FootstepController>(enemy.gameObject);
            added++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Footsteps",
            $"Đã thêm FootstepController lên {added} nhân vật.",
            "OK");
    }

    private static AudioClip[] LoadDefaultClips()
    {
        List<AudioClip> clips = new List<AudioClip>();
        foreach (string path in DefaultConcreteClipPaths)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                clips.Add(clip);
        }

        return clips.ToArray();
    }
}
#endif
