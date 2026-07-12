#if UNITY_EDITOR
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

public static class TopDownCameraSetup
{
    [MenuItem("GameObject/Cinemachine/Setup Top-Down Camera", false, 10)]
    private static void SetupTopDownCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var mainCameraObject = GameObject.Find("Main Camera");
            if (mainCameraObject != null)
                mainCamera = mainCameraObject.GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog(
                "Top-Down Camera",
                "Không tìm thấy Main Camera trong scene.",
                "OK");
            return;
        }

        Undo.RecordObject(mainCamera.gameObject, "Setup Top-Down Camera");

        if (mainCamera.GetComponent<CinemachineBrain>() == null)
            mainCamera.gameObject.AddComponent<CinemachineBrain>();

        DisableLegacyCameraScripts();

        TopDownCinemachineCamera topDownCamera = Object.FindAnyObjectByType<TopDownCinemachineCamera>();
        GameObject virtualCameraObject;

        if (topDownCamera != null)
        {
            virtualCameraObject = topDownCamera.gameObject;
        }
        else
        {
            virtualCameraObject = new GameObject("CM TopDown");
            Undo.RegisterCreatedObjectUndo(virtualCameraObject, "Create Top-Down Camera");
            virtualCameraObject.AddComponent<CinemachineCamera>();
            topDownCamera = virtualCameraObject.AddComponent<TopDownCinemachineCamera>();
        }

        SoldierController soldier = Object.FindAnyObjectByType<SoldierController>();
        if (soldier != null)
        {
            Transform target = CameraTargetAnchor.GetOrCreate(soldier.transform);
            topDownCamera.SetFollowTarget(target);

            if (!soldier.CompareTag("Player"))
            {
                Undo.RecordObject(soldier.gameObject, "Tag Soldier as Player");
                soldier.gameObject.tag = "Player";
            }
        }
        else
        {
            Debug.LogWarning("TopDownCameraSetup: Không tìm thấy SoldierController trong scene. Gán Follow Target thủ công sau.");
        }

        Selection.activeGameObject = virtualCameraObject;
        EditorUtility.DisplayDialog(
            "Top-Down Camera",
            "Đã cấu hình Cinemachine top-down.\n\n" +
            "- Main Camera có Cinemachine Brain\n" +
            "- CM TopDown follow soldier từ trên xuống\n" +
            "- Script camera cũ (ThirdPerson/CameraFollow) đã tắt",
            "OK");
    }

    private static void DisableLegacyCameraScripts()
    {
        foreach (var thirdPersonCamera in Object.FindObjectsByType<ThirdPersonCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(thirdPersonCamera, "Disable ThirdPersonCamera");
            thirdPersonCamera.enabled = false;
        }

        foreach (var cameraFollow in Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(cameraFollow, "Disable CameraFollow");
            cameraFollow.enabled = false;
        }
    }
}
#endif
