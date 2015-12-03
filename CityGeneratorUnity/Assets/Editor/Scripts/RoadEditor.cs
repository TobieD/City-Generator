

using CityGenerator;
using UnityEditor;
using UnityEngine;

public class RoadEditor
{

    public RoadPrefabs RoadPrefabs;
    public RoadSettings Settings;

    public RoadEditor(RoadSettings settings)
    {
        Settings = settings;

        RoadPrefabs = new RoadPrefabs();
    }

    public void DrawGUI(GUIStyle foldoutStyle)
    {
        Settings.Amount = EditorGUILayout.IntSlider("Amount",Settings.Amount, 0, 15);
        Settings.Branches = EditorGUILayout.IntSlider("Branches",Settings.Branches, 0, 10);
        Settings.Max = EditorGUILayout.IntSlider("Max",Settings.Max, 1, 75);
        Settings.Width = EditorGUILayout.IntSlider("Width", Settings.Width, 1, 20);

        RoadPrefabs.Straight = (GameObject)EditorGUILayout.ObjectField("Road Straight", RoadPrefabs.Straight, typeof(GameObject), false);
        
    }



}
