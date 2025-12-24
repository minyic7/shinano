using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Shinano Character Expression & Customization Controller
/// 
/// ARCHITECTURE:
/// - Facial Parts (F_Parts): Eye effects like hearts, spirals, tears, etc.
/// - Facial Set (F_Set): Selects which expression set is active (0=Joy, 1=Calm, 2=Complex)
/// - Gesture Triggers: Within each F_Set, different gesture values trigger different expressions
/// - Costume/Body: Toggle visibility of clothing and body features
/// 
/// NOTE: Hand poses require VRChat SDK Avatar Masks. Without them, we control facial expressions only.
/// </summary>
public class ShinanoController : MonoBehaviour
{
    [Header("Character Reference")]
    public GameObject shinanoCharacter;
    public Animator characterAnimator;
    
    [Header("Camera Reference")]
    public Camera mainCamera;
    
    [Header("UI Settings")]
    public KeyCode togglePanelKey = KeyCode.Tab;
    public bool panelVisible = true;
    
    // UI References
    private Canvas uiCanvas;
    private GameObject panelRoot;
    private ScrollRect scrollRect;
    
    // State tracking
    private float characterRotation = 0f;
    private int currentFSet = 0;
    
    // Colors
    private Color panelBg = new Color(0.12f, 0.12f, 0.16f, 0.95f);
    private Color sectionColor = new Color(0.75f, 0.55f, 0.85f);
    private Color textColor = new Color(0.92f, 0.92f, 0.96f);
    private Color btnActive = new Color(0.45f, 0.55f, 0.65f);
    private Color btnNormal = new Color(0.28f, 0.28f, 0.35f);
    
    // Expression data
    private string[] eyeEffects = { "Default", "Cheek", "Heart", "Dead", "Spiral", "Sparkle", "White", "Tear", "Sweat" };
    
    // Facial expressions per set - these are triggered by gesture values 0-7
    private string[][] facialSets = new string[][] {
        // F_Set 0 (Joy): Gesture 0-7 triggers these
        new string[] { "Idle", "Smile1", "Joy2", "Wink1", "Kirakira", "EyeCls1", "Surprised", "Angry2" },
        // F_Set 1 (Calm): Gesture 0-7 triggers these
        new string[] { "Idle", "Smile2", "Joy1", "Wink2", "Nagomi", "EyeCls2", "Confuse", "Zito" },
        // F_Set 2 (Complex): Gesture 0-7 triggers these
        new string[] { "Idle", "Smile3", "Cry", "Grin", "Doya", "EyeCls3", "Kyoton", "Bitter" }
    };
    
    void Start()
    {
        FindCharacter();
        FindCamera();
        CreateUI();
    }
    
    void FindCharacter()
    {
        if (shinanoCharacter == null)
        {
            shinanoCharacter = GameObject.Find("Shinano_kisekae");
            if (shinanoCharacter == null)
                shinanoCharacter = GameObject.Find("Shinano");
        }
        
        if (shinanoCharacter != null && characterAnimator == null)
        {
            characterAnimator = shinanoCharacter.GetComponent<Animator>();
        }
    }
    
    void FindCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(togglePanelKey))
        {
            panelVisible = !panelVisible;
            if (panelRoot != null)
                panelRoot.SetActive(panelVisible);
        }
    }
    
    void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("ShinanoControlPanel");
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Ensure EventSystem exists
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
        
        // Create main panel - WIDER (400px)
        panelRoot = new GameObject("MainPanel");
        panelRoot.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 0.5f);
        panelRect.anchoredPosition = new Vector2(10, 0);
        panelRect.sizeDelta = new Vector2(400, -20); // Wider panel
        
        Image panelImg = panelRoot.AddComponent<Image>();
        panelImg.color = panelBg;
        
        // Create scroll view for content
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panelRoot.transform, false);
        
        RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(5, 5);
        scrollRt.offsetMax = new Vector2(-5, -5);
        
        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
        
        // Content container
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        
        RectTransform contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        
        scrollRect.content = contentRt;
        
        // Add mask
        Image scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0, 0, 0, 0);
        scrollObj.AddComponent<Mask>().showMaskGraphic = false;
        
        float y = 0;
        float panelWidth = 380; // Content width
        
        // Title
        AddLabel(contentRt, "✨ Shinano Controller", 20, y, sectionColor, panelWidth);
        y -= 28;
        AddLabel(contentRt, "Press TAB to toggle panel", 11, y, new Color(0.6f, 0.6f, 0.65f), panelWidth);
        y -= 35;
        
        // === EYE EFFECTS (F_Parts) ===
        AddSection(contentRt, "👁️ Eye Effects", ref y, panelWidth);
        AddLabel(contentRt, "Add special eye effects (hearts, tears, etc.)", 10, y, new Color(0.6f, 0.6f, 0.7f), panelWidth);
        y -= 18;
        AddButtonRow(contentRt, eyeEffects, ref y, panelWidth, (i) => SetAnimatorInt("F_Parts", i));
        
        // === FACIAL SET ===
        AddSection(contentRt, "😊 Facial Expression Set", ref y, panelWidth);
        AddLabel(contentRt, "Select expression style, then choose expression below", 10, y, new Color(0.6f, 0.6f, 0.7f), panelWidth);
        y -= 18;
        AddFSetSelector(contentRt, ref y, panelWidth);
        
        // === EXPRESSIONS (based on F_Set) ===
        y -= 10;
        AddLabel(contentRt, "Choose Expression:", 11, y, textColor, panelWidth);
        y -= 18;
        AddExpressionButtons(contentRt, ref y, panelWidth);
        
        // === COSTUME ===
        AddSection(contentRt, "👗 Costume", ref y, panelWidth);
        AddToggleGrid(contentRt, ref y, panelWidth, new string[]{"Sweater","Dress","Skirt","Tights","Boots"},
            new string[]{"Sweater","Dress","Skirt","Tights","Boots"}, true, true);
        AddToggleGrid(contentRt, ref y, panelWidth, new string[]{"Bra","Shorts"},
            new string[]{"Cloth_under_bra","Cloth_under_shorts"}, true, false);
        
        // === HAIR ===
        AddSection(contentRt, "💇 Hair Style", ref y, panelWidth);
        AddToggleGrid(contentRt, ref y, panelWidth, new string[]{"Side Bangs","Half-up"},
            new string[]{"Bangs","Half"}, true, true);
        AddSliderRow(contentRt, "Length", ref y, panelWidth, (v) => SetAnimatorFloat("Length", v), 0.5f);
        y -= 5;
        AddButtonRow(contentRt, new string[]{"Default","Braid","Side L","Side R","All"}, ref y, panelWidth,
            (i) => SetAnimatorInt("Hair", i));
        
        // === BODY ===
        AddSection(contentRt, "✨ Body Features", ref y, panelWidth);
        AddToggleGrid(contentRt, ref y, panelWidth, new string[]{"Fox Ears","Tail","Big Hip"},
            new string[]{"Ear","Tail","Hip"}, new bool[]{true,true,false}, new bool[]{true,true,false});
        AddToggleGrid(contentRt, ref y, panelWidth, new string[]{"Backlit Effect","AFK Mode"},
            new string[]{"Backlit","AFK"}, new bool[]{false,false}, new bool[]{false,false});
        AddSliderRow(contentRt, "Breast Size", ref y, panelWidth, (v) => SetAnimatorFloat("Breast", v), 0.5f);
        
        // === CAMERA ===
        AddSection(contentRt, "📷 View Controls", ref y, panelWidth);
        AddSliderRow(contentRt, "Rotate Character", ref y, panelWidth, (v) => {
            characterRotation = (v - 0.5f) * 360f;
            if (shinanoCharacter != null)
                shinanoCharacter.transform.rotation = Quaternion.Euler(0, characterRotation, 0);
        }, 0.5f);
        AddSliderRow(contentRt, "Camera Distance", ref y, panelWidth, (v) => {
            if (mainCamera != null)
            {
                Vector3 pos = mainCamera.transform.position;
                pos.z = Mathf.Lerp(0.5f, 5f, v);
                mainCamera.transform.position = pos;
            }
        }, 0.5f);
        
        // Set content height
        y -= 20;
        contentRt.sizeDelta = new Vector2(0, -y);
    }
    
    // === UI Building Methods ===
    
    void AddLabel(RectTransform parent, string text, int size, float y, Color color, float width)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(0, size + 8);
        
        Text txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = size;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
    }
    
    void AddSection(RectTransform parent, string title, ref float y, float width)
    {
        y -= 12;
        
        // Divider
        GameObject div = new GameObject("Divider");
        div.transform.SetParent(parent, false);
        RectTransform divRt = div.AddComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0, 1);
        divRt.anchorMax = new Vector2(1, 1);
        divRt.pivot = new Vector2(0.5f, 1);
        divRt.anchoredPosition = new Vector2(0, y);
        divRt.sizeDelta = new Vector2(-20, 1);
        div.AddComponent<Image>().color = new Color(0.5f, 0.4f, 0.6f, 0.4f);
        
        y -= 10;
        AddLabel(parent, title, 14, y, sectionColor, width);
        y -= 24;
    }
    
    void AddButtonRow(RectTransform parent, string[] labels, ref float y, float width, System.Action<int> onClick)
    {
        int cols = Mathf.Min(labels.Length, 5);
        float btnW = (width - 10) / cols - 4;
        float btnH = 28;
        
        for (int i = 0; i < labels.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            
            CreateButton(parent, labels[i], 10 + col * (btnW + 4), y - row * (btnH + 3), btnW, btnH, i, onClick);
        }
        
        int rows = (labels.Length + cols - 1) / cols;
        y -= rows * (btnH + 3) + 8;
    }
    
    void CreateButton(RectTransform parent, string label, float x, float yPos, float w, float h, int idx, System.Action<int> onClick)
    {
        GameObject btn = new GameObject("Btn_" + label);
        btn.transform.SetParent(parent, false);
        
        RectTransform rect = btn.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, yPos);
        rect.sizeDelta = new Vector2(w, h);
        
        Image img = btn.AddComponent<Image>();
        img.color = btnNormal;
        
        Button button = btn.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(() => onClick(idx));
        
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btn.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        
        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 11;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }
    
    private Button[] fsetButtons;
    private Button[] expressionButtons;
    private Text[] expressionLabels;
    
    void AddFSetSelector(RectTransform parent, ref float y, float width)
    {
        string[] labels = { "Joy Set", "Calm Set", "Complex Set" };
        float btnW = (width - 10) / 3 - 4;
        float btnH = 30;
        
        fsetButtons = new Button[3];
        
        for (int i = 0; i < 3; i++)
        {
            GameObject btn = new GameObject("FSet_" + i);
            btn.transform.SetParent(parent, false);
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10 + i * (btnW + 4), y);
            rect.sizeDelta = new Vector2(btnW, btnH);
            
            Image img = btn.AddComponent<Image>();
            img.color = i == 0 ? btnActive : btnNormal;
            
            Button button = btn.AddComponent<Button>();
            button.targetGraphic = img;
            fsetButtons[i] = button;
            
            int idx = i;
            button.onClick.AddListener(() => SelectFSet(idx));
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btn.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            
            Text txt = txtObj.AddComponent<Text>();
            txt.text = labels[i];
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 12;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
        
        y -= btnH + 8;
    }
    
    void AddExpressionButtons(RectTransform parent, ref float y, float width)
    {
        string[] labels = facialSets[0];
        int cols = 4;
        float btnW = (width - 10) / cols - 4;
        float btnH = 28;
        
        expressionButtons = new Button[8];
        expressionLabels = new Text[8];
        
        for (int i = 0; i < 8; i++)
        {
            int row = i / cols;
            int col = i % cols;
            
            GameObject btn = new GameObject("Expr_" + i);
            btn.transform.SetParent(parent, false);
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10 + col * (btnW + 4), y - row * (btnH + 3));
            rect.sizeDelta = new Vector2(btnW, btnH);
            
            Image img = btn.AddComponent<Image>();
            img.color = btnNormal;
            
            Button button = btn.AddComponent<Button>();
            button.targetGraphic = img;
            expressionButtons[i] = button;
            
            int idx = i;
            button.onClick.AddListener(() => {
                SetAnimatorInt("GestureLeft", idx);
                SetAnimatorInt("GestureRight", idx);
            });
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btn.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            
            Text txt = txtObj.AddComponent<Text>();
            txt.text = labels[i];
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 10;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            
            expressionLabels[i] = txt;
        }
        
        y -= 2 * (btnH + 3) + 8;
    }
    
    void SelectFSet(int setIndex)
    {
        currentFSet = setIndex;
        SetAnimatorInt("F_Set", setIndex);
        
        // Reset gestures
        SetAnimatorInt("GestureLeft", 0);
        SetAnimatorInt("GestureRight", 0);
        
        // Update button colors
        for (int i = 0; i < fsetButtons.Length; i++)
        {
            var img = fsetButtons[i].GetComponent<Image>();
            img.color = i == setIndex ? btnActive : btnNormal;
        }
        
        // Update expression labels
        string[] labels = facialSets[setIndex];
        for (int i = 0; i < expressionLabels.Length && i < labels.Length; i++)
        {
            expressionLabels[i].text = labels[i];
        }
    }
    
    void AddToggleGrid(RectTransform parent, ref float y, float width, string[] labels, string[] paramNames, bool defaultOn, bool invertLogic)
    {
        bool[] defaults = new bool[labels.Length];
        bool[] inverts = new bool[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            defaults[i] = defaultOn;
            inverts[i] = invertLogic;
        }
        AddToggleGrid(parent, ref y, width, labels, paramNames, defaults, inverts);
    }
    
    void AddToggleGrid(RectTransform parent, ref float y, float width, string[] labels, string[] paramNames, bool[] defaults, bool[] inverts)
    {
        int cols = Mathf.Min(labels.Length, 5);
        float toggleW = (width - 10) / cols - 4;
        float toggleH = 32;
        
        for (int i = 0; i < labels.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            
            bool isOn = defaults[i];
            
            GameObject tog = new GameObject("Toggle_" + labels[i]);
            tog.transform.SetParent(parent, false);
            
            RectTransform rect = tog.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10 + col * (toggleW + 4), y - row * (toggleH + 2));
            rect.sizeDelta = new Vector2(toggleW, toggleH);
            
            Image bg = tog.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.22f, 0.28f);
            
            // Label
            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(tog.transform, false);
            RectTransform lblRt = lblObj.AddComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0);
            lblRt.anchorMax = new Vector2(0.75f, 1);
            lblRt.offsetMin = new Vector2(4, 0);
            lblRt.offsetMax = Vector2.zero;
            
            Text lbl = lblObj.AddComponent<Text>();
            lbl.text = labels[i];
            lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbl.fontSize = 10;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = textColor;
            
            // Indicator
            GameObject ind = new GameObject("Indicator");
            ind.transform.SetParent(tog.transform, false);
            RectTransform indRt = ind.AddComponent<RectTransform>();
            indRt.anchorMin = new Vector2(1, 0.5f);
            indRt.anchorMax = new Vector2(1, 0.5f);
            indRt.pivot = new Vector2(1, 0.5f);
            indRt.anchoredPosition = new Vector2(-4, 0);
            indRt.sizeDelta = new Vector2(14, 14);
            
            Image indImg = ind.AddComponent<Image>();
            indImg.color = isOn ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
            
            Toggle toggle = tog.AddComponent<Toggle>();
            toggle.isOn = isOn;
            toggle.graphic = indImg;
            
            string param = paramNames[i];
            bool isMesh = param.StartsWith("Cloth_");
            bool invert = inverts[i];
            
            toggle.onValueChanged.AddListener((val) => {
                indImg.color = val ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
                if (isMesh)
                    ToggleMesh(param, val);
                else
                    SetAnimatorBool(param, invert ? !val : val);
            });
        }
        
        int rows = (labels.Length + cols - 1) / cols;
        y -= rows * (toggleH + 2) + 6;
    }
    
    void AddSliderRow(RectTransform parent, string label, ref float y, float width, System.Action<float> onChange, float defaultVal)
    {
        float labelW = 100;
        float sliderH = 20;
        
        // Label
        GameObject lblObj = new GameObject("Label_" + label);
        lblObj.transform.SetParent(parent, false);
        RectTransform lblRt = lblObj.AddComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0, 1);
        lblRt.anchorMax = new Vector2(0, 1);
        lblRt.pivot = new Vector2(0, 1);
        lblRt.anchoredPosition = new Vector2(10, y);
        lblRt.sizeDelta = new Vector2(labelW, sliderH);
        
        Text lbl = lblObj.AddComponent<Text>();
        lbl.text = label;
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize = 11;
        lbl.alignment = TextAnchor.MiddleLeft;
        lbl.color = textColor;
        
        // Slider background
        GameObject sliderBg = new GameObject("Slider_" + label);
        sliderBg.transform.SetParent(parent, false);
        RectTransform bgRt = sliderBg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 1);
        bgRt.anchorMax = new Vector2(1, 1);
        bgRt.pivot = new Vector2(0.5f, 1);
        bgRt.anchoredPosition = new Vector2(labelW/2, y);
        bgRt.sizeDelta = new Vector2(-labelW - 20, 18);
        
        Image bgImg = sliderBg.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.18f, 0.22f);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(sliderBg.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(defaultVal, 1);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = sectionColor;
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderBg.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(defaultVal, 0);
        handleRt.anchorMax = new Vector2(defaultVal, 1);
        handleRt.sizeDelta = new Vector2(10, 0);
        
        handle.AddComponent<Image>().color = Color.white;
        
        Slider slider = sliderBg.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.value = defaultVal;
        slider.onValueChanged.AddListener((v) => {
            fillRt.anchorMax = new Vector2(v, 1);
            onChange(v);
        });
        
        y -= sliderH + 8;
    }
    
    // === Animator Control Methods ===
    
    void SetAnimatorInt(string param, int val)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetInteger(param, val); }
            catch { Debug.LogWarning($"[Shinano] Param '{param}' not found"); }
        }
    }
    
    void SetAnimatorBool(string param, bool val)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetBool(param, val); }
            catch { Debug.LogWarning($"[Shinano] Param '{param}' not found"); }
        }
    }
    
    void SetAnimatorFloat(string param, float val)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetFloat(param, val); }
            catch { Debug.LogWarning($"[Shinano] Param '{param}' not found"); }
        }
    }
    
    void ToggleMesh(string meshName, bool visible)
    {
        if (shinanoCharacter == null) return;
        
        foreach (Transform child in shinanoCharacter.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == meshName)
            {
                var smr = child.GetComponent<SkinnedMeshRenderer>();
                if (smr != null) { smr.enabled = visible; return; }
                
                var mr = child.GetComponent<MeshRenderer>();
                if (mr != null) { mr.enabled = visible; return; }
                
                child.gameObject.SetActive(visible);
                return;
            }
        }
    }
}
