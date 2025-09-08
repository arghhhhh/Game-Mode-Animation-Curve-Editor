using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathPlacer : MonoBehaviour {

    public float spacing = .1f;
    public float resolution = 1;

	
	void Start () {
        PathCreator pathCreator = FindFirstObjectByType<PathCreator>();
        if (pathCreator != null && pathCreator.path != null)
        {
            Vector2[] points = pathCreator.path.CalculateEvenlySpacedPoints(spacing, resolution);
            foreach (Vector2 p in points)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.transform.position = p;
                g.transform.localScale = Vector3.one * spacing * .5f;
            }
        }
        else
        {
            Debug.LogWarning("PathPlacer: No PathCreator found or path is null. Make sure to create a path first.");
        }
	}
	
	
}
