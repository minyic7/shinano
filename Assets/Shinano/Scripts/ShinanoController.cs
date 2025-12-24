using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Shinano Character Expression & Customization Controller
/// Controls facial expressions, hand poses, costume, hair, and body features
/// </summary>
public class ShinanoController : MonoBehaviour
{
    [Header("Character Reference")]
    public GameObject shinanoCharacter;
    public Animator characterAnimator;
    
    [Header("Camera Reference")]
    public Camera mainCamera;
    
    [Header("Hand Pose Assets")]
    public AvatarMask leftHandMask;
    public AvatarMask rightHandMask;
    public AnimationClip[] leftHandClips;
    public AnimationClip[] rightHandClips;
    
    [Header("UI Settings")]
    public KeyCode togglePanelKey = KeyCode.Tab;
    public bool panelVisible = true;
    
    // UI References
    private Canvas uiCanvas;
    private GameObject panelRoot;
    
    // State tracking
    private float characterRotation = 0f;
    private int currentFSet = 0;
    private int currentLeftHandPose = 0;
    private int currentRightHandPose = 0;
    
    // Colors
    private Color panelBg = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    private Color sectionColor = new Color(0.7f, 0.5f, 0.8f);
    private Color textColor = new Color(0.9f, 0.9f, 0.95f);
    
    // Expression data
    private string[] eyeEffects = { "Default", "Cheek", "Heart", "Dead", "Spiral", "Sparkle", "White", "Tear", "Sweat" };
    private string[] handPoseNames = { "Open", "Fist", "Point", "Victory", "Rock", "Gun", "Thumb" };
    
    // F_Set 0: Joy expressions
    private string[] gestureSet0 = { "Default", "Fist", "EyeCls1", "Point", "Wink1", "Rock", "Smile1", "Joy2" };
    // F_Set 1: Calm expressions
    private string[] gestureSet1 = { "Default", "Confuse", "EyeCls2", "Nagomi", "Wink2", "Zito", "Smile2", "Joy1" };
    // F_Set 2: Complex expressions
    private string[] gestureSet2 = { "Default", "Bitter", "EyeCls3", "Kyoton", "Grin", "Doya", "Smile3", "Cry" };
    
    private Text[] leftGestureLabels;
    private Text[] rightGestureLabels;
    
    void Start()
    {
        Debug.Log("[Shinano] Controller starting...");
        FindCharacter();
        FindCamera();
        LoadHandPoseAssets();
        CreateUI();
        Debug.Log($"[Shinano] Setup complete. Character: {(shinanoCharacter != null ? shinanoCharacter.name : "NOT FOUND")}, Animator: {(characterAnimator != null ? "OK" : "NOT FOUND")}");
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
    
    void LoadHandPoseAssets()
    {
        #if UNITY_EDITOR
        // Load masks
        if (leftHandMask == null)
            leftHandMask = AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/Shinano/Animation/Masks/LeftHand.mask");
        if (rightHandMask == null)
            rightHandMask = AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/Shinano/Animation/Masks/RightHand.mask");
        
        // Load hand pose clips
        string[] clipNames = { "Hand open", "Fist", "Finger point", "Victory", "Rockn roll", "Hand gun", "Thums up" };
        leftHandClips = new AnimationClip[clipNames.Length];
        rightHandClips = new AnimationClip[clipNames.Length];
        
        for (int i = 0; i < clipNames.Length; i++)
        {
            string path = $"Assets/Shinano/Animation/Gesture/{clipNames[i]}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                leftHandClips[i] = clip;
                rightHandClips[i] = clip;
            }
        }
        #endif
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
    
    string[] GetCurrentGestureSet()
    {
        switch (currentFSet)
        {
            case 1: return gestureSet1;
            case 2: return gestureSet2;
            default: return gestureSet0;
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
        
        // Create main panel - left side of screen, wider (400px)
        panelRoot = new GameObject("MainPanel");
        panelRoot.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 0.5f);
        panelRect.anchoredPosition = new Vector2(10, 0);
        panelRect.sizeDelta = new Vector2(400, -40);
        
        Image panelImg = panelRoot.AddComponent<Image>();
        panelImg.color = panelBg;
        
        float y = -10;
        
        // Title
        AddLabel(panelRoot.transform, "✨ Shinano Controller", 18, y, sectionColor);
        y -= 25;
        AddLabel(panelRoot.transform, "Press TAB to toggle panel", 10, y, new Color(0.6f, 0.6f, 0.6f));
        y -= 30;
        
        // === EYE EFFECTS ===
        AddSectionHeader(panelRoot.transform, "👁️ Eye Effects", ref y);
        AddButtonGrid(panelRoot.transform, eyeEffects, 5, ref y, (i) => SetAnimatorInt("F_Parts", i));
        
        // === FACIAL SET ===
        AddSectionHeader(panelRoot.transform, "😊 Expression Style", ref y);
        AddFSetButtons(panelRoot.transform, ref y);
        
        // === FACIAL EXPRESSIONS ===
        AddSectionHeader(panelRoot.transform, "🎭 Facial Expressions", ref y);
        AddLabel(panelRoot.transform, "Left Trigger:", 10, y, new Color(0.7f, 0.7f, 0.8f));
        y -= 15;
        leftGestureLabels = AddGestureGrid(panelRoot.transform, GetCurrentGestureSet(), ref y, (i) => SetAnimatorInt("GestureLeft", i));
        AddLabel(panelRoot.transform, "Right Trigger:", 10, y, new Color(0.7f, 0.7f, 0.8f));
        y -= 15;
        rightGestureLabels = AddGestureGrid(panelRoot.transform, GetCurrentGestureSet(), ref y, (i) => SetAnimatorInt("GestureRight", i));
        
        // === HAND POSES ===
        AddSectionHeader(panelRoot.transform, "🖐️ Hand Poses (Editor Only)", ref y);
        AddLabel(panelRoot.transform, "Left Hand:", 10, y, new Color(0.7f, 0.7f, 0.8f));
        y -= 15;
        AddButtonGrid(panelRoot.transform, handPoseNames, 4, ref y, (i) => ApplyHandPose(true, i));
        AddLabel(panelRoot.transform, "Right Hand:", 10, y, new Color(0.7f, 0.7f, 0.8f));
        y -= 15;
        AddButtonGrid(panelRoot.transform, handPoseNames, 4, ref y, (i) => ApplyHandPose(false, i));
        
        // === COSTUME ===
        AddSectionHeader(panelRoot.transform, "👗 Costume", ref y);
        AddToggleRow(panelRoot.transform, new string[]{"Sweater","Dress","Skirt","Tights","Boots"}, ref y,
            new string[]{"Sweater","Dress","Skirt","Tights","Boots"}, new bool[]{true,true,true,true,true}, true);
        
        // === HAIR ===
        AddSectionHeader(panelRoot.transform, "💇 Hair", ref y);
        AddToggleRow(panelRoot.transform, new string[]{"Bangs","Half-up"}, ref y,
            new string[]{"Bangs","Half"}, new bool[]{true, true}, true);
        AddSlider(panelRoot.transform, "Length", ref y, (v) => SetAnimatorFloat("Length", v));
        AddButtonGrid(panelRoot.transform, new string[]{"Default","Braid","Side L","Side R","All"}, 5, ref y, (i) => SetAnimatorInt("Hair", i));
        
        // === BODY ===
        AddSectionHeader(panelRoot.transform, "✨ Body", ref y);
        AddToggleRow(panelRoot.transform, new string[]{"Ears","Tail","Big Hip"}, ref y,
            new string[]{"Ear","Tail","Hip"}, new bool[]{true, true, false}, new bool[]{true, true, false});
        AddSlider(panelRoot.transform, "Breast", ref y, (v) => SetAnimatorFloat("Breast", v));
        
        // === CAMERA ===
        AddSectionHeader(panelRoot.transform, "📷 Camera", ref y);
        AddSlider(panelRoot.transform, "Rotate", ref y, (v) => {
            characterRotation = (v - 0.5f) * 360f;
            if (shinanoCharacter != null)
                shinanoCharacter.transform.rotation = Quaternion.Euler(0, characterRotation, 0);
        });
    }
    
    void ApplyHandPose(bool isLeft, int poseIndex)
    {
        #if UNITY_EDITOR
        if (characterAnimator == null) return;
        
        var clips = isLeft ? leftHandClips : rightHandClips;
        if (clips == null || poseIndex >= clips.Length || clips[poseIndex] == null)
        {
            Debug.LogWarning($"[Shinano] Hand pose clip not found for index {poseIndex}");
            return;
        }
        
        // Sample the animation at time 0 (hand poses are usually single-frame)
        clips[poseIndex].SampleAnimation(shinanoCharacter, 0);
        Debug.Log($"[Shinano] Applied {(isLeft ? "Left" : "Right")} hand pose: {handPoseNames[poseIndex]}");
        #else
        Debug.Log("[Shinano] Hand poses only work in Editor mode");
        #endif
    }
    
    // === UI BUILDING METHODS ===
    
    void AddLabel(Transform parent, string text, int fontSize, float y, Color color)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(0, fontSize + 6);
        
        Text txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
    }
    
    void AddSectionHeader(Transform parent, string title, ref float y)
    {
        y -= 8;
        
        // Divider line
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);
        RectTransform divRect = divider.AddComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0, 1);
        divRect.anchorMax = new Vector2(1, 1);
        divRect.pivot = new Vector2(0.5f, 1);
        divRect.anchoredPosition = new Vector2(0, y);
        divRect.sizeDelta = new Vector2(-20, 1);
        divider.AddComponent<Image>().color = new Color(0.4f, 0.3f, 0.5f, 0.5f);
        
        y -= 6;
        AddLabel(parent, title, 13, y, sectionColor);
        y -= 22;
    }
    
    void AddButtonGrid(Transform parent, string[] labels, int cols, ref float y, System.Action<int> onClick)
    {
        float btnW = 75;
        float btnH = 26;
        float spacing = 4;
        float startX = 10;
        
        for (int i = 0; i < labels.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            
            float x = startX + col * (btnW + spacing);
            float yPos = y - row * (btnH + spacing);
            
            CreateButton(parent, labels[i], x, yPos, btnW, btnH, i, onClick);
        }
        
        int rows = (labels.Length + cols - 1) / cols;
        y -= rows * (btnH + spacing) + 8;
    }
    
    void CreateButton(Transform parent, string label, float x, float yPos, float w, float h, int idx, System.Action<int> onClick)
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
        img.color = new Color(0.25f, 0.25f, 0.3f);
        
        Button button = btn.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(() => onClick(idx));
        
        // Add text
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
        txt.fontSize = 10;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }
    
    void AddFSetButtons(Transform parent, ref float y)
    {
        string[] labels = { "Joy", "Calm", "Complex" };
        float btnW = 110;
        float spacing = 8;
        float startX = 20;
        
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            GameObject btn = new GameObject("FSet_" + i);
            btn.transform.SetParent(parent, false);
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(startX + i * (btnW + spacing), y);
            rect.sizeDelta = new Vector2(btnW, 28);
            
            Image img = btn.AddComponent<Image>();
            img.color = i == 0 ? new Color(0.4f, 0.5f, 0.6f) : new Color(0.25f, 0.25f, 0.3f);
            
            Button button = btn.AddComponent<Button>();
            button.targetGraphic = img;
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
        
        y -= 36;
    }
    
    Text[] AddGestureGrid(Transform parent, string[] labels, ref float y, System.Action<int> onClick)
    {
        Text[] textLabels = new Text[labels.Length];
        float btnW = 90;
        float btnH = 24;
        float spacing = 3;
        int cols = 4;
        float startX = 10;
        
        for (int i = 0; i < labels.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            
            float x = startX + col * (btnW + spacing);
            float yPos = y - row * (btnH + spacing);
            
            int idx = i;
            GameObject btn = new GameObject("Gesture_" + i);
            btn.transform.SetParent(parent, false);
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, yPos);
            rect.sizeDelta = new Vector2(btnW, btnH);
            
            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.28f);
            
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
            txt.text = labels[i];
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 9;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            
            textLabels[i] = txt;
        }
        
        int rows = (labels.Length + cols - 1) / cols;
        y -= rows * (btnH + spacing) + 6;
        
        return textLabels;
    }
    
    void SelectFSet(int setIndex)
    {
        currentFSet = setIndex;
        SetAnimatorInt("F_Set", setIndex);
        SetAnimatorInt("GestureLeft", 0);
        SetAnimatorInt("GestureRight", 0);
        
        // Update gesture labels
        string[] newLabels = GetCurrentGestureSet();
        if (leftGestureLabels != null)
        {
            for (int i = 0; i < leftGestureLabels.Length && i < newLabels.Length; i++)
                leftGestureLabels[i].text = newLabels[i];
        }
        if (rightGestureLabels != null)
        {
            for (int i = 0; i < rightGestureLabels.Length && i < newLabels.Length; i++)
                rightGestureLabels[i].text = newLabels[i];
        }
        
        // Update F_Set button colors
        for (int i = 0; i < 3; i++)
        {
            var btn = panelRoot.transform.Find("FSet_" + i);
            if (btn != null)
            {
                var img = btn.GetComponent<Image>();
                img.color = i == setIndex ? new Color(0.4f, 0.5f, 0.6f) : new Color(0.25f, 0.25f, 0.3f);
            }
        }
    }
    
    void AddToggleRow(Transform parent, string[] labels, ref float y, string[] paramNames, bool[] defaults, bool invertLogic)
    {
        bool[] inverts = new bool[labels.Length];
        for (int i = 0; i < labels.Length; i++)
            inverts[i] = invertLogic;
        AddToggleRow(parent, labels, ref y, paramNames, defaults, inverts);
    }
    
    void AddToggleRow(Transform parent, string[] labels, ref float y, string[] paramNames, bool[] defaults, bool[] inverts)
    {
        float toggleW = 75;
        float spacing = 4;
        float startX = 10;
        
        for (int i = 0; i < labels.Length; i++)
        {
            float x = startX + i * (toggleW + spacing);
            bool isOn = defaults[i];
            
            GameObject tog = new GameObject("Toggle_" + labels[i]);
            tog.transform.SetParent(parent, false);
            
            RectTransform rect = tog.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(toggleW, 28);
            
            Image bg = tog.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f);
            
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
            lbl.fontSize = 9;
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
            indRt.sizeDelta = new Vector2(12, 12);
            
            Image indImg = ind.AddComponent<Image>();
            indImg.color = isOn ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
            
            Toggle toggle = tog.AddComponent<Toggle>();
            toggle.isOn = isOn;
            toggle.graphic = indImg;
            
            string param = paramNames[i];
            bool invert = inverts[i];
            
            toggle.onValueChanged.AddListener((val) => {
                indImg.color = val ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
                SetAnimatorBool(param, invert ? !val : val);
            });
        }
        
        y -= 36;
    }
    
    void AddSlider(Transform parent, string label, ref float y, System.Action<float> onChange)
    {
        // Label
        GameObject lblObj = new GameObject("SliderLabel");
        lblObj.transform.SetParent(parent, false);
        RectTransform lblRt = lblObj.AddComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0, 1);
        lblRt.anchorMax = new Vector2(0, 1);
        lblRt.pivot = new Vector2(0, 1);
        lblRt.anchoredPosition = new Vector2(10, y);
        lblRt.sizeDelta = new Vector2(60, 20);
        
        Text lbl = lblObj.AddComponent<Text>();
        lbl.text = label;
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize = 10;
        lbl.alignment = TextAnchor.MiddleLeft;
        lbl.color = textColor;
        
        // Slider background
        GameObject sliderBg = new GameObject("Slider");
        sliderBg.transform.SetParent(parent, false);
        RectTransform bgRt = sliderBg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 1);
        bgRt.anchorMax = new Vector2(1, 1);
        bgRt.pivot = new Vector2(0, 1);
        bgRt.anchoredPosition = new Vector2(70, y - 2);
        bgRt.sizeDelta = new Vector2(-90, 16);
        
        Image bgImg = sliderBg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.2f);
        
        // Fill area
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderBg.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0.5f, 1);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = sectionColor;
        
        // Handle area
        GameObject handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sliderBg.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = Vector2.zero;
        handleAreaRt.offsetMax = Vector2.zero;
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(10, 0);
        
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        
        Slider slider = sliderBg.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.value = 0.5f;
        slider.onValueChanged.AddListener((v) => onChange(v));
        
        y -= 26;
    }
    
    // === ANIMATOR METHODS ===
    
    void SetAnimatorInt(string param, int val)
    {
        if (characterAnimator == null)
        {
            Debug.LogError($"[Shinano] No animator found! Cannot set {param}={val}");
            return;
        }
        characterAnimator.SetInteger(param, val);
        Debug.Log($"[Shinano] SetInt {param}={val}");
    }
    
    void SetAnimatorBool(string param, bool val)
    {
        if (characterAnimator == null)
        {
            Debug.LogError($"[Shinano] No animator found! Cannot set {param}={val}");
            return;
        }
        characterAnimator.SetBool(param, val);
        Debug.Log($"[Shinano] SetBool {param}={val}");
    }
    
    void SetAnimatorFloat(string param, float val)
    {
        if (characterAnimator == null)
        {
            Debug.LogError($"[Shinano] No animator found! Cannot set {param}={val}");
            return;
        }
        characterAnimator.SetFloat(param, val);
        Debug.Log($"[Shinano] SetFloat {param}={val}");
    }
}
