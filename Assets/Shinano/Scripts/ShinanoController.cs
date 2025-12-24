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
    private GameObject contentRoot;
    private ScrollRect scrollRect;
    
    // State tracking
    private float characterRotation = 0f;
    
    // Expression names for F_Parts (0-8)
    private string[] expressionNames = {
        "Default", "Cheek", "Heart", "Dead", 
        "Guruguru", "Kirakira", "White", "Tear", "Sweat"
    };
    
    // Gesture names (0-7)
    private string[] gestureNames = {
        "Neutral", "Fist", "Open", "Point",
        "Victory", "Rock", "Gun", "Thumbs"
    };
    
    // Hair style names
    private string[] hairStyleNames = {
        "Default", "Style 1", "Style 2", "Style 3",
        "Style 4", "Style 5", "Style 6", "Style 7",
        "Style 8", "Style 9", "Style 10"
    };
    
    // Facial set names
    private string[] facialSetNames = {
        "Set 1", "Set 2", "Set 3", "Other"
    };
    
    // Colors
    private Color panelBg = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private Color sectionBg = new Color(0.12f, 0.12f, 0.18f, 0.9f);
    private Color accentColor = new Color(0.6f, 0.4f, 0.8f);
    private Color textColor = new Color(0.9f, 0.9f, 0.95f);
    private Color toggleOnColor = new Color(0.4f, 0.7f, 0.4f);
    private Color toggleOffColor = new Color(0.5f, 0.3f, 0.3f);
    
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
            {
                mainCamera = FindObjectOfType<Camera>();
            }
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
        
        // Create main scrollable panel
        panelRoot = CreateScrollablePanel(canvasObj.transform);
        
        float yPos = 0;
        
        // Title
        CreateTitle(contentRoot.transform, ref yPos);
        
        // === EXPRESSIONS SECTION ===
        CreateCollapsibleSection(contentRoot.transform, "🎭 Expressions", ref yPos, (container) => {
            CreateExpressionButtons(container);
        }, 140);
        
        // === FACIAL SET SECTION ===
        CreateCollapsibleSection(contentRoot.transform, "😊 Facial Sets", ref yPos, (container) => {
            CreateButtonRow(container, facialSetNames, (index) => {
                SetAnimatorInt("F_Set", index);
            }, new Color(0.5f, 0.4f, 0.6f));
        }, 50);
        
        // === GESTURES SECTION ===
        CreateCollapsibleSection(contentRoot.transform, "✋ Gestures", ref yPos, (container) => {
            CreateLabel(container, "Left Hand", 12, new Vector2(10, -5), new Vector2(100, 20), TextAnchor.MiddleLeft, textColor);
            CreateButtonRow(container, gestureNames, (index) => {
                SetAnimatorInt("GestureLeft", index);
            }, new Color(0.4f, 0.5f, 0.6f), 25);
            
            CreateLabel(container, "Right Hand", 12, new Vector2(10, -55), new Vector2(100, 20), TextAnchor.MiddleLeft, textColor);
            CreateButtonRow(container, gestureNames, (index) => {
                SetAnimatorInt("GestureRight", index);
            }, new Color(0.4f, 0.5f, 0.6f), 75);
        }, 120);
        
        // === COSTUME SECTION ===
        CreateCollapsibleSection(contentRoot.transform, "👗 Costume", ref yPos, (container) => {
            float toggleY = -10;
            CreateCompactToggle(container, "Sweater", true, ref toggleY, (val) => SetAnimatorBool("Sweater", !val));
            CreateCompactToggle(container, "Dress", true, ref toggleY, (val) => SetAnimatorBool("Dress", !val));
            CreateCompactToggle(container, "Skirt", true, ref toggleY, (val) => SetAnimatorBool("Skirt", !val));
            CreateCompactToggle(container, "Tights", true, ref toggleY, (val) => SetAnimatorBool("Tights", !val));
            CreateCompactToggle(container, "Boots", true, ref toggleY, (val) => SetAnimatorBool("Boots", !val));
            CreateCompactToggle(container, "Bra", true, ref toggleY, (val) => ToggleMeshByName("Cloth_under_bra", val));
            CreateCompactToggle(container, "Shorts", true, ref toggleY, (val) => ToggleMeshByName("Cloth_under_shorts", val));
        }, 200);
        
        // === HAIR SECTION ===
        CreateCollapsibleSection(contentRoot.transform, "💇 Hair", ref yPos, (container) => {
            float toggleY = -10;
            CreateCompactToggle(container, "Bangs", true, ref toggleY, (val) => SetAnimatorBool("Bangs", !val));
            CreateCompactToggle(container, "Half-up", true, ref toggleY, (val) => SetAnimatorBool("Half", !val));
            
            CreateLabel(container, "Hair Length", 12, new Vector2(10, toggleY - 5), new Vector2(100, 20), TextAnchor.MiddleLeft, textColor);
            CreateCompactSlider(container, 0.5f, toggleY - 20, (val) => SetAnimatorFloat("Length", val));
            
            CreateLabel(container, "Hair Style", 12, new Vector2(10, toggleY - 50), new Vector2(100, 20), TextAnchor.MiddleLeft, textColor);
            CreateButtonRow(container, new string[]{"1","2","3","4","5","6","7","8","9","10"}, (index) => {
                SetAnimatorInt("Hair", index);
            }, new Color(0.6f, 0.4f, 0.5f), (int)(-toggleY + 70));
        }, 150);
        
        // === BODY SECTION ===
        CreateCollapsibleSection(contentRoot.transform, "✨ Body", ref yPos, (container) => {
            float toggleY = -10;
            CreateCompactToggle(container, "Ears", true, ref toggleY, (val) => SetAnimatorBool("Ear", !val));
            CreateCompactToggle(container, "Tail", true, ref toggleY, (val) => SetAnimatorBool("Tail", !val));
            CreateCompactToggle(container, "Backlit", false, ref toggleY, (val) => SetAnimatorBool("Backlit", val));
            
            CreateLabel(container, "Breast Size", 12, new Vector2(10, toggleY - 5), new Vector2(100, 20), TextAnchor.MiddleLeft, textColor);
            CreateCompactSlider(container, 0.5f, toggleY - 20, (val) => SetAnimatorFloat("Breast", val));
            
            CreateLabel(container, "Hip Size", 12, new Vector2(10, toggleY - 50), new Vector2(100, 20), TextAnchor.MiddleLeft, textColor);
            CreateCompactSlider(container, 0.5f, toggleY - 65, (val) => SetAnimatorFloat("Hip", val));
        }, 175);
        
        // === AFK SECTION ===
        CreateCollapsibleSection(contentRoot.transform, "🛋️ AFK", ref yPos, (container) => {
            float toggleY = -10;
            CreateCompactToggle(container, "AFK Mode", false, ref toggleY, (val) => SetAnimatorBool("AFK", val));
        }, 45);
        
        // === CAMERA SECTION ===
        CreateCollapsibleSection(contentRoot.transform, "📷 Camera", ref yPos, (container) => {
            CreateLabel(container, "Rotate Character", 12, new Vector2(10, -10), new Vector2(150, 20), TextAnchor.MiddleLeft, textColor);
            CreateCompactSlider(container, 0.5f, -25, (val) => {
                characterRotation = (val - 0.5f) * 360f;
                if (shinanoCharacter != null)
                    shinanoCharacter.transform.rotation = Quaternion.Euler(0, characterRotation, 0);
            });
            
            CreateLabel(container, "Camera Distance", 12, new Vector2(10, -55), new Vector2(150, 20), TextAnchor.MiddleLeft, textColor);
            CreateCompactSlider(container, 0.5f, -70, (val) => {
                if (mainCamera != null)
                {
                    float zDistance = Mathf.Lerp(0.5f, 5f, val);
                    Vector3 pos = mainCamera.transform.position;
                    pos.z = zDistance;
                    mainCamera.transform.position = pos;
                }
            });
        }, 100);
        
        // Set content size
        RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Abs(yPos) + 50);
    }
    
    GameObject CreateScrollablePanel(Transform parent)
    {
        // Main panel container
        GameObject panel = new GameObject("MainPanel");
        panel.transform.SetParent(parent, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 0.5f);
        panelRect.sizeDelta = new Vector2(340, 0);
        panelRect.anchoredPosition = new Vector2(10, 0);
        panelRect.offsetMin = new Vector2(10, 20);
        panelRect.offsetMax = new Vector2(350, -20);
        
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = panelBg;
        
        // Add mask for scrolling
        panel.AddComponent<Mask>().showMaskGraphic = true;
        
        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(panel.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.offsetMin = new Vector2(5, 5);
        viewportRect.offsetMax = new Vector2(-5, -5);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        
        // Content container
        contentRoot = new GameObject("Content");
        contentRoot.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = contentRoot.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 1500);
        contentRect.anchoredPosition = Vector2.zero;
        
        // ScrollRect component
        scrollRect = panel.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        
        return panel;
    }
    
    void CreateTitle(Transform parent, ref float yPos)
    {
        CreateLabel(parent, "Shinano Controller", 22, new Vector2(0, yPos - 15), 
            new Vector2(300, 35), TextAnchor.MiddleCenter, accentColor);
        CreateLabel(parent, "Press TAB to toggle", 11, new Vector2(0, yPos - 40), 
            new Vector2(300, 20), TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f));
        yPos -= 60;
    }
    
    void CreateCollapsibleSection(Transform parent, string title, ref float yPos, 
        System.Action<Transform> contentBuilder, float contentHeight)
    {
        // Section container
        GameObject section = new GameObject("Section_" + title);
        section.transform.SetParent(parent, false);
        
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = new Vector2(0, 1);
        sectionRect.anchorMax = new Vector2(1, 1);
        sectionRect.pivot = new Vector2(0.5f, 1);
        sectionRect.anchoredPosition = new Vector2(0, yPos);
        sectionRect.sizeDelta = new Vector2(-20, contentHeight + 35);
        
        Image sectionImg = section.AddComponent<Image>();
        sectionImg.color = sectionBg;
        
        // Section header
        CreateLabel(section.transform, title, 14, new Vector2(10, -5), 
            new Vector2(280, 25), TextAnchor.MiddleLeft, accentColor);
        
        // Divider
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(section.transform, false);
        RectTransform divRect = divider.AddComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0, 1);
        divRect.anchorMax = new Vector2(1, 1);
        divRect.pivot = new Vector2(0.5f, 1);
        divRect.anchoredPosition = new Vector2(0, -28);
        divRect.sizeDelta = new Vector2(-20, 1);
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
        
        // Content area
        GameObject content = new GameObject("Content");
        content.transform.SetParent(section.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = new Vector2(0, -30);
        contentRect.sizeDelta = new Vector2(0, -35);
        contentRect.offsetMin = new Vector2(5, 5);
        contentRect.offsetMax = new Vector2(-5, -35);
        
        // Build content
        contentBuilder?.Invoke(content.transform);
        
        yPos -= contentHeight + 45;
    }
    
    void CreateExpressionButtons(Transform parent)
    {
        float btnWidth = 60;
        float btnHeight = 28;
        float spacing = 4;
        int cols = 5;
        
        for (int i = 0; i < expressionNames.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            float x = 5 + col * (btnWidth + spacing);
            float y = -5 - row * (btnHeight + spacing);
            
            int index = i;
            Color btnColor = GetExpressionColor(i);
            CreateSmallButton(parent, expressionNames[i], new Vector2(x, y), 
                new Vector2(btnWidth, btnHeight), () => SetAnimatorInt("F_Parts", index), btnColor);
        }
    }
    
    void CreateButtonRow(Transform parent, string[] labels, System.Action<int> onClick, 
        Color baseColor, float yOffset = 0)
    {
        float btnWidth = Mathf.Min(60, (300 - 20) / labels.Length - 4);
        float btnHeight = 24;
        float spacing = 3;
        
        for (int i = 0; i < labels.Length; i++)
        {
            float x = 5 + i * (btnWidth + spacing);
            float y = -5 - yOffset;
            
            int index = i;
            Color btnColor = new Color(baseColor.r + (i * 0.02f), baseColor.g, baseColor.b - (i * 0.01f));
            CreateSmallButton(parent, labels[i], new Vector2(x, y), 
                new Vector2(btnWidth, btnHeight), () => onClick(index), btnColor);
        }
    }
    
    void CreateSmallButton(Transform parent, string text, Vector2 pos, Vector2 size, 
        System.Action onClick, Color bgColor)
    {
        GameObject btnObj = new GameObject("Btn_" + text);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.3f;
        colors.pressedColor = bgColor * 0.7f;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());
        
        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = text;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 10;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
    }
    
    void CreateCompactToggle(Transform parent, string label, bool defaultValue, 
        ref float yPos, System.Action<bool> onValueChanged)
    {
        GameObject toggleObj = new GameObject("Toggle_" + label);
        toggleObj.transform.SetParent(parent, false);
        
        RectTransform rect = toggleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, yPos);
        rect.sizeDelta = new Vector2(-10, 24);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.7f, 1);
        labelRect.offsetMin = new Vector2(5, 0);
        labelRect.offsetMax = Vector2.zero;
        
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 12;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = textColor;
        
        // Toggle switch background
        GameObject switchBg = new GameObject("SwitchBg");
        switchBg.transform.SetParent(toggleObj.transform, false);
        RectTransform switchRect = switchBg.AddComponent<RectTransform>();
        switchRect.anchorMin = new Vector2(1, 0.5f);
        switchRect.anchorMax = new Vector2(1, 0.5f);
        switchRect.pivot = new Vector2(1, 0.5f);
        switchRect.anchoredPosition = new Vector2(-5, 0);
        switchRect.sizeDelta = new Vector2(40, 18);
        
        Image switchImg = switchBg.AddComponent<Image>();
        switchImg.color = defaultValue ? toggleOnColor : toggleOffColor;
        
        // Toggle knob
        GameObject knob = new GameObject("Knob");
        knob.transform.SetParent(switchBg.transform, false);
        RectTransform knobRect = knob.AddComponent<RectTransform>();
        knobRect.anchorMin = new Vector2(defaultValue ? 0.5f : 0, 0);
        knobRect.anchorMax = new Vector2(defaultValue ? 1f : 0.5f, 1);
        knobRect.offsetMin = new Vector2(2, 2);
        knobRect.offsetMax = new Vector2(-2, -2);
        
        Image knobImg = knob.AddComponent<Image>();
        knobImg.color = Color.white;
        
        // Toggle component
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = defaultValue;
        toggle.graphic = knobImg;
        toggle.onValueChanged.AddListener((val) => {
            switchImg.color = val ? toggleOnColor : toggleOffColor;
            knobRect.anchorMin = new Vector2(val ? 0.5f : 0, 0);
            knobRect.anchorMax = new Vector2(val ? 1f : 0.5f, 1);
            onValueChanged?.Invoke(val);
        });
        
        yPos -= 28;
    }
    
    void CreateCompactSlider(Transform parent, float defaultValue, float yPos,
        System.Action<float> onValueChanged)
    {
        // Slider background
        GameObject sliderBg = new GameObject("SliderBg");
        sliderBg.transform.SetParent(parent, false);
        RectTransform sliderBgRect = sliderBg.AddComponent<RectTransform>();
        sliderBgRect.anchorMin = new Vector2(0, 1);
        sliderBgRect.anchorMax = new Vector2(1, 1);
        sliderBgRect.pivot = new Vector2(0.5f, 1);
        sliderBgRect.anchoredPosition = new Vector2(0, yPos);
        sliderBgRect.sizeDelta = new Vector2(-20, 14);
        
        Image sliderBgImg = sliderBg.AddComponent<Image>();
        sliderBgImg.color = new Color(0.25f, 0.25f, 0.3f);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(sliderBg.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(defaultValue, 1);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = accentColor;
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderBg.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(defaultValue, 0);
        handleRect.anchorMax = new Vector2(defaultValue, 1);
        handleRect.sizeDelta = new Vector2(8, 0);
        
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        
        // Slider component
        Slider slider = sliderBg.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = defaultValue;
        slider.onValueChanged.AddListener((val) => {
            fillRect.anchorMax = new Vector2(val, 1);
            onValueChanged?.Invoke(val);
        });
    }
    
    void CreateLabel(Transform parent, string text, int fontSize, Vector2 pos, Vector2 size, 
        TextAnchor alignment, Color color)
    {
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.SetParent(parent, false);
        
        RectTransform rect = labelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        
        Text label = labelObj.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
    }
    
    Color GetExpressionColor(int index)
    {
        Color[] colors = {
            new Color(0.4f, 0.4f, 0.5f),   // Default
            new Color(0.9f, 0.5f, 0.5f),   // Cheek
            new Color(0.9f, 0.4f, 0.5f),   // Heart
            new Color(0.3f, 0.3f, 0.4f),   // Dead
            new Color(0.6f, 0.5f, 0.8f),   // Guruguru
            new Color(0.9f, 0.8f, 0.3f),   // Kirakira
            new Color(0.8f, 0.8f, 0.8f),   // White
            new Color(0.4f, 0.6f, 0.9f),   // Tear
            new Color(0.5f, 0.7f, 0.9f),   // Sweat
        };
        return index < colors.Length ? colors[index] : Color.gray;
    }
    
    // === Animator Helper Methods ===
    
    void SetAnimatorInt(string paramName, int value)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetInteger(paramName, value); }
            catch { Debug.LogWarning($"Parameter '{paramName}' not found"); }
        }
    }
    
    void SetAnimatorBool(string paramName, bool value)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetBool(paramName, value); }
            catch { Debug.LogWarning($"Parameter '{paramName}' not found"); }
        }
    }
    
    void SetAnimatorFloat(string paramName, float value)
    {
        if (characterAnimator != null)
        {
            try { characterAnimator.SetFloat(paramName, value); }
            catch { Debug.LogWarning($"Parameter '{paramName}' not found"); }
        }
    }
    
    void ToggleMeshByName(string meshName, bool visible)
    {
        if (shinanoCharacter == null) return;
        
        Transform[] allChildren = shinanoCharacter.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == meshName)
            {
                SkinnedMeshRenderer skinnedMesh = child.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMesh != null)
                {
                    skinnedMesh.enabled = visible;
                    return;
                }
                
                MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = visible;
                    return;
                }
                
                child.gameObject.SetActive(visible);
                return;
            }
        }
    }
}
