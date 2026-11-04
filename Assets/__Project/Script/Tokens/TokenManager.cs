using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


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
    Vector3 grabOffset;
    Vector3 dragStart; 
    public bool IsDragging => dragged != null;
    public TokenView Dragged => dragged;
    public Vector3 DragStart => dragStart;
    public IMovementRule Rule => rule;

    public enum MovementMode { Grip, Free }

    [Header("Modo de movimento")]
    [SerializeField] MovementMode mode = MovementMode.Grip;
    IEnumerator Start()
    {
        SetMode(mode);

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
    
    public void SetMode(MovementMode newMode)
    {
        mode = newMode;
        rule = mode == MovementMode.Grip 
        ? new GridMovementRule(grid) 
        : new FreeMovementRule(grid);

        foreach (var token in tokens)
        {
            token.Data.Position = rule.Snap(token.Data.Position, token.Data.size);
            token.Refresh();
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && tokens != null)
            SetMode(mode);
    }


    public bool IsTokenAt(Vector3 worldPoint) => FindTokenAt(worldPoint) != null;

    TokenView FindTokenAt(Vector3 worldPoint)
    {
        for (int i = tokens.Count - 1; i >= 0; i--)
            if (tokens[i].Contains(worldPoint)) return tokens[i];
        return null;
    }

    void LateUpdate()
    {
        Pointer device = Pointer.current;
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
        Vector3 dropped = dragged.transform.position;
        dragged.Data.Position = rule.Snap(dropped, dragged.Data.size);
        dragged.SetDragging(false);
        dragged = null;
    }
}
