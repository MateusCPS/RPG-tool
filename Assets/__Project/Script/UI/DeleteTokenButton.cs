using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DeleteTokenButton : MonoBehaviour
{
    [SerializeField] TokenManager tokens;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(tokens.DeleteSelected);
    }

    void OnEnable()
    {
        tokens.SelectionChanged += Refresh;
        tokens.PermissionsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        tokens.SelectionChanged -= Refresh;
        tokens.PermissionsChanged -= Refresh;
    }

    void Refresh()
    {
        button.interactable = tokens.Selected != null && tokens.CanManageTokens;
    }
}