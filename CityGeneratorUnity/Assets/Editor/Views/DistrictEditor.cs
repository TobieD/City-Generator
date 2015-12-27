using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using UnityEditor;

public class DistrictEditor
{
    public string ZoneType;
    private bool _foldout = false, _foldoutObjects = false;

    private List<GameObject> _buildingPrefabs = new List<GameObject>();
    private GameObject _selectedBuilding = null;
    private string _selectedBuildingLabel = "Building";
    private DistrictSettings _districtSettings;
    private EditorWindow _parentWindow;

    public DistrictEditor(string type, EditorWindow parentWindow)
    {
        _parentWindow = parentWindow;
        ZoneType = type;
        _districtSettings = new DistrictSettings(type);
    }

    public void DrawGUI(GUIStyle foldoutStyle)
    {
        _foldout = EditorGUILayout.Foldout(_foldout, ZoneType, foldoutStyle);

        if (!_foldout)
        {
            return;
        }


        //GUILayout.Label("Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        _districtSettings.Frequency = EditorGUILayout.IntSlider("Frequency", _districtSettings.Frequency, 0, 10);
        _districtSettings.Size = EditorGUILayout.Slider("Size", (float) _districtSettings.Size, 0.0f, 1.0f);

        _foldoutObjects = EditorGUILayout.Foldout(_foldoutObjects, "Prefabs", foldoutStyle);

        if (_foldoutObjects)
        {
            
            DistrictBuildingsGUI();
        }

        EditorGUI.indentLevel--;
       
        EditorGUI.indentLevel--;

    }

    private void DistrictBuildingsGUI()
    {
        var windowWidth = _parentWindow.maxSize.x;
        var spacing = windowWidth * 0.05f;
        var buttonWidth = windowWidth / 2 - spacing;

        //Selected Tree GUI
        EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
        GUILayout.Space(spacing / 2);
        _selectedBuilding = (GameObject)EditorGUILayout.ObjectField(_selectedBuilding, typeof(GameObject), false, GUILayout.Width(buttonWidth * 1.5f));
        EditorGUILayout.LabelField(_selectedBuildingLabel);
        GUILayout.Space(spacing / 2);
        EditorGUILayout.EndHorizontal();

        //Show Added Trees
        //texture previews of added textures
        int size = 64;

        GUILayout.BeginHorizontal();
        GUILayout.Space(spacing);

        //the splatmaps are limited to 4 + 1 road
        for (int i = 0; i < _buildingPrefabs.Count; ++i)
        {
            var building = _buildingPrefabs[i];
            GUILayout.BeginVertical();

            var prefabPreview = AssetPreview.GetAssetPreview(building);
            string label = "Building " + i; ;
            GUILayout.Label(label);
            if (GUILayout.Button(prefabPreview, GUIStyle.none, GUILayout.Width(size + (size * 0.2f)), GUILayout.Height(size)))
            {
                _selectedBuilding = building;
                _selectedBuildingLabel = label;
            }
            GUILayout.EndVertical();
        }
        GUILayout.Space(spacing);
        GUILayout.EndHorizontal();


        //Adding / Removing
        EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
        GUILayout.Space(spacing);
        if (GUILayout.Button("Add Building", GUILayout.Width(buttonWidth)))
        {
            if (!_buildingPrefabs.Contains(_selectedBuilding))
            {
               _buildingPrefabs.Add(_selectedBuilding);
            }
        }

        //remove the selected splatTexture
        if (GUILayout.Button("Remove Building", GUILayout.Width(buttonWidth)))
        {
            _buildingPrefabs.Remove(_selectedBuilding);
        }
        GUILayout.Space(spacing);
        EditorGUILayout.EndHorizontal();
    }

    public List<GameObject> GetActualPrefabs()
    {
        return _buildingPrefabs.Where(g => g != null).ToList();
    }

    public DistrictSettings GetSettings()
    {
        return _districtSettings;
    }

    public void AddPrefab(string path)
    {
        GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
       _buildingPrefabs.Add(obj);
    }

    public void ResetPrefabs()
    {
        _buildingPrefabs.Clear();
    }
}
