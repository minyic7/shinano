using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Shinano Character Expression & Customization Controller
/// Provides a beautiful UI panel to control facial expressions, costumes, and body features
/// </summary>
public class ShinanoController : MonoBehaviour
{
    [Header("Character Reference")]
    [Tooltip("The Shinano character root GameObject")]
    public GameObject shinanoCharacter;
    
    [Tooltip("The Animator component on the character")]
    public Animator characterAnimator;
    
    [Header("Camera Reference")]
    [Tooltip("Main camera for zoom control")]
    public Camera mainCamera;
    
    [Header("UI Panel Settings")]
    public KeyCode togglePanelKey = KeyCode.Tab;
    public bool panelVisible = true;
    
    // UI References (auto-generated)
    private Canvas uiCanvas;
    private GameObject panelRoot;
    
    // Expression states
    private int currentExpression = 0;
    private string[] expressionNames = {
        "Default", "Cheek", "Heart Eye", "Dead Eye", 
        "Guruguru Eye", "Kirakira Eye", "White Eye", "Tear", "Sweat"
    };
    
    // Costume states
    private bool sweaterOn = true;
    private bool dressOn = true;
    private bool skirtOn = true;
    private bool tightsOn = true;
    private bool bootsOn = true;
    
    // Body features
    private bool earOn = true;
    private bool tailOn = true;
    private float breastSize = 0.5f;
    private float hairLength = 0.5f;
    
    // Camera & rotation
    private float characterRotation = 0f;
    private float cameraDistance = 2f;
    private Vector3 initialCameraPosition;
    
    void Start()
    {
        // Auto-find character if not assigned
        if (shinanoCharacter == null)
        {
            // Try to find Shinano or Shinano_kisekae
            shinanoCharacter = GameObject.Find("Shinano_kisekae");
            if (shinanoCharacter == null)
                shinanoCharacter = GameObject.Find("Shinano");
        }
        
        if (shinanoCharacter != null && characterAnimator == null)
        {
            characterAnimator = shinanoCharacter.GetComponent<Animator>();
            
            // Check if animator has a controller
            if (characterAnimator != null && characterAnimator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("ShinanoController: Animator found but no AnimatorController assigned. " +
                    "Please assign 'Shinano_FX' controller to the character's Animator component.");
            }
        }
        
        if (characterAnimator == null)
        {
            Debug.LogWarning("ShinanoController: No Animator found on Shinano character. " +
                "Drag the Shinano_kisekae prefab into the scene for full functionality.");
        }
        
        // Auto-find camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera != null)
        {
            initialCameraPosition = mainCamera.transform.position;
            cameraDistance = Vector3.Distance(mainCamera.transform.position,
                shinanoCharacter != null ? shinanoCharacter.transform.position : Vector3.zero);
        }
        
        CreateUI();
    }
    
    void Update()
    {
        // Toggle panel visibility with Tab key
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
        
        // Ensure EventSystem exists for UI interaction
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
        
        // Create main panel
        panelRoot = CreatePanel(canvasObj.transform, "MainPanel", 
            new Vector2(320, 600), new Vector2(170, 0));
        
        // Title
        CreateLabel(panelRoot.transform, "Shinano Controller", 24, 
            new Vector2(0, -20), new Vector2(300, 40), TextAnchor.MiddleCenter, 
            new Color(0.9f, 0.7f, 0.9f));
        
        CreateLabel(panelRoot.transform, "Press TAB to toggle", 12, 
            new Vector2(0, -45), new Vector2(300, 20), TextAnchor.MiddleCenter,
            new Color(0.7f, 0.7f, 0.7f));
        
        float yPos = -80;
        
        // === EXPRESSIONS SECTION ===
        CreateSectionHeader(panelRoot.transform, "🎭 Expressions", ref yPos);
        
        // Expression buttons grid (3x3)
        float btnWidth = 90;
        float btnHeight = 35;
        float spacing = 5;
        float startX = -95;
        
        for (int i = 0; i < expressionNames.Length; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float x = startX + col * (btnWidth + spacing);
            float y = yPos - row * (btnHeight + spacing);
            
            int expressionIndex = i;
            CreateButton(panelRoot.transform, expressionNames[i], 
                new Vector2(x, y), new Vector2(btnWidth, btnHeight),
                () => SetExpression(expressionIndex),
                GetExpressionColor(i));
        }
        yPos -= 120;
        
        // === COSTUME SECTION ===
        yPos -= 20;
        CreateSectionHeader(panelRoot.transform, "👗 Costume", ref yPos);
        
        CreateToggle(panelRoot.transform, "Sweater", sweaterOn, ref yPos, (val) => {
            sweaterOn = val;
            SetAnimatorBool("Sweater", !val);
        });
        
        CreateToggle(panelRoot.transform, "Dress", dressOn, ref yPos, (val) => {
            dressOn = val;
            SetAnimatorBool("Dress", !val);
        });
        
        CreateToggle(panelRoot.transform, "Skirt", skirtOn, ref yPos, (val) => {
            skirtOn = val;
            SetAnimatorBool("Skirt", !val);
        });
        
        CreateToggle(panelRoot.transform, "Tights", tightsOn, ref yPos, (val) => {
            tightsOn = val;
            SetAnimatorBool("Tights", !val);
        });
        
        CreateToggle(panelRoot.transform, "Boots", bootsOn, ref yPos, (val) => {
            bootsOn = val;
            SetAnimatorBool("Boots", !val);
        });
        
        // === BODY SECTION ===
        yPos -= 20;
        CreateSectionHeader(panelRoot.transform, "✨ Body Features", ref yPos);
        
        CreateToggle(panelRoot.transform, "Ears", earOn, ref yPos, (val) => {
            earOn = val;
            SetAnimatorBool("Ear", !val);
        });
        
        CreateToggle(panelRoot.transform, "Tail", tailOn, ref yPos, (val) => {
            tailOn = val;
            SetAnimatorBool("Tail", !val);
        });
        
        CreateSlider(panelRoot.transform, "Breast Size", breastSize, ref yPos, (val) => {
            breastSize = val;
            SetAnimatorFloat("Breast", val);
        });
        
        CreateSlider(panelRoot.transform, "Hair Length", hairLength, ref yPos, (val) => {
            hairLength = val;
            SetAnimatorFloat("Length", val);
        });
        
        // === CAMERA SECTION ===
        yPos -= 20;
        CreateSectionHeader(panelRoot.transform, "📷 Camera & View", ref yPos);
        
        CreateSlider(panelRoot.transform, "Rotate Character", 0.5f, ref yPos, (val) => {
            characterRotation = (val - 0.5f) * 360f;
            if (shinanoCharacter != null)
            {
                shinanoCharacter.transform.rotation = Quaternion.Euler(0, characterRotation, 0);
            }
        });
        
        CreateSlider(panelRoot.transform, "Camera Distance", 0.5f, ref yPos, (val) => {
            if (mainCamera != null && shinanoCharacter != null)
            {
                // Map 0-1 to distance range (0.5 to 5 units)
                float distance = Mathf.Lerp(0.5f, 5f, val);
                Vector3 direction = (mainCamera.transform.position - shinanoCharacter.transform.position).normalized;
                mainCamera.transform.position = shinanoCharacter.transform.position + direction * distance;
            }
        });
        
        // Adjust panel height based on content
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(320, Mathf.Abs(yPos) + 40);
    }
    
    Color GetExpressionColor(int index)
    {
        Color[] colors = {
            new Color(0.4f, 0.4f, 0.5f),   // Default - gray
            new Color(0.9f, 0.6f, 0.6f),   // Cheek - pink
            new Color(0.9f, 0.4f, 0.5f),   // Heart - red-pink
            new Color(0.3f, 0.3f, 0.4f),   // Dead - dark
            new Color(0.6f, 0.5f, 0.8f),   // Guruguru - purple
            new Color(0.9f, 0.8f, 0.4f),   // Kirakira - gold
            new Color(0.9f, 0.9f, 0.9f),   // White - white
            new Color(0.4f, 0.6f, 0.9f),   // Tear - blue
            new Color(0.5f, 0.7f, 0.9f),   // Sweat - light blue
        };
        return index < colors.Length ? colors[index] : Color.gray;
    }
    
    void SetExpression(int expressionIndex)
    {
        currentExpression = expressionIndex;
        if (characterAnimator != null)
        {
            characterAnimator.SetInteger("F_Parts", expressionIndex);
        }
        Debug.Log($"Expression set to: {expressionNames[expressionIndex]}");
    }
    
    void SetAnimatorBool(string paramName, bool value)
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(paramName, value);
        }
    }
    
    void SetAnimatorFloat(string paramName, float value)
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat(paramName, value);
        }
    }
    
    // ==================== UI HELPER METHODS ====================
    
    GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // Add rounded corners effect with outline
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.4f, 0.6f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);
        
        return panel;
    }
    
    void CreateLabel(Transform parent, string text, int fontSize, Vector2 pos, Vector2 size, 
        TextAnchor alignment, Color color)
    {
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.SetParent(parent, false);
        
        RectTransform rect = labelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        
        Text label = labelObj.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
    }
    
    void CreateSectionHeader(Transform parent, string text, ref float yPos)
    {
        yPos -= 10;
        CreateLabel(parent, text, 16, new Vector2(0, yPos), new Vector2(280, 25), 
            TextAnchor.MiddleLeft, new Color(0.8f, 0.7f, 0.9f));
        
        // Divider line
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);
        RectTransform divRect = divider.AddComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.5f, 1);
        divRect.anchorMax = new Vector2(0.5f, 1);
        divRect.pivot = new Vector2(0.5f, 1);
        divRect.anchoredPosition = new Vector2(0, yPos - 20);
        divRect.sizeDelta = new Vector2(280, 2);
        
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.5f, 0.4f, 0.6f, 0.5f);
        
        yPos -= 35;
    }
    
    void CreateButton(Transform parent, string text, Vector2 pos, Vector2 size, 
        System.Action onClick, Color bgColor)
    {
        GameObject btnObj = new GameObject("Btn_" + text);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;
        
        btn.onClick.AddListener(() => onClick?.Invoke());
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = text;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 12;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
    }
    
    void CreateToggle(Transform parent, string label, bool defaultValue, ref float yPos, 
        System.Action<bool> onValueChanged)
    {
        GameObject toggleObj = new GameObject("Toggle_" + label);
        toggleObj.transform.SetParent(parent, false);
        
        RectTransform rect = toggleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, yPos);
        rect.sizeDelta = new Vector2(280, 30);
        
        // Background
        Image bgImg = toggleObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.7f, 1);
        labelRect.offsetMin = new Vector2(10, 0);
        labelRect.offsetMax = Vector2.zero;
        
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 14;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = Color.white;
        
        // Toggle checkbox background
        GameObject checkBg = new GameObject("CheckBg");
        checkBg.transform.SetParent(toggleObj.transform, false);
        RectTransform checkBgRect = checkBg.AddComponent<RectTransform>();
        checkBgRect.anchorMin = new Vector2(1, 0.5f);
        checkBgRect.anchorMax = new Vector2(1, 0.5f);
        checkBgRect.pivot = new Vector2(1, 0.5f);
        checkBgRect.anchoredPosition = new Vector2(-10, 0);
        checkBgRect.sizeDelta = new Vector2(50, 24);
        
        Image checkBgImg = checkBg.AddComponent<Image>();
        checkBgImg.color = defaultValue ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
        
        // Toggle checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(checkBg.transform, false);
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(defaultValue ? 0.5f : 0, 0);
        checkRect.anchorMax = new Vector2(defaultValue ? 1 : 0.5f, 1);
        checkRect.offsetMin = new Vector2(2, 2);
        checkRect.offsetMax = new Vector2(-2, -2);
        
        Image checkImg = checkmark.AddComponent<Image>();
        checkImg.color = Color.white;
        
        // Toggle component
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = defaultValue;
        toggle.graphic = checkImg;
        toggle.onValueChanged.AddListener((val) => {
            checkBgImg.color = val ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
            checkRect.anchorMin = new Vector2(val ? 0.5f : 0, 0);
            checkRect.anchorMax = new Vector2(val ? 1 : 0.5f, 1);
            onValueChanged?.Invoke(val);
        });
        
        yPos -= 35;
    }
    
    void CreateSlider(Transform parent, string label, float defaultValue, ref float yPos,
        System.Action<float> onValueChanged)
    {
        // Container
        GameObject container = new GameObject("Slider_" + label);
        container.transform.SetParent(parent, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1);
        containerRect.anchorMax = new Vector2(0.5f, 1);
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.anchoredPosition = new Vector2(0, yPos);
        containerRect.sizeDelta = new Vector2(280, 45);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(container.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(10, 0);
        labelRect.offsetMax = new Vector2(-10, 0);
        
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 14;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = Color.white;
        
        // Slider background
        GameObject sliderBg = new GameObject("SliderBg");
        sliderBg.transform.SetParent(container.transform, false);
        RectTransform sliderBgRect = sliderBg.AddComponent<RectTransform>();
        sliderBgRect.anchorMin = new Vector2(0, 0);
        sliderBgRect.anchorMax = new Vector2(1, 0.5f);
        sliderBgRect.offsetMin = new Vector2(10, 5);
        sliderBgRect.offsetMax = new Vector2(-10, -2);
        
        Image sliderBgImg = sliderBg.AddComponent<Image>();
        sliderBgImg.color = new Color(0.3f, 0.3f, 0.35f);
        
        // Fill area
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderBg.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = new Vector2(defaultValue, 1);
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;
        
        Image fillImg = fillArea.AddComponent<Image>();
        fillImg.color = new Color(0.6f, 0.5f, 0.8f);
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderBg.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(defaultValue, 0);
        handleRect.anchorMax = new Vector2(defaultValue, 1);
        handleRect.sizeDelta = new Vector2(10, 0);
        handleRect.anchoredPosition = Vector2.zero;
        
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.9f, 0.8f, 1f);
        
        // Slider component
        Slider slider = sliderBg.AddComponent<Slider>();
        slider.fillRect = fillAreaRect;
        slider.handleRect = handleRect;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = defaultValue;
        slider.onValueChanged.AddListener((val) => {
            fillAreaRect.anchorMax = new Vector2(val, 1);
            onValueChanged?.Invoke(val);
        });
        
        yPos -= 50;
    }
}
