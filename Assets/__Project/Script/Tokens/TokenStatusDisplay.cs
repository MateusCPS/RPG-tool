using UnityEngine;
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEngine.UIElements;

public class TokenStatusDisplay : MonoBehaviour
{
    [SerializeField] TextMeshPro nameLabel;
    [SerializeField] Transform hpBar;
    [SerializeField] MeshRenderer hpBackgroud;
    [SerializeField] MeshRenderer hpFill;

    [SerializeField] float barHeight = 0.18f;
    [SerializeField] float gap = 0.15f;
    [SerializeField, Range(0.3f, 1f)] float barWidth = 0.9f;

    [SerializeField] Color fullColor = new Color(0.30f, 0.80f, 0.35f);
    [SerializeField] Color halfColor = new Color(0.95f, 0.80f, 0.25f);
    [SerializeField] Color lowColor = new Color(0.90f, 0.25f, 0.25f);

    Material fillMaterial;

    void Awake()
    {
        fillMaterial = hpFill.material;

        nameLabel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hpBackgroud.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hpFill.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        nameLabel.alignment = TextAlignmentOptions.Center;
    }

    public void Refresh(TokenData data, float cellSize)
    {
        float size = data.size * cellSize;
        float half = size / 2f;

        nameLabel.text = data.displayName;
        nameLabel.transform.localPosition = new Vector3(0f, 0.02f, -(half + gap));
       
        float width = size * barWidth;
        hpBar.localPosition = new Vector3(0f, 0.02f, half + gap);
        hpBackgroud.transform.localPosition = Vector3.zero;
        hpBackgroud.transform.localScale = new Vector3(width, barHeight, 1f);

        float ratio = data.maxHp > 0 ? Mathf.Clamp01((float)data.hp / data.maxHp) : 0f;

        float fillWidth = width * ratio;
        hpFill.enabled = ratio > 0f;
        hpFill.transform.localPosition = new Vector3(-width / 2f + fillWidth / 2f, 0.005f, 0f);
        hpFill.transform.localScale = new Vector3(fillWidth, barHeight * 0.7f, 1f);

        fillMaterial.color = ratio > 0.5f
            ? Color.Lerp(halfColor, fullColor, (ratio - 0.5f) * 2f)
            : Color.Lerp(lowColor, halfColor, ratio * 2f);
    }

    void OnDestroy()
    {
        if (fillMaterial != null) Destroy(fillMaterial);
    }
}
