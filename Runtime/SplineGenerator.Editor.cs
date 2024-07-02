using UnityEditor;
using UnityEngine;

namespace ams
{
public partial class SplineGenerator
{
#if UNITY_EDITOR
    [CustomEditor(typeof(SplineGenerator))]
    public class SplineGeneratorEditor : UnityEditor.Editor
    {

        bool _showOrientation = false;


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            var sg = target as SplineGenerator;
            if (sg == null)
            {
                return;
            }

            sg.spritePrefab = EditorGUILayout.ObjectField("Sprite Prefab", sg.spritePrefab,
                                                          typeof(GameObject), false) as GameObject;
            sg.count = Mathf.Max(EditorGUILayout.IntField("Count", sg.count), 1);
            sg.orientToPath = EditorGUILayout.Toggle("Orient to Path", sg.orientToPath);
            if (sg.orientToPath)
            { // Orientation Drop Down Parameters
                if (_showOrientation = EditorGUILayout.Foldout(_showOrientation, "Orientation"))
                {
                    EditorGUI.indentLevel++;
                    sg.preRotation = EditorGUILayout.Vector3Field("Pre Rotation", sg.preRotation);
                    sg.interpolationMode = (InterpolationMode)EditorGUILayout.EnumPopup("Interpolation Mode",
                                                                                        sg.interpolationMode);
                    if (sg.interpolationMode == InterpolationMode.Delta)
                    {
                        sg.derivativeDelta = EditorGUILayout.Slider("Derivative Delta",
                                                                    sg.derivativeDelta,
                                                                    0.0001f, 1.0f);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            sg.startTangent = EditorGUILayout.Slider("Start", sg.startTangent, 0.0f, 1.0f);
            sg.endTangent = EditorGUILayout.Slider("End", sg.endTangent, 0.0f, 1.0f);

            if (GUILayout.Button("Generate"))
            {
                sg.Generate();
            }
        }
    }
#endif
}
}