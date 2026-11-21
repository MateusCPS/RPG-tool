using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
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
    static readonly Color AccentNormal = new Color32(52, 104, 190, 255);
    static readonly Color AccentHighlighted = new Color32(66, 124, 215, 255);
    static readonly Color InputColor = new Color32(20, 20, 26, 255);
    static readonly Color PlaceholderColor = new Color32(140, 140, 150, 255);
    static readonly Color BackdropColor = new Color32(0, 0, 0, 150);
    static readonly Color WindowColor = new Color32(30, 30, 36, 245);

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
            ApplyCalibrationRows(root, settings);
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

        ApplyCreatePanel(root);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        Debug.Log("UIStyleTool: estilo aplicado. Ctrl+Z desfaz tudo de uma vez.");
    }

    static void ApplyCreatePanel(Transform root)
    {
        Transform panel = Find(root, "TokenCreatePanel");
        if (panel == null) { Warn("TokenCreatePanel"); return; }

        StretchToCanvas(panel);
        var backdrop = panel.GetComponent<Image>();
        if (backdrop != null)
        {
            Undo.RecordObject(backdrop, "Fundo do painel");
            backdrop.sprite = null;
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;
        }
        var oldLayout = panel.GetComponent<VerticalLayoutGroup>();
        if (oldLayout != null) Undo.DestroyObjectImmediate(oldLayout);
        var oldFitter = panel.GetComponent<ContentSizeFitter>();
        if (oldFitter != null) Undo.DestroyObjectImmediate(oldFitter);

        RectTransform window = FindOrCreateChild(panel, "Window", typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RecordObject(window, "Janela");
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.anchoredPosition = Vector2.zero;
        window.localScale = Vector3.one;
        window.sizeDelta = new Vector2(420f, window.sizeDelta.y);

        var windowImage = window.GetComponent<Image>();
        Undo.RecordObject(windowImage, "Janela");
        windowImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        windowImage.type = Image.Type.Sliced;
        windowImage.color = WindowColor;

        var windowLayout = window.GetComponent<VerticalLayoutGroup>();
        Undo.RecordObject(windowLayout, "Janela");
        windowLayout.padding = new RectOffset(24, 24, 22, 24);
        windowLayout.spacing = 8f;
        windowLayout.childAlignment = TextAnchor.UpperLeft;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        var fitter = window.GetComponent<ContentSizeFitter>();
        Undo.RecordObject(fitter, "Janela");
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Transform title = CreateText(window, "Title", "Novo token", 28, true);
        SetPreferredHeight(title, 40);

        Transform nameLabel = CreateText(window, "NameLabel", "Nome", 18, false);
        Transform nameField = Find(panel, "Nome do Token");
        Transform sizeLabel = CreateText(window, "SizeLabel", "Tamanho", 18, false);
        Transform sizeField = Find(panel, "Dropdown");
        Transform hpLabel = CreateText(window, "HpLabel", "Pontos de vida", 18, false);
        Transform hpField = Find(panel, "Pontos de Vida");
        Transform imageLabel = CreateText(window, "ImageLabel", "Imagem", 18, false);

        RectTransform imageRow = FindOrCreateChild(window, "ImageRow", typeof(HorizontalLayoutGroup));
        var rowLayout = imageRow.GetComponent<HorizontalLayoutGroup>();
        Undo.RecordObject(rowLayout, "Linha da imagem");
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        SetPreferredHeight(imageRow, 96);

        RectTransform frame = FindOrCreateChild(imageRow, "PreviewFrame", typeof(Image), typeof(LayoutElement));
        var frameImage = frame.GetComponent<Image>();
        Undo.RecordObject(frameImage, "Moldura");
        frameImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        frameImage.type = Image.Type.Sliced;
        frameImage.color = InputColor;
        var frameElement = frame.GetComponent<LayoutElement>();
        Undo.RecordObject(frameElement, "Moldura");
        frameElement.minWidth = frameElement.preferredWidth = 96f;
        frameElement.minHeight = frameElement.preferredHeight = 96f;
        frameElement.flexibleWidth = 0f;

        Transform preview = Find(panel, "Previa da Imagem");
        if (preview != null)
        {
            Undo.SetTransformParent(preview, frame, "Prévia");
            var previewRect = preview as RectTransform;
            Undo.RecordObject(previewRect, "Prévia");
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.offsetMin = new Vector2(4f, 4f);
            previewRect.offsetMax = new Vector2(-4f, -4f);
            previewRect.localScale = Vector3.one;
            var previewElement = preview.GetComponent<LayoutElement>();
            if (previewElement != null) Undo.DestroyObjectImmediate(previewElement);
        }
        else Warn("Previa da Imagem");

        RectTransform imageInfo = FindOrCreateChild(imageRow, "ImageInfo", typeof(VerticalLayoutGroup), typeof(LayoutElement));
        var infoLayout = imageInfo.GetComponent<VerticalLayoutGroup>();
        Undo.RecordObject(infoLayout, "Informações da imagem");
        infoLayout.spacing = 6f;
        infoLayout.childAlignment = TextAnchor.MiddleLeft;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;
        var infoElement = imageInfo.GetComponent<LayoutElement>();
        Undo.RecordObject(infoElement, "Informações da imagem");
        infoElement.flexibleWidth = 1f;

        Transform chooseButton = Find(panel, "Escolher Imagem");
        Transform fileName = Find(panel, "Nome do Arquivo");
        MoveTo(chooseButton, imageInfo, 0);
        MoveTo(fileName, imageInfo, 1);
        StyleButton(chooseButton, ButtonNormal, ButtonHighlighted, 44, 20, "Escolher imagem");
        StyleText(fileName, 16, false);
        if (fileName != null)
        {
            var fileText = fileName.GetComponent<TMP_Text>();
            Undo.RecordObject(fileText, "Nome do arquivo");
            fileText.color = PlaceholderColor;
            fileText.textWrappingMode = TextWrappingModes.NoWrap;
            fileText.overflowMode = TextOverflowModes.Ellipsis;
            fileText.alignment = TextAlignmentOptions.Left;
            SetPreferredHeight(fileName, 24);
        }

        RectTransform buttonsRow = FindOrCreateChild(window, "ButtonsRow", typeof(HorizontalLayoutGroup));
        var buttonsLayout = buttonsRow.GetComponent<HorizontalLayoutGroup>();
        Undo.RecordObject(buttonsLayout, "Botões");
        buttonsLayout.spacing = 10f;
        buttonsLayout.padding = new RectOffset(0, 0, 10, 0);
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childForceExpandWidth = true;
        buttonsLayout.childForceExpandHeight = false;
        SetPreferredHeight(buttonsRow, 58);

        Transform cancel = Find(panel, "Cancelar");
        Transform create = Find(panel, "Criar");
        MoveTo(cancel, buttonsRow, 0);
        MoveTo(create, buttonsRow, 1);
        StyleButton(cancel, ButtonNormal, ButtonHighlighted, 48, 22, "Cancelar");
        StyleButton(create, AccentNormal, AccentHighlighted, 48, 22, "Criar");

        Transform[] order = { title, nameLabel, nameField, sizeLabel, sizeField, hpLabel, hpField, imageLabel, imageRow, buttonsRow };
        for (int i = 0; i < order.Length; i++) MoveTo(order[i], window, i);

        StyleDarkInput(nameField, "Nome do token");
        StyleDarkInput(hpField, "10");
        StyleDarkDropdown(sizeField);
        StyleDarkInput(Find(root, "AmountInput"), "Valor");
    }

    [MenuItem("Tools/Mesa Virtual/Montar lista de jogadores")]
    static void SetupPlayers()
    {
        GameObject canvasObject = GameObject.Find("Canvas");
        var session = Object.FindAnyObjectByType<SessionService>();
        var sessionPanel = Object.FindAnyObjectByType<SessionPanel>(FindObjectsInactive.Include);
        var tableSync = Object.FindAnyObjectByType<TableSync>(FindObjectsInactive.Include);
        if (canvasObject == null || session == null || sessionPanel == null || tableSync == null)
        {
            Debug.LogError("UIStyleTool: Canvas, SessionService, SessionPanel ou TableSync não encontrado na cena.");
            return;
        }

        Undo.SetCurrentGroupName("Montar lista de jogadores");
        int group = Undo.GetCurrentGroup();
        Transform root = canvasObject.transform;

        var roster = tableSync.GetComponent<PlayerRoster>();
        if (roster == null) roster = Undo.AddComponent<PlayerRoster>(tableSync.gameObject);
        SetReference(roster, "session", session);

        RectTransform toolbar = FindOrCreateToolbar(root);
        RectTransform spacer = FindOrCreateChild(toolbar, "Spacer", typeof(LayoutElement));
        var spacerElement = spacer.GetComponent<LayoutElement>();
        Undo.RecordObject(spacerElement, "Espaço");
        spacerElement.flexibleHeight = 1f;
        Transform leave = CreateButton(toolbar, "LeaveRoom", "Sair da sala");
        StyleButton(leave, DamageNormal, DamageHighlighted, 48, 20, "Sair da sala");
        var leaveElement = leave.GetComponent<LayoutElement>();
        Undo.RecordObject(leaveElement, "Botão sair");
        leaveElement.flexibleWidth = 0f;
        MoveTo(spacer, toolbar, toolbar.childCount - 1);
        MoveTo(leave, toolbar, toolbar.childCount - 1);

        var leaveClick = leave.GetComponent<Button>();
        Undo.RecordObject(leaveClick, "Ação do botão");
        while (leaveClick.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(leaveClick.onClick, 0);
        UnityEventTools.AddPersistentListener(leaveClick.onClick, new UnityAction(sessionPanel.LeaveRoom));
        SetReference(sessionPanel, "leaveButton", leaveClick);

        RectTransform listPanel = FindOrCreateChild(root, "PlayersList", typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        PlaceInCorner(listPanel, new Vector2(1f, 0f), new Vector2(-16f, 16f), 240f);
        var listImage = listPanel.GetComponent<Image>();
        Undo.RecordObject(listImage, "Lista de jogadores");
        listImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        listImage.type = Image.Type.Sliced;
        listImage.raycastTarget = false;
        var listLayout = listPanel.GetComponent<VerticalLayoutGroup>();
        Undo.RecordObject(listLayout, "Lista de jogadores");
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        StylePanel(listPanel);
        var listFitter = listPanel.GetComponent<ContentSizeFitter>();
        Undo.RecordObject(listFitter, "Lista de jogadores");
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Transform listText = CreateText(listPanel, "ListText", "Na mesa (1)", 18, false);
        var listTmp = listText.GetComponent<TMP_Text>();
        Undo.RecordObject(listTmp, "Lista de jogadores");
        listTmp.textWrappingMode = TextWrappingModes.NoWrap;
        listTmp.overflowMode = TextOverflowModes.Ellipsis;
        listTmp.alignment = TextAlignmentOptions.TopLeft;

        Transform notice = CreateText(root, "PlayerNotice", "", 22, true);
        var noticeRect = (RectTransform)notice;
        Undo.RecordObject(noticeRect, "Aviso");
        noticeRect.anchorMin = noticeRect.anchorMax = noticeRect.pivot = new Vector2(0.5f, 1f);
        noticeRect.anchoredPosition = new Vector2(0f, -16f);
        noticeRect.sizeDelta = new Vector2(600f, 40f);
        noticeRect.localScale = Vector3.one;
        var noticeTmp = notice.GetComponent<TMP_Text>();
        Undo.RecordObject(noticeTmp, "Aviso");
        noticeTmp.alignment = TextAlignmentOptions.Center;
        noticeTmp.outlineWidth = 0.2f;
        noticeTmp.outlineColor = new Color32(0, 0, 0, 255);

        GameObject controller = GameObject.Find("PlayersController");
        if (controller == null)
        {
            controller = new GameObject("PlayersController");
            Undo.RegisterCreatedObjectUndo(controller, "Criar PlayersController");
        }
        var playersPanel = controller.GetComponent<PlayersPanel>();
        if (playersPanel == null) playersPanel = Undo.AddComponent<PlayersPanel>(controller);
        SetReference(playersPanel, "session", session);
        SetReference(playersPanel, "roster", roster);
        SetReference(playersPanel, "listPanel", listPanel.gameObject);
        SetReference(playersPanel, "listText", listTmp);
        SetReference(playersPanel, "noticeText", noticeTmp);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        Debug.Log("UIStyleTool: lista de jogadores montada. Ctrl+Z desfaz tudo de uma vez.");
    }

    [MenuItem("Tools/Mesa Virtual/Montar botão de cancelar reconexão")]
    static void SetupReconnectCancel()
    {
        var sessionPanel = Object.FindAnyObjectByType<SessionPanel>(FindObjectsInactive.Include);
        var window = sessionPanel != null
            ? new SerializedObject(sessionPanel).FindProperty("window").objectReferenceValue as GameObject
            : null;
        Transform row = window != null ? Find(window.transform, "ButtonsRow") : null;
        if (sessionPanel == null || row == null)
        {
            Debug.LogError("UIStyleTool: SessionPanel ou a linha de botões da janela de sala não encontrados.");
            return;
        }

        Undo.SetCurrentGroupName("Montar botão de cancelar reconexão");
        int group = Undo.GetCurrentGroup();

        Transform cancel = CreateButton(row, "CancelarReconexao", "Cancelar");
        StyleButton(cancel, ButtonNormal, ButtonHighlighted, 48, 22, "Cancelar");
        MoveTo(cancel, row, row.childCount - 1);

        var click = cancel.GetComponent<Button>();
        Undo.RecordObject(click, "Ação do botão");
        while (click.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(click.onClick, 0);
        UnityEventTools.AddPersistentListener(click.onClick, new UnityAction(sessionPanel.CancelReconnect));
        SetReference(sessionPanel, "cancelButton", click);

        Undo.RecordObject(cancel.gameObject, "Esconder botão");
        cancel.gameObject.SetActive(false);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(sessionPanel.gameObject.scene);
        Debug.Log("UIStyleTool: botão de cancelar reconexão montado.");
    }

    [MenuItem("Tools/Mesa Virtual/Montar versão e relatório")]
    static void SetupVersionReport()
    {
        GameObject canvasObject = GameObject.Find("Canvas");
        var session = Object.FindAnyObjectByType<SessionService>();
        var sessionPanel = Object.FindAnyObjectByType<SessionPanel>(FindObjectsInactive.Include);
        var roster = Object.FindAnyObjectByType<PlayerRoster>(FindObjectsInactive.Include);
        var window = sessionPanel != null
            ? new SerializedObject(sessionPanel).FindProperty("window").objectReferenceValue as GameObject
            : null;
        Transform row = window != null ? Find(window.transform, "ButtonsRow") : null;
        if (canvasObject == null || session == null || roster == null || row == null)
        {
            Debug.LogError("UIStyleTool: Canvas, SessionService, PlayerRoster ou a janela de sala não encontrados.");
            return;
        }

        Undo.SetCurrentGroupName("Montar versão e relatório");
        int group = Undo.GetCurrentGroup();
        Transform root = canvasObject.transform;
        Transform sessionWindow = row.parent;

        GameObject controller = GameObject.Find("ReportController");
        if (controller == null)
        {
            controller = new GameObject("ReportController");
            Undo.RegisterCreatedObjectUndo(controller, "Criar ReportController");
        }
        var log = controller.GetComponent<LogRecorder>();
        if (log == null) log = Undo.AddComponent<LogRecorder>(controller);
        var report = controller.GetComponent<ProblemReport>();
        if (report == null) report = Undo.AddComponent<ProblemReport>(controller);

        Transform windowReport = CreateButton(sessionWindow, "RelatorioSala", "Copiar relatório");
        StyleButton(windowReport, ButtonNormal, ButtonHighlighted, 36, 16, "Copiar relatório");
        MoveTo(windowReport, sessionWindow, sessionWindow.childCount - 1);
        WireClick(windowReport, report);

        Transform version = CreateText(sessionWindow, "VersionText", "v0.0.0", 14, false);
        var versionTmp = version.GetComponent<TMP_Text>();
        Undo.RecordObject(versionTmp, "Versão");
        versionTmp.alignment = TextAlignmentOptions.Center;
        versionTmp.color = PlaceholderColor;
        MoveTo(version, sessionWindow, sessionWindow.childCount - 1);
        SetReference(sessionPanel, "versionText", versionTmp);

        RectTransform toolbar = FindOrCreateToolbar(root);
        Transform toolbarReport = CreateButton(toolbar, "RelatorioMesa", "Copiar relatório");
        StyleButton(toolbarReport, ButtonNormal, ButtonHighlighted, 40, 16, "Copiar relatório");
        Transform leave = toolbar.Find("LeaveRoom");
        MoveTo(toolbarReport, toolbar, leave != null ? leave.GetSiblingIndex() : toolbar.childCount - 1);
        WireClick(toolbarReport, report);

        Transform feedback = CreateText(root, "ReportFeedback", "", 20, true);
        var feedbackRect = (RectTransform)feedback;
        Undo.RecordObject(feedbackRect, "Aviso do relatório");
        feedbackRect.anchorMin = feedbackRect.anchorMax = feedbackRect.pivot = new Vector2(0.5f, 1f);
        feedbackRect.anchoredPosition = new Vector2(0f, -60f);
        feedbackRect.sizeDelta = new Vector2(700f, 36f);
        feedbackRect.localScale = Vector3.one;
        var feedbackTmp = feedback.GetComponent<TMP_Text>();
        Undo.RecordObject(feedbackTmp, "Aviso do relatório");
        feedbackTmp.alignment = TextAlignmentOptions.Center;
        feedbackTmp.outlineWidth = 0.2f;
        feedbackTmp.outlineColor = new Color32(0, 0, 0, 255);

        SetReference(report, "log", log);
        SetReference(report, "session", session);
        SetReference(report, "roster", roster);
        SetReference(report, "feedbackText", feedbackTmp);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        Debug.Log("UIStyleTool: versão e relatório montados.");
    }

    [MenuItem("Tools/Mesa Virtual/Montar barra de combate")]
    static void SetupCombatBar()
    {
        GameObject canvasObject = GameObject.Find("Canvas");
        var tokens = Object.FindAnyObjectByType<TokenManager>();
        var masterOnly = Object.FindAnyObjectByType<MasterOnlyGroup>(FindObjectsInactive.Include);
        if (canvasObject == null || tokens == null || masterOnly == null)
        {
            Debug.LogError("UIStyleTool: Canvas, TokenManager ou MasterOnlyGroup não encontrados.");
            return;
        }

        Undo.SetCurrentGroupName("Montar barra de combate");
        int group = Undo.GetCurrentGroup();
        Transform root = canvasObject.transform;

        RectTransform bar = FindOrCreateChild(root, "CombatBar", typeof(HorizontalLayoutGroup), typeof(CombatBar));
        Undo.RecordObject(bar, "Barra de combate");
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 16f);
        bar.sizeDelta = new Vector2(640f, 48f);
        bar.localScale = Vector3.one;

        var layout = bar.GetComponent<HorizontalLayoutGroup>();
        Undo.RecordObject(layout, "Barra de combate");
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        Transform status = CreateText(bar, "Status", "Combate · Rodada 1", 22, true);
        var statusTmp = status.GetComponent<TMP_Text>();
        Undo.RecordObject(statusTmp, "Status do combate");
        statusTmp.alignment = TextAlignmentOptions.Center;
        statusTmp.outlineWidth = 0.2f;
        statusTmp.outlineColor = new Color32(0, 0, 0, 255);
        var statusElement = status.GetComponent<LayoutElement>();
        if (statusElement == null) statusElement = Undo.AddComponent<LayoutElement>(status.gameObject);
        Undo.RecordObject(statusElement, "Largura do status");
        statusElement.preferredWidth = 260f;

        Transform toggle = CreateButton(bar, "CombatButton", "Iniciar combate");
        StyleButton(toggle, AccentNormal, AccentHighlighted, 44, 18, "Iniciar combate");
        Transform newRound = CreateButton(bar, "NewRoundButton", "Nova rodada");
        StyleButton(newRound, ButtonNormal, ButtonHighlighted, 44, 18, "Nova rodada");
        MoveTo(status, bar, 0);
        MoveTo(toggle, bar, 1);
        MoveTo(newRound, bar, 2);

        var combatBar = bar.GetComponent<CombatBar>();
        WireClick(toggle, new UnityAction(combatBar.ToggleCombat));
        WireClick(newRound, new UnityAction(combatBar.NewRound));

        SetReference(combatBar, "tokens", tokens);
        SetReference(combatBar, "statusText", statusTmp);
        SetReference(combatBar, "toggleLabel", toggle.GetComponentInChildren<TMP_Text>(true));
        SetReference(combatBar, "newRoundButton", newRound.GetComponent<Button>());

        AddMasterOnlyTarget(masterOnly, toggle.gameObject);
        AddMasterOnlyTarget(masterOnly, newRound.gameObject);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        Debug.Log("UIStyleTool: barra de combate montada.");
    }

    static void WireClick(Transform target, UnityAction action)
    {
        var click = target.GetComponent<Button>();
        Undo.RecordObject(click, "Ação do botão");
        while (click.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(click.onClick, 0);
        UnityEventTools.AddPersistentListener(click.onClick, action);
    }

    static void AddMasterOnlyTarget(MasterOnlyGroup masterOnly, GameObject target)
    {
        var serialized = new SerializedObject(masterOnly);
        SerializedProperty targets = serialized.FindProperty("targets");
        for (int i = 0; i < targets.arraySize; i++)
            if (targets.GetArrayElementAtIndex(i).objectReferenceValue == target) return;
        targets.arraySize++;
        targets.GetArrayElementAtIndex(targets.arraySize - 1).objectReferenceValue = target;
        serialized.ApplyModifiedProperties();
    }

    static void WireClick(Transform target, ProblemReport report)
    {
        var click = target.GetComponent<Button>();
        Undo.RecordObject(click, "Ação do botão");
        while (click.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(click.onClick, 0);
        UnityEventTools.AddPersistentListener(click.onClick, new UnityAction(report.Copy));
    }

    static void SetReference(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) { Warn(field); return; }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    static readonly float[] CalibrationSteps = { -10f, -1f, 1f, 10f };

    static void ApplyCalibrationRows(Transform root, Transform settings)
    {
        var panel = root.GetComponentInChildren<TableSettingsPanel>(true);
        if (panel == null) { Warn("TableSettingsPanel (script)"); return; }

        Transform pixelsLabel = Find(settings, "PixelsLabel");
        Transform offsetLabel = Find(settings, "OffsetLabel");
        Activate(pixelsLabel);
        Activate(offsetLabel);

        RectTransform pixelsRow = CreateStepRow(settings, "PixelsRow", null, new UnityAction<float>(panel.ChangePixels));
        RectTransform offsetXRow = CreateStepRow(settings, "OffsetXRow", "X", new UnityAction<float>(panel.ChangeOffsetX));
        RectTransform offsetYRow = CreateStepRow(settings, "OffsetYRow", "Y", new UnityAction<float>(panel.ChangeOffsetY));

        MoveTo(pixelsLabel, settings, 2);
        MoveTo(pixelsRow, settings, 3);
        MoveTo(offsetLabel, settings, 4);
        MoveTo(offsetXRow, settings, 5);
        MoveTo(offsetYRow, settings, 6);
    }

    static void Activate(Transform target)
    {
        if (target == null || target.gameObject.activeSelf) return;
        Undo.RecordObject(target.gameObject, "Mostrar calibração");
        target.gameObject.SetActive(true);
    }

    static RectTransform CreateStepRow(Transform parent, string name, string prefix, UnityAction<float> action)
    {
        RectTransform row = FindOrCreateChild(parent, name, typeof(HorizontalLayoutGroup));
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        Undo.RecordObject(layout, "Linha de calibração");
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        SetPreferredHeight(row, 40);

        if (prefix != null)
        {
            Transform axis = CreateText(row, "Axis", prefix, 18, true);
            var axisText = axis.GetComponent<TMP_Text>();
            axisText.alignment = TextAlignmentOptions.Center;
            var axisElement = axis.GetComponent<LayoutElement>();
            if (axisElement == null) axisElement = Undo.AddComponent<LayoutElement>(axis.gameObject);
            else Undo.RecordObject(axisElement, "Largura");
            axisElement.preferredWidth = 24f;
            axisElement.flexibleWidth = 0f;
            axis.SetSiblingIndex(0);
        }

        for (int i = 0; i < CalibrationSteps.Length; i++)
        {
            float step = CalibrationSteps[i];
            string label = step > 0f ? $"+{step:0}" : $"{step:0}";
            Transform button = CreateButton(row, "Step" + i, label);

            var click = button.GetComponent<Button>();
            Undo.RecordObject(click, "Ação do botão");
            while (click.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(click.onClick, 0);
            UnityEventTools.AddFloatPersistentListener(click.onClick, action, step);
        }

        return row;
    }

    static Transform CreateButton(Transform parent, string name, string label)
    {
        RectTransform rect = FindOrCreateChild(parent, name, typeof(Image), typeof(Button));

        var button = rect.GetComponent<Button>();
        Undo.RecordObject(button, "Botão");
        button.targetGraphic = rect.GetComponent<Image>();

        var text = (RectTransform)CreateText(rect, "Text", label, 18, false);
        Undo.RecordObject(text, "Texto do botão");
        text.anchorMin = Vector2.zero;
        text.anchorMax = Vector2.one;
        text.offsetMin = Vector2.zero;
        text.offsetMax = Vector2.zero;

        StyleButton(rect, ButtonNormal, ButtonHighlighted, 40, 18, label);

        var element = rect.GetComponent<LayoutElement>();
        Undo.RecordObject(element, "Largura");
        element.flexibleWidth = 1f;
        return rect;
    }

    static void MoveTo(Transform target, Transform parent, int index)
    {
        if (target == null) return;
        if (target.parent != parent) Undo.SetTransformParent(target, parent, "Organizar painel");
        target.SetSiblingIndex(index);
        Undo.RecordObject(target, "Organizar painel");
        target.localScale = Vector3.one;
    }

    static RectTransform FindOrCreateChild(Transform parent, string name, params System.Type[] components)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            foreach (var type in components)
                if (existing.GetComponent(type) == null) Undo.AddComponent(existing.gameObject, type);
            return existing as RectTransform;
        }

        var types = new System.Collections.Generic.List<System.Type> { typeof(RectTransform) };
        foreach (var type in components)
            if (typeof(Graphic).IsAssignableFrom(type) && !types.Contains(typeof(CanvasRenderer))) types.Add(typeof(CanvasRenderer));
        types.AddRange(components);

        var go = new GameObject(name, types.ToArray());
        Undo.RegisterCreatedObjectUndo(go, "Criar " + name);
        go.layer = parent.gameObject.layer;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static Transform CreateText(Transform parent, string name, string content, float size, bool bold)
    {
        RectTransform rect = FindOrCreateChild(parent, name, typeof(TextMeshProUGUI));
        var text = rect.GetComponent<TextMeshProUGUI>();
        Undo.RecordObject(text, "Texto");
        text.text = content;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        StyleText(rect, size, bold);
        return rect;
    }

    static void StyleDarkInput(Transform target, string placeholder)
    {
        if (target == null) return;
        SetPreferredHeight(target, 44);
        var image = target.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "Campo");
            image.color = InputColor;
        }
        var input = target.GetComponent<TMP_InputField>();
        if (input == null) return;
        if (input.textComponent != null)
        {
            Undo.RecordObject(input.textComponent, "Campo");
            input.textComponent.fontSize = 20;
            input.textComponent.color = TextColor;
        }
        if (input.placeholder is TMP_Text hint)
        {
            Undo.RecordObject(hint, "Campo");
            hint.text = placeholder;
            hint.fontSize = 20;
            hint.color = PlaceholderColor;
        }
        Undo.RecordObject(input, "Campo");
        input.caretColor = TextColor;
        input.customCaretColor = true;
    }

    static void StyleDarkDropdown(Transform target)
    {
        if (target == null) return;
        SetPreferredHeight(target, 44);
        var image = target.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "Lista");
            image.color = InputColor;
        }
        var dropdown = target.GetComponent<TMP_Dropdown>();
        if (dropdown != null && dropdown.captionText != null)
        {
            Undo.RecordObject(dropdown.captionText, "Lista");
            dropdown.captionText.fontSize = 20;
            dropdown.captionText.color = TextColor;
        }
        Transform arrow = target.Find("Arrow");
        if (arrow != null && arrow.GetComponent<Image>() is Image arrowImage)
        {
            Undo.RecordObject(arrowImage, "Lista");
            arrowImage.color = TextColor;
        }
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
