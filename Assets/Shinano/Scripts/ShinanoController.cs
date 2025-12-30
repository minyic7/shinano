using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Shinano Character Expression & Customization Controller
/// Controls facial expressions, costume, hair, body features, and custom animations
/// </summary>
public class ShinanoController : MonoBehaviour
{
    [Header("Character Reference")]
    public GameObject shinanoCharacter;
    public Animator characterAnimator;
    
    [Header("Camera Reference")]
    public Camera mainCamera;
    
    [Header("Custom Animations")]
    public List<AnimationClip> customAnimations = new List<AnimationClip>();
    private List<string> customAnimationNames = new List<string>();
    
    [Header("UI Settings")]
    public KeyCode togglePanelKey = KeyCode.Tab;
    public bool panelVisible = true;
    
    // UI References
    private Canvas uiCanvas;
    private GameObject panelRoot;
    
    // State tracking
    private float characterRotation = 0f;
    private int currentFSet = 0;
    private bool isPlayingAction = false;
    private bool isBlendingOut = false;
    private PlayableGraph actionGraph;
    private AnimationLayerMixerPlayable currentLayerMixer;
    private AnimatorControllerPlayable currentAnimatorPlayable;
    private int currentAnimationIndex = -1;
    private Coroutine currentAnimationCoroutine;
    private Vector3 savedCharacterPosition;  // Save position before custom animation
    
    // Direct control references for toggles that need to work during custom animations
    private GameObject earObject;
    private GameObject tailObject;
    
    // Hip blendshape is on multiple meshes - need to control all of them
    private List<SkinnedMeshRenderer> hipRenderers = new List<SkinnedMeshRenderer>();
    private List<int> hipBlendShapeIndices = new List<int>();
    
    // State tracking for toggles (to reapply in LateUpdate during custom animations)
    private bool earToggleState = true;   // true = visible
    private bool tailToggleState = true;  // true = visible
    private bool hipToggleState = false;  // true = big hip
    
    // Colors
    private Color panelBg = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    private Color sectionColor = new Color(0.7f, 0.5f, 0.8f);
    private Color textColor = new Color(0.9f, 0.9f, 0.95f);
    
    // Expression data - matches animator F_Parts: 0=Default, 1=Cheek, 2=Heart, 3=Kirakira, 4=Sweat, 5=Tear, 6=Dead, 7=Guruguru, 8=White
    private string[] eyeEffects = { "Default", "Cheek", "Heart", "Sparkle", "Sweat", "Tear", "Dead", "Spiral", "White" };
    
    // F_Set 0: Joy expressions (Gesture 0-7 triggers)
    private string[] gestureSet0 = { "Default", "Smile1", "Joy2", "Wink1", "Kirakira", "EyeCls1", "Surprised", "Angry2" };
    // F_Set 1: Calm expressions (Gesture 0-7 triggers)
    private string[] gestureSet1 = { "Default", "Smile2", "Joy1", "Wink2", "Nagomi", "EyeCls2", "Confuse", "ZitoAngry" };
    // F_Set 2: Complex expressions (Gesture 0-7 triggers)
    private string[] gestureSet2 = { "Default", "Smile3", "Cry", "Grin", "Doya", "EyeCls3", "Kyoton", "Bitter" };
    
    private Text[] leftGestureLabels;
    private Text[] rightGestureLabels;
    private List<Image> animationButtonImages = new List<Image>();
    
    // Animation button colors
    private Color animBtnInactive = new Color(0.3f, 0.4f, 0.5f);
    private Color animBtnActive = new Color(0.4f, 0.7f, 0.4f);
    
    void Start()
    {
        Debug.Log("[Shinano] Controller starting...");
        FindCharacter();
        FindCamera();
        LoadCustomAnimations();
        CreateUI();
        Debug.Log($"[Shinano] Setup complete. Character: {(shinanoCharacter != null ? shinanoCharacter.name : "NOT FOUND")}, Animator: {(characterAnimator != null ? "OK" : "NOT FOUND")}");
    }
    
    void OnDestroy()
    {
        if (actionGraph.IsValid())
            actionGraph.Destroy();
    }
    
    void LoadCustomAnimations()
    {
        #if UNITY_EDITOR
        // In Editor, automatically load animations from Custom_animation folder
        string folderPath = "Assets/Shinano/Animation/Custom_animation";
        
        // Load animations embedded in FBX files (Mixamo style)
        string[] fbxGuids = UnityEditor.AssetDatabase.FindAssets("t:Model", new[] { folderPath });
        foreach (string guid in fbxGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string fbxName = System.IO.Path.GetFileNameWithoutExtension(path);
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    if (!customAnimations.Contains(clip))
                    {
                        customAnimations.Add(clip);
                        // Use FBX filename if clip name is generic like "mixamo.com"
                        string displayName = (clip.name == "mixamo.com" || clip.name.Contains("Take")) ? fbxName : clip.name;
                        customAnimationNames.Add(displayName);
                        Debug.Log($"[Shinano] Loaded animation: {displayName} ({clip.length}s) from {fbxName}");
                    }
                }
            }
        }
        
        // Also load standalone animation clips
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            // Skip clips that are inside FBX files (already loaded above)
            if (path.EndsWith(".anim"))
            {
                AnimationClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null && !customAnimations.Contains(clip))
                {
                    customAnimations.Add(clip);
                    customAnimationNames.Add(clip.name);
                    Debug.Log($"[Shinano] Loaded animation: {clip.name} ({clip.length}s)");
                }
            }
        }
        #endif
        
        Debug.Log($"[Shinano] Total custom animations loaded: {customAnimations.Count}");
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
        
        // Find direct control references for toggles
        if (shinanoCharacter != null)
        {
            // Find ear and tail objects
            earObject = FindChildRecursive(shinanoCharacter.transform, "Other_ear");
            tailObject = FindChildRecursive(shinanoCharacter.transform, "Other_tail");
            
            // Find all renderers with Hip_big blendshape (multiple meshes have it)
            // Based on animation files: Body_base, Cloth_dress, Cloth_under_shorts, Cloth_skirt, Cloth_tights
            string[] hipMeshNames = { "Body_base", "Cloth_dress", "Cloth_under_shorts", "Cloth_skirt", "Cloth_tights" };
            hipRenderers.Clear();
            hipBlendShapeIndices.Clear();
            
            foreach (string meshName in hipMeshNames)
            {
                GameObject meshObj = FindChildRecursive(shinanoCharacter.transform, meshName);
                if (meshObj != null)
                {
                    SkinnedMeshRenderer smr = meshObj.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null && smr.sharedMesh != null)
                    {
                        int idx = smr.sharedMesh.GetBlendShapeIndex("Hip_big");
                        if (idx >= 0)
                        {
                            hipRenderers.Add(smr);
                            hipBlendShapeIndices.Add(idx);
                            Debug.Log($"[Shinano] Found Hip_big blendshape on {meshName} at index: {idx}");
                        }
                    }
                }
            }
            
            Debug.Log($"[Shinano] Direct control refs - Ear: {(earObject != null ? "Found" : "NOT FOUND")}, Tail: {(tailObject != null ? "Found" : "NOT FOUND")}, Hip meshes: {hipRenderers.Count}");
        }
    }
    
    GameObject FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;
            
            GameObject found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
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
    
    // LateUpdate runs after all animation processing - use it to reapply toggle states
    // that get overwritten by the PlayableGraph during custom animation playback
    void LateUpdate()
    {
        if (isPlayingAction)
        {
            // Reapply ear toggle state
            if (earObject != null)
            {
                earObject.SetActive(earToggleState);
            }
            
            // Reapply tail toggle state
            if (tailObject != null)
            {
                tailObject.SetActive(tailToggleState);
            }
            
            // Reapply hip blendshape state on ALL meshes that have it
            float hipWeight = hipToggleState ? 100f : 0f;
            for (int i = 0; i < hipRenderers.Count; i++)
            {
                if (hipRenderers[i] != null && hipBlendShapeIndices[i] >= 0)
                {
                    hipRenderers[i].SetBlendShapeWeight(hipBlendShapeIndices[i], hipWeight);
                }
            }
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
        
        // Create main panel - left side of screen (400px wide)
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
        
        // === COSTUME ===
        AddSectionHeader(panelRoot.transform, "👗 Costume", ref y);
        AddToggleRow(panelRoot.transform, new string[]{"Sweater","Dress","Skirt","Tights","Boots"}, ref y,
            new string[]{"Sweater","Dress","Skirt","Tights","Boots"}, new bool[]{true,true,true,true,true}, true);
        AddToggleRow(panelRoot.transform, new string[]{"Bra","Shorts"}, ref y,
            new string[]{"Bra","Shorts"}, new bool[]{true, true}, true);
        
        // === HAIR ===
        AddSectionHeader(panelRoot.transform, "💇 Hair", ref y);
        AddToggleRow(panelRoot.transform, new string[]{"Bangs","Half-up"}, ref y,
            new string[]{"Bangs","Half"}, new bool[]{true, true}, true);
        AddSlider(panelRoot.transform, "Length", ref y, (v) => SetAnimatorFloat("Length", v));
        AddButtonGrid(panelRoot.transform, new string[]{"Default","Braid","Side L","Side R","All"}, 5, ref y, (i) => SetAnimatorInt("Hair", i));
        
        // === BODY ===
        AddSectionHeader(panelRoot.transform, "✨ Body", ref y);
        AddBodyToggles(panelRoot.transform, ref y);
        AddSlider(panelRoot.transform, "Breast", ref y, (v) => SetAnimatorFloat("Breast", v));
        
        // === CAMERA ===
        AddSectionHeader(panelRoot.transform, "📷 Camera", ref y);
        AddSlider(panelRoot.transform, "Rotate", ref y, (v) => {
            characterRotation = (v - 0.5f) * 360f;
            if (shinanoCharacter != null)
                shinanoCharacter.transform.rotation = Quaternion.Euler(0, characterRotation, 0);
        });
        AddCameraDistanceSlider(panelRoot.transform, ref y);
        
        // === CUSTOM ANIMATIONS ===
        AddSectionHeader(panelRoot.transform, "🎬 Custom Animations", ref y);
        AddCustomAnimationButtons(panelRoot.transform, ref y);
    }
    
    void AddCustomAnimationButtons(Transform parent, ref float y)
    {
        animationButtonImages.Clear();
        
        if (customAnimations.Count == 0)
        {
            AddLabel(parent, "No animations in Custom_animation folder", 10, y, new Color(0.6f, 0.6f, 0.6f));
            y -= 20;
            return;
        }
        
        // Create buttons for each custom animation (no Stop button - use toggle)
        string[] animNames = new string[customAnimations.Count];
        for (int i = 0; i < customAnimations.Count; i++)
        {
            // Use display name if available, otherwise use clip name
            if (i < customAnimationNames.Count)
                animNames[i] = customAnimationNames[i];
            else
                animNames[i] = customAnimations[i].name;
        }
        
        // Add buttons in a grid
        float btnW = 120;
        float btnH = 28;
        float spacing = 4;
        int cols = 3;
        float startX = 10;
        
        for (int i = 0; i < animNames.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;
            
            float x = startX + col * (btnW + spacing);
            float yPos = y - row * (btnH + spacing);
            
            int idx = i;
            CreateAnimationButton(parent, animNames[i], x, yPos, btnW, btnH, idx);
        }
        
        int rows = (animNames.Length + cols - 1) / cols;
        y -= rows * (btnH + spacing) + 8;
    }
    
    void CreateAnimationButton(Transform parent, string label, float x, float yPos, float w, float h, int idx)
    {
        GameObject btn = new GameObject("AnimBtn_" + idx);
        btn.transform.SetParent(parent, false);
        
        RectTransform rect = btn.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, yPos);
        rect.sizeDelta = new Vector2(w, h);
        
        Image img = btn.AddComponent<Image>();
        img.color = animBtnInactive;
        
        // Store button image for visual feedback
        animationButtonImages.Add(img);
        
        Button button = btn.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(() => {
            PlayCustomAnimation(idx);
        });
        
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btn.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(4, 0);
        txtRt.offsetMax = new Vector2(-4, 0);
        
        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 10;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
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
            
            // Initialize the animator parameter to match the default toggle state
            SetAnimatorBool(param, invert ? !isOn : isOn);
            
            toggle.onValueChanged.AddListener((val) => {
                indImg.color = val ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
                SetAnimatorBool(param, invert ? !val : val);
            });
        }
        
        y -= 36;
    }
    
    // Specialized toggles for Ear/Tail/Hip with direct control (bypasses animator during animations)
    void AddBodyToggles(Transform parent, ref float y)
    {
        string[] labels = { "Ears", "Tail", "Big Hip" };
        bool[] defaults = { true, true, false };  // Ears ON, Tail ON, Hip normal
        
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
            
            int toggleIndex = i;
            
            // Initialize state
            ApplyBodyToggle(toggleIndex, isOn);
            
            toggle.onValueChanged.AddListener((val) => {
                indImg.color = val ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.5f, 0.3f, 0.3f);
                ApplyBodyToggle(toggleIndex, val);
            });
        }
        
        y -= 36;
    }
    
    // Direct control of body features - works during custom animations
    // Saves state for LateUpdate to reapply after animation processing
    void ApplyBodyToggle(int toggleIndex, bool isOn)
    {
        switch (toggleIndex)
        {
            case 0: // Ears - inverted logic: toggle ON = visible, Ear param = false
                earToggleState = isOn;  // Save state for LateUpdate
                SetAnimatorBool("Ear", !isOn);
                if (earObject != null)
                {
                    earObject.SetActive(isOn);
                    Debug.Log($"[Shinano] Ear toggle: {isOn} (saved for LateUpdate)");
                }
                break;
                
            case 1: // Tail - inverted logic: toggle ON = visible, Tail param = false
                tailToggleState = isOn;  // Save state for LateUpdate
                SetAnimatorBool("Tail", !isOn);
                if (tailObject != null)
                {
                    tailObject.SetActive(isOn);
                    Debug.Log($"[Shinano] Tail toggle: {isOn} (saved for LateUpdate)");
                }
                break;
                
            case 2: // Hip - direct logic: toggle ON = big hip, Hip param = true
                hipToggleState = isOn;  // Save state for LateUpdate
                SetAnimatorBool("Hip", isOn);
                // Apply to ALL meshes that have Hip_big blendshape
                float weight = isOn ? 100f : 0f;
                for (int i = 0; i < hipRenderers.Count; i++)
                {
                    if (hipRenderers[i] != null && hipBlendShapeIndices[i] >= 0)
                    {
                        hipRenderers[i].SetBlendShapeWeight(hipBlendShapeIndices[i], weight);
                    }
                }
                Debug.Log($"[Shinano] Hip blendshape on {hipRenderers.Count} meshes: {weight} (saved for LateUpdate)");
                break;
        }
    }
    
    void AddSlider(Transform parent, string label, ref float y, System.Action<float> onChange)
    {
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
        
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderBg.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0.5f, 1);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = sectionColor;
        
        GameObject handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sliderBg.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = Vector2.zero;
        handleAreaRt.offsetMax = Vector2.zero;
        
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
    
    void AddCameraDistanceSlider(Transform parent, ref float y)
    {
        // Get current camera z position (default is 2, positive because camera faces origin from +z)
        float currentZ = 2f;
        if (mainCamera != null)
        {
            currentZ = mainCamera.transform.position.z;  // Positive z (camera at z=2 looking at origin)
        }
        
        // Distance range: 1 (close) to 2 (far), safe range per user preference
        float minDist = 1f;
        float maxDist = 2f;
        float defaultNormalized = Mathf.InverseLerp(minDist, maxDist, currentZ);
        
        GameObject lblObj = new GameObject("SliderLabel_Distance");
        lblObj.transform.SetParent(parent, false);
        RectTransform lblRt = lblObj.AddComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0, 1);
        lblRt.anchorMax = new Vector2(0, 1);
        lblRt.pivot = new Vector2(0, 1);
        lblRt.anchoredPosition = new Vector2(10, y);
        lblRt.sizeDelta = new Vector2(60, 20);
        
        Text lbl = lblObj.AddComponent<Text>();
        lbl.text = "Distance";
        lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lbl.fontSize = 10;
        lbl.alignment = TextAnchor.MiddleLeft;
        lbl.color = textColor;
        
        GameObject sliderBg = new GameObject("Slider_Distance");
        sliderBg.transform.SetParent(parent, false);
        RectTransform bgRt = sliderBg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 1);
        bgRt.anchorMax = new Vector2(1, 1);
        bgRt.pivot = new Vector2(0, 1);
        bgRt.anchoredPosition = new Vector2(70, y - 2);
        bgRt.sizeDelta = new Vector2(-90, 16);
        
        Image bgImg = sliderBg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.2f);
        
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderBg.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(defaultNormalized, 1);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = sectionColor;
        
        GameObject handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sliderBg.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = Vector2.zero;
        handleAreaRt.offsetMax = Vector2.zero;
        
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
        slider.value = defaultNormalized;
        slider.onValueChanged.AddListener((v) => {
            if (mainCamera != null)
            {
                // Map 0-1 slider to distance range (1=close, 2=far)
                float distance = Mathf.Lerp(minDist, maxDist, v);
                Vector3 pos = mainCamera.transform.position;
                pos.z = distance;  // Positive z (camera at z=2 looking at origin)
                mainCamera.transform.position = pos;
                Debug.Log($"[Shinano] Camera distance: {distance}");
            }
        });
        
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
        
        // Also update the AnimatorControllerPlayable if custom animation is playing
        if (isPlayingAction && currentAnimatorPlayable.IsValid())
        {
            currentAnimatorPlayable.SetInteger(param, val);
        }
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
        
        // Also update the AnimatorControllerPlayable if custom animation is playing
        if (isPlayingAction && currentAnimatorPlayable.IsValid())
        {
            currentAnimatorPlayable.SetBool(param, val);
        }
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
        
        // Also update the AnimatorControllerPlayable if custom animation is playing
        if (isPlayingAction && currentAnimatorPlayable.IsValid())
        {
            currentAnimatorPlayable.SetFloat(param, val);
        }
        Debug.Log($"[Shinano] SetFloat {param}={val}");
    }
    
    // === CUSTOM ANIMATION METHODS ===
    
    void UpdateAnimationButtonColors()
    {
        for (int i = 0; i < animationButtonImages.Count; i++)
        {
            if (animationButtonImages[i] != null)
            {
                bool isActive = (i == currentAnimationIndex && isPlayingAction);
                animationButtonImages[i].color = isActive ? animBtnActive : animBtnInactive;
            }
        }
    }
    
    void PlayCustomAnimation(int index)
    {
        if (index < 0 || index >= customAnimations.Count)
        {
            Debug.LogWarning($"[Shinano] Invalid animation index: {index}");
            return;
        }
        
        // Toggle behavior: if same animation is playing, stop it
        if (currentAnimationIndex == index && isPlayingAction && !isBlendingOut)
        {
            StopCustomAnimationWithBlendOut();
            return;
        }
        
        AnimationClip clip = customAnimations[index];
        if (clip == null)
        {
            Debug.LogWarning("[Shinano] Animation clip is null!");
            return;
        }
        
        if (characterAnimator == null)
        {
            Debug.LogWarning("[Shinano] No animator found!");
            return;
        }
        
        // Stop any currently playing animation immediately (no blend) when switching to a different animation
        StopCustomAnimationImmediate();
        
        // Save character position before playing animation (to restore after)
        if (shinanoCharacter != null)
        {
            savedCharacterPosition = shinanoCharacter.transform.position;
        }
        
        currentAnimationIndex = index;
        UpdateAnimationButtonColors();  // Show active state
        currentAnimationCoroutine = StartCoroutine(PlayAnimationCoroutine(clip));
    }
    
    void StopCustomAnimationWithBlendOut()
    {
        if (isBlendingOut) return;  // Already blending out
        
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
        
        StartCoroutine(BlendOutAndStopCoroutine());
    }
    
    IEnumerator BlendOutAndStopCoroutine()
    {
        if (!actionGraph.IsValid())
        {
            isPlayingAction = false;
            currentAnimationIndex = -1;
            yield break;
        }
        
        isBlendingOut = true;
        Debug.Log("[Shinano] Blending out custom animation...");
        
        // Blend out - smoothly transition back to standing
        float blendOutTime = 0.3f;
        float t = 0;
        while (t < blendOutTime && actionGraph.IsValid())
        {
            t += Time.deltaTime;
            float weight = Mathf.SmoothStep(1f, 0f, t / blendOutTime);
            if (actionGraph.IsValid() && currentLayerMixer.IsValid())
                currentLayerMixer.SetInputWeight(1, weight);
            yield return null;
        }
        
        // Clean up
        if (actionGraph.IsValid())
        {
            actionGraph.Destroy();
        }
        
        // Restore character position after animation ends
        if (shinanoCharacter != null)
        {
            shinanoCharacter.transform.position = savedCharacterPosition;
        }
        
        isPlayingAction = false;
        isBlendingOut = false;
        currentAnimationIndex = -1;
        currentAnimatorPlayable = default;  // Clear the playable reference
        UpdateAnimationButtonColors();  // Reset all buttons to inactive
        Debug.Log("[Shinano] Stopped custom animation with blend-out");
    }
    
    void StopCustomAnimationImmediate()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
        
        if (actionGraph.IsValid())
        {
            actionGraph.Destroy();
        }
        
        // Restore character position after animation ends
        if (shinanoCharacter != null && isPlayingAction)
        {
            shinanoCharacter.transform.position = savedCharacterPosition;
        }
        
        isPlayingAction = false;
        isBlendingOut = false;
        currentAnimationIndex = -1;
        currentAnimatorPlayable = default;  // Clear the playable reference
        UpdateAnimationButtonColors();  // Reset all buttons to inactive
    }
    
    void StopCustomAnimation()
    {
        // Use blend-out version for user-initiated stops
        if (isPlayingAction && !isBlendingOut)
        {
            StopCustomAnimationWithBlendOut();
        }
        else
        {
            StopCustomAnimationImmediate();
        }
    }
    
    IEnumerator PlayAnimationCoroutine(AnimationClip clip)
    {
        isPlayingAction = true;
        isBlendingOut = false;
        Debug.Log($"[Shinano] Playing custom animation: {clip.name} ({clip.length}s)");
        
        // Create PlayableGraph for the animation
        actionGraph = PlayableGraph.Create("CustomAnimation");
        actionGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        
        var playableOutput = AnimationPlayableOutput.Create(actionGraph, "Animation", characterAnimator);
        
        // Create a layer mixer to blend with existing animation
        currentLayerMixer = AnimationLayerMixerPlayable.Create(actionGraph, 2);
        
        // Layer 0: Base animator controller
        currentAnimatorPlayable = AnimatorControllerPlayable.Create(actionGraph, characterAnimator.runtimeAnimatorController);
        
        // Copy all parameter values from the original animator to the new playable
        // This preserves toggle states (Bra, Shorts, etc.)
        for (int i = 0; i < characterAnimator.parameterCount; i++)
        {
            var param = characterAnimator.GetParameter(i);
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    currentAnimatorPlayable.SetBool(param.nameHash, characterAnimator.GetBool(param.nameHash));
                    break;
                case AnimatorControllerParameterType.Int:
                    currentAnimatorPlayable.SetInteger(param.nameHash, characterAnimator.GetInteger(param.nameHash));
                    break;
                case AnimatorControllerParameterType.Float:
                    currentAnimatorPlayable.SetFloat(param.nameHash, characterAnimator.GetFloat(param.nameHash));
                    break;
            }
        }
        
        currentLayerMixer.ConnectInput(0, currentAnimatorPlayable, 0, 1.0f);
        
        // Layer 1: Custom animation clip (start at 0 weight for blend-in)
        var clipPlayable = AnimationClipPlayable.Create(actionGraph, clip);
        clipPlayable.SetApplyFootIK(false);
        currentLayerMixer.ConnectInput(1, clipPlayable, 0, 0f);  // Start at 0 weight
        
        // Set layer to override mode
        currentLayerMixer.SetLayerAdditive(1, false);
        
        playableOutput.SetSourcePlayable(currentLayerMixer);
        
        actionGraph.Play();
        
        // Blend in - smoothly transition from standing to the custom animation
        float blendInTime = 0.3f;
        float t = 0;
        while (t < blendInTime && actionGraph.IsValid() && !isBlendingOut)
        {
            t += Time.deltaTime;
            float weight = Mathf.SmoothStep(0f, 1f, t / blendInTime);
            if (actionGraph.IsValid() && currentLayerMixer.IsValid())
                currentLayerMixer.SetInputWeight(1, weight);
            yield return null;
        }
        
        // Ensure full weight after blend-in (unless we're blending out)
        if (actionGraph.IsValid() && currentLayerMixer.IsValid() && !isBlendingOut)
            currentLayerMixer.SetInputWeight(1, 1.0f);
        
        // If animation is not looping, wait for it to finish then auto-stop
        if (!clip.isLooping)
        {
            float remainingTime = Mathf.Max(0f, clip.length - blendInTime);
            float elapsed = 0f;
            
            // Wait but check for blend-out request
            while (elapsed < remainingTime && !isBlendingOut)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Only auto blend-out if not already being stopped externally
            if (!isBlendingOut && actionGraph.IsValid())
            {
                isBlendingOut = true;
                
                // Blend out - smoothly transition back to standing
                float blendOutTime = 0.3f;
                float blendOutT = 0;
                while (blendOutT < blendOutTime && actionGraph.IsValid())
                {
                    blendOutT += Time.deltaTime;
                    float weight = Mathf.SmoothStep(1f, 0f, blendOutT / blendOutTime);
                    if (actionGraph.IsValid() && currentLayerMixer.IsValid())
                        currentLayerMixer.SetInputWeight(1, weight);
                    yield return null;
                }
                
                StopCustomAnimationImmediate();
            }
        }
        // If looping, it will play until manually stopped via toggle or Stop button
    }
}
