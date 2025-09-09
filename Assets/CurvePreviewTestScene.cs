using UnityEngine;
using System.Collections.Generic;

public class CurvePreviewTestScene : MonoBehaviour
{
    [SerializeField] private GameObject curvePreviewButtonPrefab;
    [SerializeField] private List<CurveTestData> testCurves = new List<CurveTestData>();
    
    private List<CurvePreviewButton> previewButtons = new List<CurvePreviewButton>();
    
    [System.Serializable]
    public class CurveTestData
    {
        public string name = "Test Curve";
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        public Color color = Color.cyan;
    }

    void Start()
    {
        CreateDefaultCurves();
        SetupPreviewButtons();
    }
    
    private void CreateDefaultCurves()
    {
        if (testCurves.Count == 0)
        {
            // Add some default test curves
            testCurves.AddRange(new CurveTestData[]
            {
                new CurveTestData 
                { 
                    name = "Linear", 
                    curve = AnimationCurve.Linear(0, 0, 1, 1),
                    color = Color.cyan
                },
                new CurveTestData 
                { 
                    name = "Ease In", 
                    curve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                    color = Color.green
                },
                new CurveTestData 
                { 
                    name = "Constant", 
                    curve = new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 0.5f)),
                    color = Color.yellow
                },
                new CurveTestData 
                { 
                    name = "S-Curve", 
                    curve = CreateSCurve(),
                    color = Color.magenta
                },
                new CurveTestData 
                { 
                    name = "Bounce", 
                    curve = CreateBounceCurve(),
                    color = Color.red
                }
            });
        }
    }
    
    private AnimationCurve CreateSCurve()
    {
        var curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f, 0f, 0f, 0f));
        curve.AddKey(new Keyframe(1f, 1f, 2f, 0f));
        return curve;
    }
    
    private AnimationCurve CreateBounceCurve()
    {
        var curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f, 0f));
        curve.AddKey(new Keyframe(0.3f, 0.7f));
        curve.AddKey(new Keyframe(0.6f, 0.4f));
        curve.AddKey(new Keyframe(1f, 1f));
        return curve;
    }
    
    private void SetupPreviewButtons()
    {
        // Calculate layout positions
        int buttonsPerRow = 3;
        float buttonSpacing = 150f;
        float rowSpacing = 100f;
        Vector2 startPosition = new Vector2(50f, 50f);
        
        for (int i = 0; i < testCurves.Count; i++)
        {
            var curveData = testCurves[i];
            
            // Create button GameObject
            GameObject buttonObj;
            if (curvePreviewButtonPrefab != null)
            {
                buttonObj = Instantiate(curvePreviewButtonPrefab, transform);
            }
            else
            {
                buttonObj = new GameObject($"CurvePreview_{curveData.name}");
                buttonObj.transform.SetParent(transform);
            }
            
            // Ensure the CurvePreviewButton component exists
            var previewButton = buttonObj.GetComponent<CurvePreviewButton>();
            if (previewButton == null)
            {
                previewButton = buttonObj.AddComponent<CurvePreviewButton>();
            }
            if (previewButton != null)
            {
                previewButton.Curve = curveData.curve;
                previewButton.CurveName = curveData.name;
                
                // Calculate position
                int row = i / buttonsPerRow;
                int col = i % buttonsPerRow;
                Vector2 position = startPosition + new Vector2(col * buttonSpacing, row * rowSpacing);
                
                // Set position after component is fully set up
                previewButton.SetPosition(position);
                
                Debug.Log($"Positioned {curveData.name} at {position}");
                
                // Set up custom callback (optional - demonstrates the callback system)
                previewButton.OnCurveEditRequested += OnCurveEditRequested;
                
                previewButtons.Add(previewButton);
            }
        }
        
        Debug.Log($"Created {previewButtons.Count} curve preview buttons");
    }
    
    private void OnCurveEditRequested(CurvePreviewButton button)
    {
        Debug.Log($"Edit requested for curve: {button.CurveName}");
        // The button will automatically use the shared editor as fallback
        // This callback is just for demonstration/logging
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, Screen.height - 100, 300, 100));
        
        GUILayout.Label("Production Curve Editor Demo");
        GUILayout.Label($"Preview Buttons: {previewButtons.Count}");
        GUILayout.Label("Click any curve preview to edit it");
        
        GUILayout.EndArea();
    }
}