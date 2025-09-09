using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class RuntimeCurveEditor : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private AnimationCurve currentCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    private CurveVisualElement curveVisualElement;
    private DropdownField presetDropdown;
    private Button saveButton;
    private Button saveAsButton;
    private Button newButton;
    private Toggle showControlsToggle;
    private FloatField rangeXField;
    private FloatField rangeYField;
    
    private VisualElement curveEditorPopup;
    private Button openEditorButton;
    private Button closeEditorButton;
    private bool isPopupOpen = false;
    
    private AnimationCurvePreset currentPreset = null;
    private List<AnimationCurvePreset> availablePresets = new List<AnimationCurvePreset>();
    
    // Shared editor support
    private CurvePreviewButton currentEditingButton = null;
    public static RuntimeCurveEditor SharedInstance { get; private set; }
    
    public AnimationCurve CurrentCurve
    {
        get { return currentCurve; }
        private set
        {
            SetCurveInternal(value, true);
        }
    }
    
    private void SetCurveInternal(AnimationCurve curve, bool updateDisplay)
    {
        currentCurve = curve;
        if (updateDisplay)
        {
            UpdateCurveDisplay();
        }
        OnCurveChanged?.Invoke(currentCurve);
    }
    
    public System.Action<AnimationCurve> OnCurveChanged;

    void Start()
    {
        // Set up singleton pattern for shared editor
        if (SharedInstance == null)
        {
            SharedInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (SharedInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument not found on RuntimeCurveEditor");
            return;
        }
        
        SetupUI();
        LoadAvailablePresets();
        
        // Start with popup closed
        TogglePopup(false);
    }

    private void SetupUI()
    {
        var root = uiDocument.rootVisualElement;
        
        // Create popup window (initially hidden) - no main UI button needed
        CreatePopupWindow(root);
    }
    
    private void CreatePopupWindow(VisualElement root)
    {
        // Create full-screen overlay for modal behavior
        curveEditorPopup = new VisualElement();
        curveEditorPopup.style.position = Position.Absolute;
        curveEditorPopup.style.top = 0;
        curveEditorPopup.style.left = 0;
        curveEditorPopup.style.width = Length.Percent(100);
        curveEditorPopup.style.height = Length.Percent(100);
        curveEditorPopup.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f); // Semi-transparent overlay
        curveEditorPopup.style.display = DisplayStyle.None; // Hidden by default
        
        // Create the actual editor window
        var editorWindow = new VisualElement();
        editorWindow.style.position = Position.Absolute;
        editorWindow.style.width = 800;
        editorWindow.style.height = 600;
        editorWindow.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        editorWindow.style.borderTopWidth = 2;
        editorWindow.style.borderBottomWidth = 2;
        editorWindow.style.borderLeftWidth = 2;
        editorWindow.style.borderRightWidth = 2;
        editorWindow.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        editorWindow.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        editorWindow.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        editorWindow.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        // Center the window
        editorWindow.style.left = Length.Percent(50);
        editorWindow.style.top = Length.Percent(50);
        editorWindow.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
        
        // Add title bar
        var titleBar = new VisualElement();
        titleBar.style.flexDirection = FlexDirection.Row;
        titleBar.style.height = 30;
        titleBar.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        titleBar.style.paddingLeft = 10;
        titleBar.style.paddingRight = 10;
        titleBar.style.justifyContent = Justify.SpaceBetween;
        titleBar.style.alignItems = Align.Center;
        
        var titleLabel = new Label("Animation Curve Editor");
        titleLabel.style.color = Color.white;
        titleBar.Add(titleLabel);
        
        closeEditorButton = new Button(() => TogglePopup(false)) { text = "✕" };
        closeEditorButton.style.width = 25;
        closeEditorButton.style.height = 25;
        closeEditorButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        titleBar.Add(closeEditorButton);
        
        editorWindow.Add(titleBar);
        
        // Add the curve editor content
        var contentArea = new VisualElement();
        contentArea.style.flexDirection = FlexDirection.Column;
        contentArea.style.flexGrow = 1;
        contentArea.style.paddingTop = 10;
        contentArea.style.paddingBottom = 10;
        contentArea.style.paddingLeft = 10;
        contentArea.style.paddingRight = 10;
        
        SetupControls(contentArea);
        SetupCurveDisplay(contentArea);
        
        editorWindow.Add(contentArea);
        curveEditorPopup.Add(editorWindow);
        root.Add(curveEditorPopup);
    }
    
    private void TogglePopup(bool open)
    {
        isPopupOpen = open;
        curveEditorPopup.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (open)
        {
            UpdateCurveDisplay(); // Refresh display when opening
        }
        else
        {
            // When closing, apply changes back to the editing button
            if (currentEditingButton != null)
            {
                currentEditingButton.Curve = currentCurve;
                currentEditingButton = null;
            }
        }
    }

    private void SetupControls(VisualElement parent)
    {
        var controlPanel = new VisualElement();
        controlPanel.style.flexDirection = FlexDirection.Row;
        controlPanel.style.height = 40;
        controlPanel.style.paddingBottom = 10;
        controlPanel.style.paddingTop = 10;
        controlPanel.style.paddingLeft = 10;
        controlPanel.style.paddingRight = 10;
        
        presetDropdown = new DropdownField("Preset:", new List<string> { "None" }, 0);
        presetDropdown.style.width = 300; // Wider to show full names
        presetDropdown.RegisterValueChangedCallback(OnPresetSelected);
        controlPanel.Add(presetDropdown);
        
        newButton = new Button(() => CreateNewCurve()) { text = "New" };
        newButton.style.width = 60;
        newButton.style.marginLeft = 10;
        controlPanel.Add(newButton);
        
        saveButton = new Button(() => SaveCurrentCurve()) { text = "Save" };
        saveButton.style.width = 60;
        saveButton.style.marginLeft = 10;
        saveButton.style.display = DisplayStyle.None; // Hidden by default
        controlPanel.Add(saveButton);
        
        saveAsButton = new Button(() => SaveAsCurve()) { text = "Save As" };
        saveAsButton.style.width = 70;
        saveAsButton.style.marginLeft = 10;
        controlPanel.Add(saveAsButton);
        
        showControlsToggle = new Toggle("Show Controls");
        showControlsToggle.value = true;
        showControlsToggle.style.marginLeft = 20;
        showControlsToggle.RegisterValueChangedCallback(OnShowControlsChanged);
        controlPanel.Add(showControlsToggle);
        
        var rangeLabel = new Label("Range:");
        rangeLabel.style.marginLeft = 20;
        controlPanel.Add(rangeLabel);
        
        rangeXField = new FloatField("X:");
        rangeXField.value = 1f;
        rangeXField.style.width = 80;
        rangeXField.style.marginLeft = 10;
        rangeXField.RegisterValueChangedCallback(OnRangeChanged);
        controlPanel.Add(rangeXField);
        
        rangeYField = new FloatField("Y:");
        rangeYField.value = 1f;
        rangeYField.style.width = 80;
        rangeYField.style.marginLeft = 10;
        rangeYField.RegisterValueChangedCallback(OnRangeChanged);
        controlPanel.Add(rangeYField);
        
        parent.Add(controlPanel);
    }

    private void SetupCurveDisplay(VisualElement parent)
    {
        curveVisualElement = new CurveVisualElement();
        curveVisualElement.style.alignSelf = Align.Center;
        curveVisualElement.style.marginTop = 20;
        curveVisualElement.style.marginBottom = 20;
        curveVisualElement.style.flexGrow = 1; // Take up remaining space
        curveVisualElement.OnPathChanged += OnPathChanged;
        
        parent.Add(curveVisualElement);
    }

    private void LoadAvailablePresets()
    {
        // Clear and reload all presets
        availablePresets.Clear();
        var presetNames = new List<string> { "None" };
        
#if UNITY_EDITOR
        // Find all AnimationCurvePreset assets in the project
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AnimationCurvePreset");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            AnimationCurvePreset preset = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationCurvePreset>(path);
            if (preset != null)
            {
                availablePresets.Add(preset);
                presetNames.Add(preset.name);
            }
        }
#else
        // In builds, use the manually assigned presets
        foreach (var preset in availablePresets)
        {
            if (preset != null)
            {
                presetNames.Add(preset.name);
            }
        }
#endif
        
        presetDropdown.choices = presetNames;
        Debug.Log($"Loaded {availablePresets.Count} curve presets");
    }

    private void OnPresetSelected(ChangeEvent<string> evt)
    {
        if (evt.newValue == "None")
        {
            currentPreset = null;
            UpdateSaveButtonVisibility();
            return;
        }
        
        var selectedPreset = availablePresets.Find(p => p != null && p.name == evt.newValue);
        if (selectedPreset != null)
        {
            currentPreset = selectedPreset;
            CurrentCurve = selectedPreset.GetCurveCopy();
            UpdateSaveButtonVisibility();
        }
    }
    
    private void UpdateSaveButtonVisibility()
    {
        saveButton.style.display = currentPreset != null ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void CreateNewCurve()
    {
        currentPreset = null;
        CurrentCurve = AnimationCurve.Linear(0, 0, 1, 1);
        presetDropdown.value = "None";
        UpdateSaveButtonVisibility();
    }

    private void SaveCurrentCurve()
    {
        if (currentPreset == null) return;
        
#if UNITY_EDITOR
        // Save to the existing preset
        currentPreset.SetCurve(currentCurve);
        UnityEditor.EditorUtility.SetDirty(currentPreset);
        UnityEditor.AssetDatabase.SaveAssets();
        
        Debug.Log($"Curve saved to existing preset: {currentPreset.name}");
#else
        Debug.Log("Saving curves at runtime requires custom implementation");
#endif
    }
    
    private void SaveAsCurve()
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Animation Curve Preset",
            "NewCurvePreset",
            "asset",
            "Save curve as preset");
        
        if (!string.IsNullOrEmpty(path))
        {
            var preset = ScriptableObject.CreateInstance<AnimationCurvePreset>();
            preset.SetCurve(currentCurve);
            preset.Description = "Runtime created curve";
            
            UnityEditor.AssetDatabase.CreateAsset(preset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            // Set this as the current preset and reload the dropdown
            currentPreset = preset;
            LoadAvailablePresets();
            presetDropdown.value = preset.name;
            UpdateSaveButtonVisibility();
            
            Debug.Log($"Curve preset saved to {path}");
        }
#else
        Debug.Log("Saving curves at runtime requires custom implementation");
#endif
    }

    private void OnShowControlsChanged(ChangeEvent<bool> evt)
    {
        curveVisualElement.ShowControlPoints = evt.newValue;
    }

    private void OnRangeChanged(ChangeEvent<float> evt)
    {
        Vector2 newRange = new Vector2(rangeXField.value, rangeYField.value);
        curveVisualElement.CurveRange = newRange;
    }

    private void OnPathChanged(Path path)
    {
        // Only update the curve if we're not currently dragging to avoid feedback loops
        if (curveVisualElement == null || !curveVisualElement.IsDragging)
        {
            // Convert path to curve and update both internal state and display
            CurrentCurve = AnimationCurveAdapter.PathToAnimationCurve(path);
        }
    }

    private void UpdateCurveDisplay()
    {
        if (curveVisualElement != null)
        {
            Debug.Log("UpdateCurveDisplay: Converting curve back to path");
            Path path = AnimationCurveAdapter.AnimationCurveToPath(currentCurve);
            
            Debug.Log("Points after conversion back to path:");
            for (int i = 0; i < path.NumPoints; i++)
            {
                Debug.Log($"  Point {i}: {path[i]}");
            }
            
            curveVisualElement.CurrentPath = path;
        }
    }

    public void SetCurve(AnimationCurve curve)
    {
        CurrentCurve = curve;
    }
    
    // Public method for preview buttons to open the editor
    public void EditCurveFromButton(CurvePreviewButton button)
    {
        Debug.Log($"EditCurveFromButton called - button: {(button != null ? button.CurveName : "null")}");
        
        if (button == null)
        {
            Debug.LogError("EditCurveFromButton: button is null");
            return;
        }
        
        Debug.Log($"Setting up editor for curve: {button.CurveName}");
        currentEditingButton = button;
        currentPreset = null; // Clear preset when editing from button
        CurrentCurve = new AnimationCurve(button.Curve.keys); // Copy the curve
        UpdateSaveButtonVisibility();
        
        Debug.Log("Opening popup...");
        TogglePopup(true);
        Debug.Log($"Popup should be open - isPopupOpen: {isPopupOpen}");
    }
    
    // Static convenience method for easy access
    public static void OpenCurveEditor(CurvePreviewButton button)
    {
        Debug.Log($"OpenCurveEditor called for button: {(button != null ? button.CurveName : "null")}");
        Debug.Log($"SharedInstance exists: {SharedInstance != null}");
        
        if (SharedInstance != null)
        {
            Debug.Log("Calling EditCurveFromButton...");
            SharedInstance.EditCurveFromButton(button);
        }
        else
        {
            Debug.LogError("No RuntimeCurveEditor instance found. Make sure there's a RuntimeCurveEditor in the scene.");
            
            // Try to find one in the scene as fallback
            var editor = FindFirstObjectByType<RuntimeCurveEditor>();
            if (editor != null)
            {
                Debug.Log("Found RuntimeCurveEditor in scene, using as fallback");
                SharedInstance = editor;
                SharedInstance.EditCurveFromButton(button);
            }
        }
    }

    public void LoadPreset(AnimationCurvePreset preset)
    {
        if (preset != null)
        {
            CurrentCurve = preset.GetCurveCopy();
            
            if (availablePresets.Contains(preset))
            {
                presetDropdown.value = preset.name;
            }
        }
    }
}