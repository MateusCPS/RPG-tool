using TMPro;
using UnityEngine;

public class MoveRejectedNotice : MonoBehaviour
{
    [SerializeField] TokenManager tokens;
    [SerializeField] TMP_Text noticeText;
    [SerializeField] float duration = 2.5f;

    float hideTime;

    void OnEnable() => tokens.MoveRejected += Show;
    void OnDisable() => tokens.MoveRejected -= Show;
    void Start() => noticeText.gameObject.SetActive(false);

    void Update()
    {
        if(noticeText.gameObject.activeSelf && Time.unscaledTime >= hideTime)
            noticeText.gameObject.SetActive(false);
    }

    void Show(TokenView token, MoveRejection reason)
    {
        if(reason == MoveRejection.NoMovementLeft)
        {
            float left = CombatRules.RemainingMovement(token.Data.movedThisRound, token.Data.speed);
            noticeText.text = $"Movimento maior que o deslocamento: restam {left:0.#} m nesta rodada";
        }
        else
        {
            noticeText.text = $"Você não pode mover {token.Data.displayName}";
        }

        noticeText.gameObject.SetActive(true);
        hideTime = Time.unscaledTime + duration;
    }
}
