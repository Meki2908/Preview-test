using UnityEngine;

public class UIBillboarding : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Camera.main == null) return;

        // Hướng popup về camera
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                         Camera.main.transform.rotation * Vector3.up);
    }

}
