using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Cria os tokens e cuida do arraste: apertar pega, mover leva junto, soltar encaixa.
public class TokenManager : MonoBehaviour
{
    [SerializeField] TablePointer pointer;
    [SerializeField] TableGrid grid;
    [SerializeField] TokenView tokenPrefab;

    [Header("Tokens de teste")]
    [SerializeField] Texture2D[] testImages;

    readonly List<TokenView> tokens = new List<TokenView>();
    IMovementRule rule;
    TokenView dragged;
    Vector3 grabOffset; // diferença entre o ponto agarrado e o centro do token
    Vector3 dragStart;  // onde o token estava quando foi pego

    // Informações para outros scripts (texto de distância, câmera).
    public bool IsDragging => dragged != null;
    public TokenView Dragged => dragged;
    public Vector3 DragStart => dragStart;
    public IMovementRule Rule => rule;

    IEnumerator Start()
    {
        rule = new GridMovementRule(grid);

        // Espera um quadro: o MapBoard ajusta a grade no Start dele,
        // e a Unity não garante a ordem entre os Start de scripts diferentes.
        yield return null;

        for (int i = 0; i < testImages.Length; i++)
        {
            var data = new TokenData { displayName = testImages[i].name };
            data.Position = grid.CellToWorldCenter(new Vector2Int(2 + i * 2, 2));
            Spawn(data, testImages[i]);
        }
    }

    public TokenView Spawn(TokenData data, Texture2D image)
    {
        data.Position = rule.Snap(data.Position, data.size);
        TokenView view = Instantiate(tokenPrefab, transform);
        view.Init(data, image, grid.CellSize);
        tokens.Add(view);
        return view;
    }

    // Há algum token neste ponto do chão? (a câmera pergunta isso)
    public bool IsTokenAt(Vector3 worldPoint) => FindTokenAt(worldPoint) != null;

    TokenView FindTokenAt(Vector3 worldPoint)
    {
        // Do último para o primeiro: o token criado por último está por cima.
        for (int i = tokens.Count - 1; i >= 0; i--)
            if (tokens[i].Contains(worldPoint)) return tokens[i];
        return null;
    }

    // LateUpdate: o TablePointer já calculou a posição do ponteiro neste quadro.
    void LateUpdate()
    {
        Pointer device = Pointer.current; // mouse (botão esquerdo) ou dedo
        if (device == null || rule == null) return;

        if (device.press.wasPressedThisFrame && pointer.HasPoint)
            BeginDrag(pointer.WorldPoint);

        if (dragged == null) return;

        if (device.press.isPressed)
        {
            if (pointer.HasPoint) dragged.MoveVisual(pointer.WorldPoint + grabOffset);
        }
        else
        {
            EndDrag();
        }
    }

    void BeginDrag(Vector3 point)
    {
        TokenView found = FindTokenAt(point);
        if (found == null) return;

        dragged = found;
        dragStart = dragged.Data.Position;
        grabOffset = dragged.transform.position - point;
        grabOffset.y = 0f;
        dragged.SetDragging(true);
    }

    void EndDrag()
    {
        // Encaixa onde o token foi solto e só então atualiza os dados.
        Vector3 dropped = dragged.transform.position;
        dragged.Data.Position = rule.Snap(dropped, dragged.Data.size);
        dragged.SetDragging(false);
        dragged = null;
    }
}
