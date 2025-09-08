using UnityEngine;

[CreateAssetMenu(fileName = "New Animation Curve Preset", menuName = "Animation Curve Preset")]
public class AnimationCurvePreset : ScriptableObject
{
    [SerializeField]
    private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [SerializeField]
    private string description = "";

    public AnimationCurve Curve
    {
        get { return curve; }
        set { curve = value; }
    }

    public string Description
    {
        get { return description; }
        set { description = value; }
    }

    public void SetCurve(AnimationCurve newCurve)
    {
        curve = new AnimationCurve(newCurve.keys);
        for (int i = 0; i < curve.length; i++)
        {
            curve.keys[i] = newCurve.keys[i];
        }
    }

    public AnimationCurve GetCurveCopy()
    {
        return new AnimationCurve(curve.keys);
    }

#if UNITY_EDITOR
    public void SavePreset(AnimationCurve curveToSave, string presetDescription = "")
    {
        SetCurve(curveToSave);
        description = presetDescription;
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }
#endif
}