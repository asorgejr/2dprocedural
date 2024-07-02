using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

using CU = UnityEngine.Splines.CurveUtility;

namespace ams.2d
{
[Serializable]
public class SplinePointCollection
{
    [SerializeField]
    public List<GameObject> Points = new List<GameObject>();
}

public enum InterpolationMode
{
    Knot,
    Delta
}

[ExecuteInEditMode]
public partial class SplineGenerator : MonoBehaviour
{
    [SerializeField]
    SplineContainer _splineContainer;
    
    [SerializeField, HideInInspector]
    GameObject spritePrefab;
    [SerializeField, HideInInspector]
    int count = 10;
    [SerializeField, HideInInspector]
    bool orientToPath = true;
    [SerializeField, HideInInspector]
    float startTangent = 0.01f;
    [SerializeField, HideInInspector]
    float endTangent = 0.99f;
    [SerializeField, HideInInspector]
    Vector3 preRotation = Vector3.up * 90;
    
    [SerializeField, HideInInspector]
    InterpolationMode interpolationMode = InterpolationMode.Delta;
    [SerializeField, HideInInspector]
    float derivativeDelta = 0.01f;
    
    [SerializeField, HideInInspector]
    List<SplinePointCollection> splineObjects = new List<SplinePointCollection>();
    
    [SerializeField, HideInInspector]
    internal List<SplineExtension> generators = new List<SplineExtension>();
    
    public List<SplinePointCollection> SplineObjects => splineObjects;
    
    public Tuple<int, float> GetCurveIndexAndT(Spline s, float t)
    {
        float l = s.GetLength();
        float d = t * l;
        float lpos = 0.0f;
        int ci = 0;
        float cl = 0.0f;
        for (int i = 0; i < s.Count; i++)
        {
            cl = s.GetCurveLength(i);
            if (lpos + cl > d)
            {
                ci = i;
                break;
            }
            lpos += cl;
        }
        float lt = ci == s.Count - 1
            ? 1.0f
            : (d - lpos) / cl;
        return new Tuple<int, float>(ci, lt);
    }

    public Vector3 InterpolatePosition(Spline s, float t)
    {
        float l = s.GetLength();
        float d = t * l;
        var (ci, ct) = GetCurveIndexAndT(s, t);
        return CU.EvaluatePosition(s.GetCurve(ci), ct);
    }
    
    public Quaternion InterpolateRotation(Spline s, float t, InterpolationMode mode = InterpolationMode.Knot)
    {
        float l = s.GetLength();
        var (ci, ct) = GetCurveIndexAndT(s, t);
        // look at next point
        if (ci < s.Count - 1)
        {
            var p0 = CU.EvaluatePosition(s.GetCurve(ci), ct);
            float3 p1 = mode switch {
                InterpolationMode.Delta => CU.EvaluatePosition(s.GetCurve(ci), ct + 0.01f),
                _ => CU.EvaluatePosition(s.GetCurve(ci + 1), 0.0f),
            };
            var n = Vector3.Normalize(p1 - p0);
            var q = Quaternion.LookRotation(n);
            Debug.Log($"p0: {p0}, p1: {p1}, n: {n}, q: {q}");
            return Quaternion.LookRotation(n);
        }
        // look from 0.99 to 1.0
        if (ci > 0)
        {
            var (ci1, ct1) = GetCurveIndexAndT(s, 0.99f);
            var p0 = CU.EvaluatePosition(s.GetCurve(ci1), ct1);
            var p1 = CU.EvaluatePosition(s.GetCurve(ci), ct);
            var n = Vector3.Normalize(p1 - p0);
            var q = Quaternion.LookRotation(n);
            Debug.Log($"p0: {p0}, p1: {p1}, n: {n}, q: {q}");
            return Quaternion.LookRotation(n);
        }
        return Quaternion.identity;
    }

    public Quaternion InterpolateRotationDerivative(Spline s, float t, float dt = 0.01f)
    {
        float l = s.GetLength();
        var (ci, ct) = GetCurveIndexAndT(s, t);
        var p0 = CU.EvaluatePosition(s.GetCurve(ci), ct);
        var p1 = CU.EvaluatePosition(s.GetCurve(ci), ct + dt);
        var n = Vector3.Normalize(p1 - p0);
        return Quaternion.LookRotation(n);
    }

    void Generate()
    {
        if (_splineContainer == null)
        {
            Debug.LogError("SplineContainer component not specified");
            return;
        }
        foreach (var splineObject in splineObjects)
        {
            foreach (var point in splineObject.Points)
            {
                DestroyImmediate(point);
            }
        }
        splineObjects.Clear();

        var splines = _splineContainer.Splines;
        Matrix4x4 pt = transform.localToWorldMatrix;
        for (int j = 0; j < splines.Count; j++)
        {
            var spline = splines[j];
            float l = spline.GetLength();
            var spc = new SplinePointCollection();
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Lerp(startTangent, endTangent, (float)i / (count - 1));
                Vector3 pos = InterpolatePosition(spline, t);
                pos = pt.MultiplyPoint3x4(pos);
                var go = Instantiate(spritePrefab, pos, Quaternion.identity);
                go.name = $"{gameObject.name}-Point-{j}-{i}";
                go.transform.SetParent(transform);
                spc.Points.Add(go);
                if (orientToPath)
                {
                    var pr = Quaternion.Euler(preRotation);
                    var rot = interpolationMode switch {
                        InterpolationMode.Knot => InterpolateRotation(spline, t),
                        InterpolationMode.Delta => InterpolateRotationDerivative(spline, t, derivativeDelta),
                        _ => Quaternion.identity,
                    };
                    go.transform.rotation = pt.rotation * rot * pr;
                }
            }
            splineObjects.Add(spc);
        }
        
        foreach (var generator in generators)
        {
            generator.Generate();
        }
    }
}

}