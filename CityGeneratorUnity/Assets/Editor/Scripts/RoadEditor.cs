

using CityGenerator;
using UnityEditor;
using UnityEngine;

public class RoadEditor
{

    public RoadSettings Settings;

    private bool _foldout;

    public RoadEditor(RoadSettings settings)
    {
        Settings = settings;
    }

    public void DrawGUI(GUIStyle foldoutStyle)
    {

        _foldout = EditorGUILayout.Foldout(_foldout, Settings.Type, foldoutStyle);

        if (!_foldout)
        {
            return;
        }

        EditorGUI.indentLevel++;
        Settings.Amount = EditorGUILayout.IntSlider("Amount",Settings.Amount, 0, 15);
        Settings.Branches = EditorGUILayout.IntSlider("Branches",Settings.Branches, 0, 10);
        Settings.Max = EditorGUILayout.IntSlider("Max",Settings.Max, 1, 75);
        EditorGUI.indentLevel--;



    }



}
