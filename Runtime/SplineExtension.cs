using System;
using UnityEngine;

namespace ams.2d
{
[RequireComponent(typeof(SplineGenerator))]
public class SplineExtension : MonoBehaviour
{
    public virtual void Generate() {}

    public virtual void OnEnable()
    {
        var sg = GetComponent<SplineGenerator>();
        if (sg == null)
        {
            Debug.LogError("SplineGenerator component not found");
            return;
        }
        if (!sg.generators.Contains(this)) sg.generators.Add(this);
    }
    
    public virtual void OnDisable()
    {
        var sg = GetComponent<SplineGenerator>();
        if (sg == null)
        {
            Debug.LogError("SplineGenerator component not found");
            return;
        }
        if (sg.generators.Contains(this)) sg.generators.Remove(this);
    }
}

}