using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomLerpSpeed = 10f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 15f;

    private InputSystem_Actions controls;
    [SerializeField] private CinemachineCamera cam;
    private CinemachineOrbitalFollow orbital;
    private float scrollDelta;

    private float targetZoom;
    private float currentZoom;
    void Start()
    {
        if (FindAnyObjectByType<TopDownCinemachineCamera>() != null)
        {
            enabled = false;
            return;
        }

        controls = new InputSystem_Actions();
        controls.Enable();
        controls.CameraControls.Mousezoom.performed += HandleMouseScroll;
        orbital = cam.GetComponent<CinemachineOrbitalFollow>();
        if (orbital == null)
        {
            Debug.LogError("Orbital Follow component is missing on the Cinemachine Camera!");
        }
        currentZoom = orbital.Radius;
        targetZoom = currentZoom;
    }

    private void HandleMouseScroll(InputAction.CallbackContext context)
    {
        scrollDelta = context.ReadValue<float>();
    }

    void Update()
    {
        if (scrollDelta != 0)
        {
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                targetZoom = Mathf.Clamp(orbital.Radius - scrollDelta * zoomSpeed, minDistance, maxDistance);
                scrollDelta = 0;
            }
        }
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomLerpSpeed);
        orbital.Radius = currentZoom;
    }
}
