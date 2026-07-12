using UnityEngine;

public class MirrorReflection : MonoBehaviour
{
    public Transform playerCamera;
    public Transform mirrorSurface; // mặt phẳng gương
    public Camera mirrorCamera;

    void LateUpdate()
    {
        Vector3 cameraDirection = playerCamera.forward;
        Vector3 mirrorNormal = mirrorSurface.forward;

        Vector3 reflectedDirection = Vector3.Reflect(cameraDirection, mirrorNormal);
        mirrorCamera.transform.position = ReflectPosition(playerCamera.position, mirrorSurface.position, mirrorNormal);
        mirrorCamera.transform.forward = reflectedDirection;
    }

    Vector3 ReflectPosition(Vector3 pos, Vector3 mirrorPos, Vector3 mirrorNormal)
    {
        Vector3 toMirror = pos - mirrorPos;
        Vector3 reflected = toMirror - 2 * Vector3.Dot(toMirror, mirrorNormal) * mirrorNormal;
        return mirrorPos + reflected;
    }
}
