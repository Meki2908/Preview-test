using UnityEngine;
using TMPro;

public class DamageDisplay : MonoBehaviour
{
    public static DamageDisplay current;
    public GameObject prefab;

    private void Awake()
    {
        current = this;
    }

    public void CreatePopUp(Vector3 position, string text, Color color, float sizeMultiplier = 1f, float speedMultiplier = 1f)
    {
        GameObject popUp = Instantiate(prefab, position, Quaternion.identity);
        var popupAnim = popUp.GetComponent<DamagePopupAnimation>();

        var temp = popUp.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        temp.text = text;
        temp.faceColor = color;

        // Gửi parameter qua script animation
        popupAnim.SetParameters(sizeMultiplier, speedMultiplier);

        Destroy(popUp, 1f / speedMultiplier);
    }

}
