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
            
            // Calculate the expected tangent for a straight line between the two points
            float straightLineTangent = (endPos.y - startPos.y) / (endPos.x - startPos.x);
            
            bool shouldBeFlat = Mathf.Approximately(startPos.y, endPos.y) || 
                               (Mathf.Approximately(firstKey.outTangent, 0f) && Mathf.Approximately(secondKey.inTangent, 0f)) ||
                               (Mathf.Abs(firstKey.outTangent - straightLineTangent) < 0.01f && Mathf.Abs(secondKey.inTangent - straightLineTangent) < 0.01f);
            
            Debug.Log($"Straight line check: startPos={startPos}, endPos={endPos}, straightLineTangent={straightLineTangent}");
            Debug.Log($"Tangents: out={firstKey.outTangent}, in={secondKey.inTangent}, shouldBeFlat={shouldBeFlat}");
            
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
                // For curved lines, use tangent-based control points with stored distances
                string outKey = $"out_{startPos.x}_{startPos.y}";
                string inKey = $"in_{endPos.x}_{endPos.y}";
                
                float storedOutDistance = controlPointDistances.ContainsKey(outKey) ? controlPointDistances[outKey] : tangentWeight;
                float storedInDistance = controlPointDistances.ContainsKey(inKey) ? Mathf.Abs(controlPointDistances[inKey]) : tangentWeight;
                
                Debug.Log($"Converting tangent {firstKey.outTangent} with stored distance {storedOutDistance}");
                control1 = CalculateControlFromTangentWithDistance(startPos, firstKey.outTangent, storedOutDistance);
                Debug.Log($"Converting tangent {secondKey.inTangent} with stored distance {storedInDistance}");
                control2 = CalculateControlFromTangentWithDistance(endPos, -secondKey.inTangent, -storedInDistance);
                
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
            
            // Calculate the expected tangent for a straight line between the two points
            float straightLineTangent = (secondPos.y - startPos.y) / (secondPos.x - startPos.x);
            
            bool shouldBeFlat = Mathf.Approximately(startPos.y, secondPos.y) || 
                               (Mathf.Approximately(firstKey.outTangent, 0f) && Mathf.Approximately(secondKey.inTangent, 0f)) ||
                               (Mathf.Abs(firstKey.outTangent - straightLineTangent) < 0.01f && Mathf.Abs(secondKey.inTangent - straightLineTangent) < 0.01f);
            
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
                
                // Calculate expected tangent for straight line segment
                float segmentStraightTangent = (currentAnchor.y - prevAnchor.y) / (currentAnchor.x - prevAnchor.x);
                
                bool isSegmentFlat = Mathf.Approximately(prevKey.outTangent, 0f) && Mathf.Approximately(currentKey.inTangent, 0f) ||
                                    (Mathf.Abs(prevKey.outTangent - segmentStraightTangent) < 0.01f && Mathf.Abs(currentKey.inTangent - segmentStraightTangent) < 0.01f);
                
                if (isSegmentFlat)
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

    // Static storage for control point distances to maintain precision during roundtrip conversion
    private static Dictionary<string, float> controlPointDistances = new Dictionary<string, float>();
    
    public static AnimationCurve PathToAnimationCurve(Path path)
    {
        if (path.NumSegments == 0)
        {
            return new AnimationCurve();
        }
        
        // Clear previous distance data
        controlPointDistances.Clear();
        
        List<Keyframe> keyframes = new List<Keyframe>();
        
        // First keyframe from first anchor
        Vector2 firstAnchor = path[0];
        Keyframe firstKey = new Keyframe(firstAnchor.x, firstAnchor.y);
        
        // Calculate out tangent from first control point if exists
        if (path.NumPoints > 1)
        {
            Vector2 firstControl = path[1];
            float distance;
            float calculatedTangent = CalculateTangentFromControl(firstAnchor, firstControl, out distance);
            Debug.Log($"Calculated tangent for firstKey: {calculatedTangent}, distance: {distance}");
            
            // Store the actual distance for later reconstruction
            string key = $"out_{firstKey.time}_{firstKey.value}";
            controlPointDistances[key] = distance;
            
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
            float inDistance, outDistance = 0f;
            float inTangent = CalculateTangentFromControl(endAnchor, inControl, out inDistance);
            float outTangent = 0f;
            
            // Set out tangent if there's a next segment
            if (segmentIndex < path.NumSegments - 1)
            {
                Vector2[] nextSegmentPoints = path.GetPointsInSegment(segmentIndex + 1);
                Vector2 outControl = nextSegmentPoints[1]; // Out control point of next segment
                outTangent = CalculateTangentFromControl(endAnchor, outControl, out outDistance);
            }
            
            Debug.Log($"Segment {segmentIndex} keyframe tangents - in: {inTangent} (dist: {inDistance}), out: {outTangent} (dist: {outDistance})");
            
            // Store the actual distances for later reconstruction
            string inKey = $"in_{endAnchor.x}_{endAnchor.y}";
            string outKey = $"out_{endAnchor.x}_{endAnchor.y}";
            controlPointDistances[inKey] = inDistance;
            if (segmentIndex < path.NumSegments - 1)
            {
                controlPointDistances[outKey] = outDistance;
            }
            
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
        // Don't set tangent modes as they override our manually calculated tangent values
        // The tangent values we calculated should be preserved as-is
        Debug.Log($"Skipping tangent mode setting to preserve manual tangent values");
#endif
        
        return result;
    }
    
    private static float CalculateTangentFromControl(Vector2 anchor, Vector2 control, out float distance)
    {
        Vector2 delta = control - anchor;
        distance = delta.x; // Store the X distance for later reconstruction
        Debug.Log($"CalculateTangentFromControl: anchor={anchor}, control={control}, delta={delta}, distance={distance}");
        
        if (Mathf.Approximately(delta.x, 0f))
        {
            distance = 0f;
            Debug.Log("Vertical tangent, returning 0");
            return 0f; // Vertical tangent becomes zero slope
        }
        
        float tangent = delta.y / delta.x;
        Debug.Log($"Calculated tangent: {tangent}");
        return tangent;
    }
    
    // Overload for backward compatibility
    private static float CalculateTangentFromControl(Vector2 anchor, Vector2 control)
    {
        float distance;
        return CalculateTangentFromControl(anchor, control, out distance);
    }
    
    private static Vector2 CalculateControlFromTangentWithDistance(Vector2 anchor, float tangent, float exactDistance)
    {
        // Use the exact stored distance to recreate the original control point position
        Vector2 result = anchor + new Vector2(exactDistance, tangent * exactDistance);
        Debug.Log($"  Recreated control point: {result} (exact distance: {exactDistance})");
        return result;
    }
    
    private static Vector2 CalculateControlFromTangent(Vector2 anchor, float tangent, float suggestedDistance, float maxDistance = 0.5f)
    {
        // Fallback method when no stored distance is available
        Debug.Log($"CalculateControlFromTangent: anchor={anchor}, tangent={tangent}, suggestedDistance={suggestedDistance}");
        
        // Use a more reasonable distance estimation
        float targetDistance = suggestedDistance;
        
        // Still limit the maximum distance to keep things reasonable
        targetDistance = Mathf.Min(targetDistance, maxDistance);
        
        Vector2 result = anchor + new Vector2(targetDistance, tangent * targetDistance);
        Debug.Log($"  Final control point: {result} (used distance: {targetDistance})");
        return result;
    }
    
    private static void SetPathPoint(Path path, int index, Vector2 position)
    {
        // Use reflection to directly access the private points list and bypass MovePoint logic
        var pointsField = typeof(Path).GetField("points", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var points = (System.Collections.Generic.List<Vector2>)pointsField.GetValue(path);
        points[index] = position;
    }
}