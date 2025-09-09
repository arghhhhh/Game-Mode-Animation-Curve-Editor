using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class CurvePreviewButton : MonoBehaviour
{
    [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private string curveName = "Curve";
    [SerializeField] private Vector2 previewSize = new Vector2(120, 60);
    [SerializeField] private Color curveColor = Color.cyan;
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color borderColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    
    private UIDocument uiDocument;
    private VisualElement previewContainer;
    private Button previewButton;
    private CurvePreviewElement curvePreviewElement;
    
    public AnimationCurve Curve 
    { 
        get { return curve; } 
        set 
        { 
            curve = value; 
            if (curvePreviewElement != null) 
                curvePreviewElement.UpdateCurve(curve); 
        } 
    }
    
    // Method to update the curve from the editor (bypasses conversion issues)
    public void UpdateFromEditor(AnimationCurve editedCurve)
    {
        // Store the edited curve as the new original
        curve = new AnimationCurve(editedCurve.keys);
        
        // Update the preview display
        if (curvePreviewElement != null)
            curvePreviewElement.UpdateCurve(curve);
    }
    
    public string CurveName 
    { 
        get { return curveName; } 
        set { curveName = value; } 
    }
    
    public System.Action<CurvePreviewButton> OnCurveEditRequested;

    void Start()
    {
        SetupUI();
    }
    
    private void SetupUI()
    {
        // Get or create UIDocument
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }
        
        var root = uiDocument.rootVisualElement;
        
        // Create preview button container
        previewContainer = new VisualElement();
        previewContainer.style.position = Position.Absolute;
        previewContainer.style.top = pendingPosition.y;
        previewContainer.style.left = pendingPosition.x;
        previewContainer.style.flexDirection = FlexDirection.Column;
        previewContainer.style.alignItems = Align.Center;
        
        // Create curve name label
        var nameLabel = new Label(curveName);
        nameLabel.style.color = Color.white;
        nameLabel.style.fontSize = 12;
        nameLabel.style.marginBottom = 4;
        previewContainer.Add(nameLabel);
        
        // Create preview button
        previewButton = new Button(() => OnPreviewClicked());
        previewButton.style.width = previewSize.x;
        previewButton.style.height = previewSize.y;
        previewButton.style.paddingTop = 0;
        previewButton.style.paddingBottom = 0;
        previewButton.style.paddingLeft = 0;
        previewButton.style.paddingRight = 0;
        previewButton.style.marginTop = 0;
        previewButton.style.marginBottom = 0;
        previewButton.style.marginLeft = 0;
        previewButton.style.marginRight = 0;
        previewButton.style.backgroundColor = backgroundColor;
        previewButton.style.borderTopColor = borderColor;
        previewButton.style.borderBottomColor = borderColor;
        previewButton.style.borderLeftColor = borderColor;
        previewButton.style.borderRightColor = borderColor;
        previewButton.style.borderTopWidth = 1;
        previewButton.style.borderBottomWidth = 1;
        previewButton.style.borderLeftWidth = 1;
        previewButton.style.borderRightWidth = 1;
        
        // Create curve preview element
        curvePreviewElement = new CurvePreviewElement(curve, previewSize, curveColor);
        previewButton.Add(curvePreviewElement);
        
        previewContainer.Add(previewButton);
        root.Add(previewContainer);
    }
    
    private void OnPreviewClicked()
    {
        // Always notify any custom listeners first
        OnCurveEditRequested?.Invoke(this);
        
        // Always try to open the shared editor (the callback is just for logging/notification)
        RuntimeCurveEditor.OpenCurveEditor(this);
    }
    
    private Vector2 pendingPosition = new Vector2(10, 10);
    
    public void SetPosition(Vector2 position)
    {
        pendingPosition = position;
        
        if (previewContainer != null)
        {
            previewContainer.style.left = position.x;
            previewContainer.style.top = position.y;
            Debug.Log($"Set position for {curveName}: {position} - Container exists: {previewContainer != null}");
        }
        else
        {
            Debug.LogWarning($"Cannot set position for {curveName}: {position} - Container not ready, will apply on Start");
        }
    }
}

// Custom visual element for drawing curve preview
public class CurvePreviewElement : VisualElement
{
    private AnimationCurve currentCurve;
    private Vector2 size;
    private Color curveColor;
    private const int CURVE_RESOLUTION = 50;
    
    public CurvePreviewElement(AnimationCurve curve, Vector2 previewSize, Color color)
    {
        currentCurve = curve;
        size = previewSize;
        curveColor = color;
        
        style.width = size.x;
        style.height = size.y;
        
        generateVisualContent += OnGenerateVisualContent;
    }
    
    public void UpdateCurve(AnimationCurve newCurve)
    {
        currentCurve = newCurve;
        MarkDirtyRepaint();
    }
    
    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (currentCurve == null || currentCurve.length < 2)
            return;
            
        var painter = mgc.painter2D;
        
        // Draw grid lines
        DrawGrid(painter);
        
        // Draw curve
        DrawCurve(painter);
    }
    
    private void DrawGrid(Painter2D painter)
    {
        painter.strokeColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        painter.lineWidth = 1f;
        
        // Vertical lines
        for (int i = 1; i < 4; i++)
        {
            float x = (size.x * i) / 4f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, 0));
            painter.LineTo(new Vector2(x, size.y));
            painter.Stroke();
        }
        
        // Horizontal lines  
        for (int i = 1; i < 4; i++)
        {
            float y = (size.y * i) / 4f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, y));
            painter.LineTo(new Vector2(size.x, y));
            painter.Stroke();
        }
    }
    
    private void DrawCurve(Painter2D painter)
    {
        painter.strokeColor = curveColor;
        painter.lineWidth = 2f;
        
        // Get curve bounds
        float minTime = currentCurve.keys[0].time;
        float maxTime = currentCurve.keys[currentCurve.length - 1].time;
        
        // Use fixed value range (0 to 1) to show accurate proportions like Unity's inspector
        float minValue = 0f;
        float maxValue = 1f;
        
        // Find actual curve value range by evaluating the curve (NOT by looking at control points)
        float actualMinValue = float.MaxValue;
        float actualMaxValue = float.MinValue;
        float minValueTime = 0f;
        float maxValueTime = 0f;
        
        // Use higher resolution for better range detection
        int rangeResolution = CURVE_RESOLUTION * 2;
        for (int i = 0; i <= rangeResolution; i++)
        {
            float t = minTime + (maxTime - minTime) * (i / (float)rangeResolution);
            float value = currentCurve.Evaluate(t);
            
            if (value < actualMinValue)
            {
                actualMinValue = value;
                minValueTime = t;
            }
            if (value > actualMaxValue)
            {
                actualMaxValue = value;
                maxValueTime = t;
            }
        }
        
        // Always use the actual evaluated range for accuracy
        // This ensures the preview matches what AnimationCurve.Evaluate() returns
        minValue = Mathf.Min(0f, actualMinValue - 0.02f);
        maxValue = Mathf.Max(1f, actualMaxValue + 0.02f);
        
        Debug.Log($"Curve range detection - Actual: [{actualMinValue:F3}, {actualMaxValue:F3}], Display: [{minValue:F3}, {maxValue:F3}]");
        Debug.Log($"Extrema positions - Min: {actualMinValue:F3} at time {minValueTime:F3}, Max: {actualMaxValue:F3} at time {maxValueTime:F3}");
        
        // Also log some sample evaluated points to verify
        Debug.Log($"Sample points: Start={currentCurve.Evaluate(minTime):F3}, Mid={currentCurve.Evaluate((minTime + maxTime) * 0.5f):F3}, End={currentCurve.Evaluate(maxTime):F3}");
        
        painter.BeginPath();
        
        // Draw curve segments
        bool firstPoint = true;
        for (int i = 0; i <= CURVE_RESOLUTION; i++)
        {
            float t = minTime + (maxTime - minTime) * (i / (float)CURVE_RESOLUTION);
            float value = currentCurve.Evaluate(t);
            
            // Convert to screen coordinates with padding to prevent clipping
            float padding = 2f; // 2 pixels of padding
            float usableWidth = size.x - (padding * 2);
            float usableHeight = size.y - (padding * 2);
            
            float x = padding + ((t - minTime) / (maxTime - minTime) * usableWidth);
            float y = padding + (usableHeight - ((value - minValue) / (maxValue - minValue) * usableHeight));
            
            // Clamp to ensure we stay within bounds
            x = Mathf.Clamp(x, padding, size.x - padding);
            y = Mathf.Clamp(y, padding, size.y - padding);
            
            if (firstPoint)
            {
                painter.MoveTo(new Vector2(x, y));
                firstPoint = false;
            }
            else
            {
                painter.LineTo(new Vector2(x, y));
            }
        }
        
        painter.Stroke();
    }
}