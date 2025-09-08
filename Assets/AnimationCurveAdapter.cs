using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AnimationCurveAdapter
{
    public static Path AnimationCurveToPath(AnimationCurve curve)
    {
        if (curve.length < 2)
        {
            return new Path(Vector2.zero);
        }

        Keyframe firstKey = curve[0];
        Vector2 startPos = new Vector2(firstKey.time, firstKey.value);
        
        // Create path and immediately replace the default points with our first keyframe
        Path path = new Path(Vector2.zero);
        path.AutoSetControlPoints = false;
        
        // Replace the default first anchor with our actual first keyframe
        path.MovePoint(0, startPos);
        
        // Only process the first segment if we have exactly 2 keyframes
        if (curve.length == 2)
        {
            Keyframe secondKey = curve[1];
            Vector2 endPos = new Vector2(secondKey.time, secondKey.value);
            
            float timeDelta = secondKey.time - firstKey.time;
            float tangentWeight = timeDelta * 0.2f;
            
            // Handle flat curves by checking if the line should be straight
            Vector2 control1, control2;
            bool shouldBeFlat = Mathf.Approximately(startPos.y, endPos.y) || 
                               (Mathf.Approximately(firstKey.outTangent, 0f) && Mathf.Approximately(secondKey.inTangent, 0f));
            
            if (shouldBeFlat)
            {
                // For flat curves, place control points on the straight line between anchors
                float t1 = 0.333f;
                float t2 = 0.667f;
                control1 = Vector2.Lerp(startPos, endPos, t1);
                control2 = Vector2.Lerp(startPos, endPos, t2);
                Debug.Log($"Creating flat controls: control1={control1}, control2={control2}");
            }
            else
            {
                // For curved lines, use tangent-based control points
                control1 = startPos + new Vector2(tangentWeight, firstKey.outTangent * tangentWeight);
                control2 = endPos + new Vector2(-tangentWeight, secondKey.inTangent * -tangentWeight);
                Debug.Log($"Creating curved controls from tangents {firstKey.outTangent}, {secondKey.inTangent}: control1={control1}, control2={control2}");
            }
            
            // Set control points directly to bypass Path's automatic control point logic
            SetPathPoint(path, 1, control1);
            SetPathPoint(path, 2, control2);
            path.MovePoint(3, endPos);
            
            // Force set again after anchor placement
            SetPathPoint(path, 1, control1);
            SetPathPoint(path, 2, control2);
        }
        else
        {
            // For curves with more than 2 keyframes, rebuild from scratch
            // Clear the default segment and build properly
            Keyframe secondKey = curve[1];
            Vector2 secondPos = new Vector2(secondKey.time, secondKey.value);
            
            float timeDelta = secondKey.time - firstKey.time;
            // Reduce tangent weight for smoother curves that match Unity's visual style
            float tangentWeight = timeDelta * 0.2f; // Reduced from 1/3 to 1/5 for smoother curves
            
            // Handle flat curves by checking if the line should be straight  
            Vector2 control1, control2;
            bool shouldBeFlat = Mathf.Approximately(startPos.y, secondPos.y) || 
                               (Mathf.Approximately(firstKey.outTangent, 0f) && Mathf.Approximately(secondKey.inTangent, 0f));
            
            if (shouldBeFlat)
            {
                // For flat curves, place control points on the straight line between anchors
                float t1 = 0.333f;
                float t2 = 0.667f;
                control1 = Vector2.Lerp(startPos, secondPos, t1);
                control2 = Vector2.Lerp(startPos, secondPos, t2);
            }
            else
            {
                // For curved lines, use tangent-based control points
                control1 = startPos + new Vector2(tangentWeight, firstKey.outTangent * tangentWeight);
                control2 = secondPos + new Vector2(-tangentWeight, secondKey.inTangent * -tangentWeight);
            }
            
            // Set control points directly to bypass Path's automatic control point logic
            SetPathPoint(path, 1, control1);
            SetPathPoint(path, 2, control2);
            path.MovePoint(3, secondPos);
            
            // Re-set control points after anchor placement to override any automatic adjustments
            SetPathPoint(path, 1, control1);
            SetPathPoint(path, 2, control2);
            
            // Add remaining segments
            for (int i = 2; i < curve.length; i++)
            {
                Keyframe prevKey = curve[i - 1];
                Keyframe currentKey = curve[i];
                
                Vector2 prevAnchor = new Vector2(prevKey.time, prevKey.value);
                Vector2 currentAnchor = new Vector2(currentKey.time, currentKey.value);
                
                float segmentTimeDelta = currentKey.time - prevKey.time;
                // Use reduced tangent weight for smoother curves
                float segmentTangentWeight = segmentTimeDelta * 0.2f;
                
                // Handle zero tangents for straight line segments
                Vector2 segmentControl1, segmentControl2;
                if (Mathf.Approximately(prevKey.outTangent, 0f) && Mathf.Approximately(currentKey.inTangent, 0f))
                {
                    // For flat segments, place control points on the straight line
                    float t1 = 0.333f;
                    float t2 = 0.667f;
                    segmentControl1 = Vector2.Lerp(prevAnchor, currentAnchor, t1);
                    segmentControl2 = Vector2.Lerp(prevAnchor, currentAnchor, t2);
                }
                else
                {
                    // For curved segments, use tangent-based control points
                    segmentControl1 = prevAnchor + new Vector2(segmentTangentWeight, prevKey.outTangent * segmentTangentWeight);
                    segmentControl2 = currentAnchor + new Vector2(-segmentTangentWeight, currentKey.inTangent * -segmentTangentWeight);
                }
                
                path.AddSegment(currentAnchor);
                
                int segmentIndex = path.NumSegments - 1;
                int controlIndex1 = segmentIndex * 3 + 1;
                int controlIndex2 = segmentIndex * 3 + 2;
                
                SetPathPoint(path, controlIndex1, segmentControl1);
                SetPathPoint(path, controlIndex2, segmentControl2);
            }
        }

        return path;
    }

    public static AnimationCurve PathToAnimationCurve(Path path)
    {
        if (path.NumSegments == 0)
        {
            return new AnimationCurve();
        }
        
        List<Keyframe> keyframes = new List<Keyframe>();
        
        // First keyframe from first anchor
        Vector2 firstAnchor = path[0];
        Keyframe firstKey = new Keyframe(firstAnchor.x, firstAnchor.y);
        
        // Calculate out tangent from first control point if exists
        if (path.NumPoints > 1)
        {
            Vector2 firstControl = path[1];
            float calculatedTangent = CalculateTangentFromControl(firstAnchor, firstControl);
            Debug.Log($"Calculated tangent for firstKey: {calculatedTangent}");
            
            // Create new keyframe with tangent values to ensure they're preserved
            firstKey = new Keyframe(firstAnchor.x, firstAnchor.y, 0f, calculatedTangent);
            Debug.Log($"Created firstKey with tangent - time={firstKey.time}, value={firstKey.value}, inTangent={firstKey.inTangent}, outTangent={firstKey.outTangent}");
        }
        keyframes.Add(firstKey);
        Debug.Log($"Added firstKey to keyframes: time={firstKey.time}, value={firstKey.value}, inTangent={firstKey.inTangent}, outTangent={firstKey.outTangent}");
        
        // Add keyframe for each segment end
        for (int segmentIndex = 0; segmentIndex < path.NumSegments; segmentIndex++)
        {
            Vector2[] segmentPoints = path.GetPointsInSegment(segmentIndex);
            Vector2 endAnchor = segmentPoints[3]; // End anchor of segment
            Vector2 inControl = segmentPoints[2]; // In control point
            
            // Calculate tangents first
            float inTangent = CalculateTangentFromControl(endAnchor, inControl);
            float outTangent = 0f;
            
            // Set out tangent if there's a next segment
            if (segmentIndex < path.NumSegments - 1)
            {
                Vector2[] nextSegmentPoints = path.GetPointsInSegment(segmentIndex + 1);
                Vector2 outControl = nextSegmentPoints[1]; // Out control point of next segment
                outTangent = CalculateTangentFromControl(endAnchor, outControl);
            }
            
            Debug.Log($"Segment {segmentIndex} keyframe tangents - in: {inTangent}, out: {outTangent}");
            
            // Create keyframe with tangents in constructor to ensure they're preserved
            Keyframe key = new Keyframe(endAnchor.x, endAnchor.y, inTangent, outTangent);
            Debug.Log($"Created segment keyframe - time={key.time}, value={key.value}, inTangent={key.inTangent}, outTangent={key.outTangent}");
            
            keyframes.Add(key);
        }
        
        AnimationCurve result = new AnimationCurve(keyframes.ToArray());
        
        Debug.Log($"Created AnimationCurve with {result.length} keyframes:");
        for (int i = 0; i < result.length; i++)
        {
            var key = result[i];
            Debug.Log($"  Keyframe {i} in result: time={key.time}, value={key.value}, inTangent={key.inTangent}, outTangent={key.outTangent}");
        }
        
#if UNITY_EDITOR
        // Set all keyframes to use free tangent mode for smooth editing
        for (int i = 0; i < result.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(result, i, AnimationUtility.TangentMode.Free);
            AnimationUtility.SetKeyRightTangentMode(result, i, AnimationUtility.TangentMode.Free);
        }
        
        Debug.Log($"After setting tangent modes:");
        for (int i = 0; i < result.length; i++)
        {
            var key = result[i];
            Debug.Log($"  Keyframe {i} after tangent modes: time={key.time}, value={key.value}, inTangent={key.inTangent}, outTangent={key.outTangent}");
        }
#endif
        
        return result;
    }
    
    private static float CalculateTangentFromControl(Vector2 anchor, Vector2 control)
    {
        Vector2 delta = control - anchor;
        Debug.Log($"CalculateTangentFromControl: anchor={anchor}, control={control}, delta={delta}");
        
        if (Mathf.Approximately(delta.x, 0f))
        {
            Debug.Log("Vertical tangent, returning 0");
            return 0f; // Vertical tangent becomes zero slope
        }
        
        float tangent = delta.y / delta.x;
        Debug.Log($"Calculated tangent: {tangent}");
        return tangent;
    }
    
    private static void SetPathPoint(Path path, int index, Vector2 position)
    {
        // Use reflection to directly access the private points list and bypass MovePoint logic
        var pointsField = typeof(Path).GetField("points", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var points = (System.Collections.Generic.List<Vector2>)pointsField.GetValue(path);
        points[index] = position;
    }
}