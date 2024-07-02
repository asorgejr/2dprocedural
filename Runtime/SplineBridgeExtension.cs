using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ams.2d
{
[ExecuteInEditMode]
public class SplineBridgeExtension : SplineExtension
{
    public float inertia = 1.0f;
    public float density = 1.0f;
    public float angularDrag = 2.0f;
    public float limitMin = -45;
    public float limitMax = 45;


    void MakeBridgePhysics(List<GameObject> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var go = points[i];
            if (go == null)
            {
                Debug.LogError("A SplinePointCollection Point is null");
                continue;
            }

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody2D>();
            }

            rb.useAutoMass = true;
            rb.inertia = inertia;
            rb.angularDrag = angularDrag;
            var c = go.GetComponent<Collider2D>();
            if (c == null)
            {
                Debug.LogWarning($"No Collider2D found on '{go.name}'");
                c = go.AddComponent<BoxCollider2D>();
            }

            if (rb.useAutoMass)
            {
                c.density = density;
            }

            if (i == 0 || i == points.Count - 1)
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints2D.FreezePosition;
            }
            else
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints2D.None;
            }

            if (i == 0)
            {
                continue;
            }

            var j = go.GetComponent<HingeJoint2D>();
            if (j == null)
            {
                j = go.AddComponent<HingeJoint2D>();
            }

            j.connectedBody = points[i - 1].GetComponent<Rigidbody2D>();
            j.autoConfigureConnectedAnchor = true;
            j.useLimits = true;
            JointAngleLimits2D limits = j.limits;
            limits.min = limitMin;
            limits.max = limitMax;
            j.limits = limits;
        }
    }


    [ContextMenu("Generate")]
    public override void Generate()
    {
        base.Generate();
        var sg = GetComponent<SplineGenerator>();
        if (sg == null)
        {
            Debug.LogError("SplineGenerator component not found");
            return;
        }

        var splineObjects = sg.SplineObjects;
        foreach (var splineObject in splineObjects)
        {
            MakeBridgePhysics(splineObject.Points);
        }
    }
}
}