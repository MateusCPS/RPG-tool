using UnityEngine;

public class MasterOnlyGroup : MonoBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] GameObject[] targets;

    void OnEnable()
    {
        tokens.PermissionsChanged += Refresh;
        Refresh();
    }

    void OnDisable() => tokens.PermissionsChanged -= Refresh;

    void Refresh()
    {
        bool show = tokens.CanManageTokens;
        foreach (GameObject target in targets)
            target.SetActive(show);
    }
}
