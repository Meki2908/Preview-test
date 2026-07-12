using TMPro;
using UnityEngine;

public class DamagePopupAnimation : MonoBehaviour
{
    public AnimationCurve opacityCurve;
    public AnimationCurve sizeCurve;
    public AnimationCurve heightCurve;

    private TextMeshProUGUI text;
    private float timer = 0;
    private Vector3 origin;
    private Vector3 originScale;

    private float sizeMultiplier = 1f;
    private float speedMultiplier = 1f;

    private void Awake()
    {
        text = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        origin = transform.position;
        originScale = transform.localScale;
    }
    // Update is called once per frame
    private void Update()
    {
        text.color = new Color(1, 1, 1, opacityCurve.Evaluate(timer));
        transform.localScale = originScale * sizeCurve.Evaluate(timer) * sizeMultiplier;
        transform.position = origin + new Vector3(0, 1 + heightCurve.Evaluate(timer), 0);
        timer += Time.deltaTime * speedMultiplier;
    }
    public void SetParameters(float sizeMul, float speedMul)
    {
        sizeMultiplier = sizeMul;
        speedMultiplier = speedMul;
    }
}
