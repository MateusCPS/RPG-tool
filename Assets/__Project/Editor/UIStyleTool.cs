using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIStyleTool
{
    static readonly Color PanelColor = new Color32(30, 30, 36, 220);
    static readonly Color ButtonNormal = new Color32(46, 46, 54, 255);
    static readonly Color ButtonHighlighted = new Color32(62, 62, 72, 255);
    static readonly Color TextColor = new Color32(236, 236, 240, 255);
    static readonly Color DamageNormal = new Color32(150, 50, 50, 255);
    static readonly Color DamageHighlighted = new Color32(175, 65, 65, 255);
    static readonly Color HealNormal = new Color32(40, 120, 75, 255);
    static readonly Color HealHighlighted = new Color32(50, 145, 90, 255);

    [MenuItem("Tools/Mesa Virtual/Aplicar estilo da interface")]
    static void Apply()
    {
        GameObject canvasObject = GameObject.Find("Canvas");
        if (canvasObject == null)
        {
            Debug.LogError("UIStyleTool: objeto 'Canvas' não encontrado na cena.");
            return;
        }

        Undo.SetCurrentGroupName("Aplicar estilo da interface");
        int group = Undo.GetCurrentGroup();
        Transform root = canvasObject.transform;

        RectTransform toolbar = FindOrCreateToolbar(root);
        string[] buttons = { "NewToken", "DeleteToken", "LoadMap", "SettingsButton" };
        string[] labels = { "Novo token", "Apagar token", "Carregar mapa", "Configurações" };
        for (int i = 0; i < buttons.Length; i++)
        {
            Transform button = Find(root, buttons[i]);
            if (button == null) { Warn(buttons[i]); continue; }
            Undo.SetTransformParent(button, toolbar, "Mover botão para a barra");
            button.SetSiblingIndex(i);
            StyleButton(button, ButtonNormal, ButtonHighlighted, 52, 22, labels[i]);
        }

        StretchToCanvas(Find(root, "TableSettings"));
        Transform settings = Find(root, "TableSettingsPanel");
        if (settings != null)
        {
            StylePanel(settings);
            PlaceInCorner(settings as RectTransform, new Vector2(0f, 1f), new Vector2(236f, -16f), 280f);
            StyleButton(Find(settings, "ModeButton"), ButtonNormal, ButtonHighlighted, 48, 20, null);
            StyleButton(Find(settings, "GridButton"), ButtonNormal, ButtonHighlighted, 48, 20, null);
            StyleText(Find(settings, "PixelsLabel"), 18, false);
            StyleText(Find(settings, "OffsetLabel"), 18, false);
        }
        else Warn("TableSettingsPanel");

        StretchToCanvas(Find(root, "TokenInfo"));
        Transform info = Find(root, "TokenInfoPanel");
        if (info != null)
        {
            StylePanel(info);
            PlaceInCorner(info as RectTransform, new Vector2(1f, 1f), new Vector2(-16f, -16f), 300f);
            StyleText(Find(info, "NameText"), 26, true);
            StyleText(Find(info, "HpText"), 22, false);
            StyleInput(Find(info, "AmountInput"), "Valor");
            SetPreferredHeight(Find(info, "Buttons"), 48);
            StyleButton(Find(info, "Damage"), DamageNormal, DamageHighlighted, 48, 22, "Dano");
            StyleButton(Find(info, "Heal"), HealNormal, HealHighlighted, 48, 22, "Cura");
        }
        else Warn("TokenInfoPanel");

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        Debug.Log("UIStyleTool: estilo aplicado. Ctrl+Z desfaz tudo de uma vez.");
    }

    static RectTransform FindOrCreateToolbar(Transform root)
    {
        Transform existing = Find(root, "Toolbar");
        RectTransform toolbar;
        if (existing != null)
        {
            toolbar = existing as RectTransform;
        }
        else
        {
            var go = new GameObject("Toolbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(go, "Criar barra de ferramentas");
            go.layer = root.gameObject.layer;
            toolbar = go.GetComponent<RectTransform>();
            toolbar.SetParent(root, false);
        }

        Undo.RecordObject(toolbar, "Barra de ferramentas");
        toolbar.SetSiblingIndex(0);
        toolbar.anchorMin = new Vector2(0f, 0f);
        toolbar.anchorMax = new Vector2(0f, 1f);
        toolbar.pivot = new Vector2(0f, 0.5f);
        toolbar.anchoredPosition = Vector2.zero;
        toolbar.sizeDelta = new Vector2(220f, 0f);

        var image = toolbar.GetComponent<Image>();
        Undo.RecordObject(image, "Barra de ferramentas");
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        image.color = PanelColor;

        var layout = toolbar.GetComponent<VerticalLayoutGroup>();
        Undo.RecordObject(layout, "Barra de ferramentas");
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return toolbar;
    }

    static void StyleButton(Transform target, Color normal, Color highlighted, float height, float fontSize, string label)
    {
        if (target == null) return;

        var image = target.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "Estilo do botão");
            if (image.sprite == null) image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        var button = target.GetComponent<Button>();
        if (button != null)
        {
            Undo.RecordObject(button, "Estilo do botão");
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = normal * 0.75f + new Color(0f, 0f, 0f, 0.25f);
            colors.selectedColor = normal;
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 110f / 255f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        SetPreferredHeight(target, height);

        var text = target.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            Undo.RecordObject(text, "Estilo do botão");
            text.enableAutoSizing = false;
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            if (label != null) text.text = label;
        }
    }

    static void StyleText(Transform target, float fontSize, bool bold)
    {
        if (target == null) return;
        var text = target.GetComponent<TMP_Text>();
        if (text == null) return;
        Undo.RecordObject(text, "Estilo do texto");
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.color = TextColor;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
    }

    static void StyleInput(Transform target, string placeholder)
    {
        if (target == null) return;
        SetPreferredHeight(target, 44);
        var input = target.GetComponent<TMP_InputField>();
        if (input == null) return;
        if (input.placeholder is TMP_Text hint)
        {
            Undo.RecordObject(hint, "Estilo do campo");
            hint.text = placeholder;
        }
        if (input.textComponent != null)
        {
            Undo.RecordObject(input.textComponent, "Estilo do campo");
            input.textComponent.fontSize = 20;
        }
    }

    static void StylePanel(Transform target)
    {
        var image = target.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "Estilo do painel");
            image.color = PanelColor;
        }

        var layout = target.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            Undo.RecordObject(layout, "Estilo do painel");
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 8f;
        }
    }

    static void PlaceInCorner(RectTransform rect, Vector2 corner, Vector2 position, float width)
    {
        if (rect == null) return;
        Undo.RecordObject(rect, "Posição do painel");
        rect.localScale = Vector3.one;
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
    }

    static void StretchToCanvas(Transform target)
    {
        var rect = target as RectTransform;
        if (rect == null) return;
        Undo.RecordObject(rect, "Ajustar contêiner");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static void SetPreferredHeight(Transform target, float height)
    {
        if (target == null) return;
        var element = target.GetComponent<LayoutElement>();
        if (element == null) element = Undo.AddComponent<LayoutElement>(target.gameObject);
        else Undo.RecordObject(element, "Altura");
        element.preferredHeight = height;
    }

    static Transform Find(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static void Warn(string name) =>
        Debug.LogWarning($"UIStyleTool: objeto '{name}' não encontrado; essa parte foi pulada.");
}
