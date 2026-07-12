using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class AutomaticShooting : MonoBehaviour
{
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private float cooldown;

    private float lastShottime;
    public UnityEvent OnShoot;

    // Update is called once per frame
    void Update()
    {
        if(shootAction.action.IsPressed() && FinishCoolDown())
        {
            Shooting();
            lastShottime = Time.time;
        }
    }
    private bool FinishCoolDown() => Time.time - lastShottime >= cooldown;
    private void Shooting() => OnShoot.Invoke();
}
