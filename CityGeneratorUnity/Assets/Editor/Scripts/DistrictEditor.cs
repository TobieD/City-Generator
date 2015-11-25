using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using UnityEditor;

public class DistrictEditor
{
    public string ZoneType;
    private bool _foldout = false, _foldoutObjects = false;

    private List<GameObject> _buildingPrefabs = new List<GameObject>();
    private int _prefabAmount = 5;
    private DistrictSettings _districtSettings;

    public DistrictEditor(string type)
    {
        ZoneType = type;
        _buildingPrefabs = Resize(_buildingPrefabs, _prefabAmount);
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

        _districtSettings.Frequency = EditorGUILayout.IntSlider("Frequency",_districtSettings.Frequency, 0, 10);
        _districtSettings.Size = EditorGUILayout.Slider("Size",(float)_districtSettings.Size, 0.0f, 1.0f);
        _districtSettings.Offset = EditorGUILayout.IntSlider("Building Offset", _districtSettings.Offset, 5, 15);

        _districtSettings.Percentage = EditorGUILayout.IntSlider("Building Interval", (int)_districtSettings.Percentage, 2, 75);


        //define prefab settings
        GUI.SetNextControlName("Size");
        if (EditorGUILayout.IntField("Prefab Size",_prefabAmount).KeyPressed("Size", KeyCode.Return, out _prefabAmount))
        {
            Mathf.Clamp(_prefabAmount, 0, _prefabAmount + 1);
            _buildingPrefabs = Resize(_buildingPrefabs, _prefabAmount);
        }

        _foldoutObjects = EditorGUILayout.Foldout(_foldoutObjects, "Prefabs", foldoutStyle);

        if (!_foldoutObjects)
        {
            EditorGUI.indentLevel--;
            return;
        }

        //allow editing of prefabs
        for (int i = 0; i < _buildingPrefabs.Count; i++)
        {
            EditorGUI.indentLevel++;
            var prefab = _buildingPrefabs[i];
            string name = "Prefab " + (i + 1) + ":";
            _buildingPrefabs[i] = (GameObject) EditorGUILayout.ObjectField(name, prefab, typeof (GameObject), true);
            EditorGUI.indentLevel--;
        }
        EditorGUI.indentLevel--;

    }


    private List<GameObject> Resize(List<GameObject> list, int newSize)
    {
        var newList = new List<GameObject>();
        
        for (int i = 0; i < newSize; i++)
        {
            GameObject go = null;
            if (i < list.Count)
            {
                go = list[i];
            }

            newList.Add(go);
        }

        return newList;



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
