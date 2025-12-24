using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Shinano Character Expression & Customization Controller
/// Complete UI panel for controlling all avatar features
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
    
    // State tracking
    private float characterRotation = 0f;
    
    // Names arrays
    private string[] expressionNames = { "Default", "Cheek", "Heart", "Dead", "Guru", "Kira", "White", "Tear", "Sweat" };
    private string[] gestureNames = { "Idle", "Fist", "Open", "Point", "Peace", "Rock", "Gun", "Thumb" };
    
    // Colors
    private Color panelBg = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    private Color sectionColor = new Color(0.7f, 0.5f, 0.8f);
    private Color textColor = new Color(0.9f, 0.9f, 0.95f);
    
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
        
        // Create main panel - left side of screen
        panelRoot = new GameObject("MainPanel");
        panelRoot.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 0.5f);
        panelRect.anchoredPosition = new Vector2(10, 0);
        panelRect.sizeDelta = new Vector2(320, -40);
        
        Image panelImg = panelRoot.AddComponent<Image>();
        panelImg.color = panelBg;
        
        float y = -10;
        
        // Title
        AddLabel(panelRoot.transform, "✨ Shinano Controller", 18, y, sectionColor);
        y -= 25;
        AddLabel(panelRoot.transform, "Press TAB to toggle", 10, y, new Color(0.6f, 0.6f, 0.6f));
        y -= 30;
        
        // === EXPRESSIONS ===
        AddSectionHeader(panelRoot.transform, "🎭 Expressions", ref y);
        AddButtonGrid(panelRoot.transform, expressionNames, 3, ref y, (i) => SetAnimatorInt("F_Parts", i));
        
        // === FACIAL SET ===
        AddSectionHeader(panelRoot.transform, "😊 Facial Set", ref y);
        AddButtonGrid(panelRoot.transform, new string[]{"Set1","Set2","Set3","Other"}, 4, ref y, (i) => SetAnimatorInt("F_Set", i));
        
        // === GESTURES ===
        AddSectionHeader(panelRoot.transform, "✋ Gestures", ref y);
        AddLabel(panelRoot.transform, "Left:", 11, y, textColor);
        y -= 5;
        AddButtonGrid(panelRoot.transform, gestureNames, 4, ref y, (i) => SetAnimatorInt("GestureLeft", i));
        AddLabel(panelRoot.transform, "Right:", 11, y, textColor);
        y -= 5;
        AddButtonGrid(panelRoot.transform, gestureNames, 4, ref y, (i) => SetAnimatorInt("GestureRight", i));
        
        // === COSTUME ===
        AddSectionHeader(panelRoot.transform, "👗 Costume", ref y);
        AddToggleRow(panelRoot.transform, new string[]{"Sweater","Dress","Skirt","Tights"}, ref y, 
            new string[]{"Sweater","Dress","Skirt","Tights"}, true);
        AddToggleRow(panelRoot.transform, new string[]{"Boots","Bra","Shorts"}, ref y, 
            new string[]{"Boots","Cloth_under_bra","Cloth_under_shorts"}, true);
        
        // === HAIR ===
        AddSectionHeader(panelRoot.transform, "💇 Hair", ref y);
        AddToggleRow(panelRoot.transform, new string[]{"Bangs","Half-up"}, ref y, 
            new string[]{"Bangs","Half"}, true);
        AddSlider(panelRoot.transform, "Length", ref y, (v) => SetAnimatorFloat("Length", v));
        AddButtonGrid(panelRoot.transform, new string[]{"1","2","3","4","5","6","7","8","9","10"}, 5, ref y, (i) => SetAnimatorInt("Hair", i));
        
        // === BODY ===
        AddSectionHeader(panelRoot.transform, "✨ Body", ref y);
        AddToggleRow(panelRoot.transform, new string[]{"Ears","Tail","Backlit","AFK"}, ref y,
            new string[]{"Ear","Tail","Backlit","AFK"}, false);
        AddToggleRow(panelRoot.transform, new string[]{"Big Hip"}, ref y,
            new string[]{"Hip"}, false);
        AddSlider(panelRoot.transform, "Breast", ref y, (v) => SetAnimatorFloat("Breast", v));
        
        // === CAMERA ===
        AddSectionHeader(panelRoot.transform, "📷 Camera", ref y);
        AddSlider(panelRoot.transform, "Rotate", ref y, (v) => {
            characterRotation = (v - 0.5f) * 360f;
            if (shinanoCharacter != null)
                shinanoCharacter.transform.rotation = Quaternion.Euler(0, characterRotation, 0);
        });
        AddSlider(panelRoot.transform, "Distance", ref y, (v) => {
            if (mainCamera != null)
            {
                Vector3 pos = mainCamera.transform.position;
                pos.z = Mathf.Lerp(0.5f, 5f, v);
                mainCamera.transform.position = pos;
            }
        });
    }
    
    void AddLabel(Transform parent, string text, int size, float y, Color color)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(0, 20);
        
        Text txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = size;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
    }
    
    void AddSectionHeader(Transform parent, string text, ref float y)
    {
        y -= 5;
        
        // Divider line
        GameObject div = new GameObject("Divider");
        div.transform.SetParent(parent, false);
        RectTransform divRect = div.AddComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0, 1);
        divRect.anchorMax = new Vector2(1, 1);
        divRect.pivot = new Vector2(0.5f, 1);
        divRect.anchoredPosition = new Vector2(0, y);
        divRect.sizeDelta = new Vector2(-20, 1);
        div.AddComponent<Image>().color = new Color(0.5f, 0.4f, 0.6f, 0.5f);
        
        y -= 8;
        AddLabel(parent, text, 13, y, sectionColor);
        y -= 20;
    }
    
    void AddButtonGrid(Transform parent, string[] labels, int cols, ref float y, System.Action<int> onClick)
    {
        float btnW = (300f - 10) / cols - 4;
        float btnH = 24;
        
        for (int i = 0; i < labels.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            
            GameObject btn = new GameObject("Btn_" + labels[i]);
            btn.transform.SetParent(parent, false);
            
            RectTransform rect = btn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10 + col * (btnW + 4), y - row * (btnH + 3));
            rect.sizeDelta = new Vector2(btnW, btnH);
            
            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.3f + (i * 0.02f), 0.3f, 0.4f);
            
            Button button = btn.AddComponent<Button>();
            button.targetGraphic = img;
            int idx = i;
            button.onClick.AddListener(() => onClick(idx));
            
            // Text
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btn.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            
            Text txt = txtObj.AddComponent<Text>();
            txt.text = labels[i];
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 11;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
        
        int rows = (labels.Length + cols - 1) / cols;
        y -= rows * (btnH + 3) + 5;
    }
    
    void AddToggleRow(Transform parent, string[] labels, ref float y, string[] paramNames, bool invertLogic)
    {
        float toggleW = (300f - 10) / labels.Length - 4;
        
        for (int i = 0; i < labels.Length; i++)
        {
            GameObject tog = new GameObject("Toggle_" + labels[i]);
            tog.transform.SetParent(parent, false);
            
            RectTransform rect = tog.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10 + i * (toggleW + 4), y);
            rect.sizeDelta = new Vector2(toggleW, 28);
            
            Image bg = tog.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f);
            
            // Label
            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(tog.transform, false);
            RectTransform lblRect = lblObj.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = new Vector2(0.7f, 1);
            lblRect.offsetMin = new Vector2(3, 0);
            lblRect.offsetMax = Vector2.zero;
            
            Text lbl = lblObj.AddComponent<Text>();
            lbl.text = labels[i];
            lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbl.fontSize = 10;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = textColor;
            
            // Toggle indicator
            GameObject ind = new GameObject("Indicator");
            ind.transform.SetParent(tog.transform, false);
            RectTransform indRect = ind.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(1, 0.5f);
            indRect.anchorMax = new Vector2(1, 0.5f);
            indRect.pivot = new Vector2(1, 0.5f);
            indRect.anchoredPosition = new Vector2(-3, 0);
            indRect.sizeDelta = new Vector2(12, 12);
            
            Image indImg = ind.AddComponent<Image>();
            indImg.color = new Color(0.4f, 0.7f, 0.4f);
            
            Toggle toggle = tog.AddComponent<Toggle>();
            toggle.isOn = true;
            toggle.graphic = indImg;
            
            string param = paramNames[i];
            bool isMesh = param.StartsWith("Cloth_");
            bool invert = invertLogic;
            
            toggle.onValueChanged.AddListener((val) => {
                indImg.color = val ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
                if (isMesh)
                    ToggleMesh(param, val);
                else
                    SetAnimatorBool(param, invert ? !val : val);
            });
        }
        
        y -= 33;
    }
    
    void AddSlider(Transform parent, string label, ref float y, System.Action<float> onChange)
    {
        // Label
        GameObject lblObj = new GameObject("Label_" + label);
        lblObj.transform.SetParent(parent, false);
        RectTransform lblRect = lblObj.AddComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 1);
        lblRect.anchorMax = new Vector2(0, 1);
        lblRect.pivot = new Vector2(0, 1);
        lblRect.anchoredPosition = new Vector2(10, y);
        lblRect.sizeDelta = new Vector2(60, 18);
        
        Text lbl = lblObj.AddComponent<Text>();
        lbl.text = label;
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize = 11;
        lbl.alignment = TextAnchor.MiddleLeft;
        lbl.color = textColor;
        
        // Slider bg
        GameObject sliderBg = new GameObject("Slider_" + label);
        sliderBg.transform.SetParent(parent, false);
        RectTransform bgRect = sliderBg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.pivot = new Vector2(0.5f, 1);
        bgRect.anchoredPosition = new Vector2(30, y);
        bgRect.sizeDelta = new Vector2(-80, 16);
        
        Image bgImg = sliderBg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(sliderBg.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.5f, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = sectionColor;
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderBg.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0);
        handleRect.anchorMax = new Vector2(0.5f, 1);
        handleRect.sizeDelta = new Vector2(8, 0);
        
        handle.AddComponent<Image>().color = Color.white;
        
        Slider slider = sliderBg.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.value = 0.5f;
        slider.onValueChanged.AddListener((v) => {
            fillRect.anchorMax = new Vector2(v, 1);
            onChange(v);
        });
        
        y -= 22;
    }
    
    // === Helper Methods ===
    
    void SetAnimatorInt(string param, int val)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetInteger(param, val); }
            catch { Debug.LogWarning($"Param '{param}' not found"); }
        }
    }
    
    void SetAnimatorBool(string param, bool val)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetBool(param, val); }
            catch { Debug.LogWarning($"Param '{param}' not found"); }
        }
    }
    
    void SetAnimatorFloat(string param, float val)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetFloat(param, val); }
            catch { Debug.LogWarning($"Param '{param}' not found"); }
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
