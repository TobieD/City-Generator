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

        GUILayout.Label("Settings", EditorStyles.boldLabel);


        GUILayout.Label("Frequency");
        _districtSettings.Frequency = EditorGUILayout.IntSlider(_districtSettings.Frequency, 0, 10);

        GUILayout.Label("Size");
        _districtSettings.Size = EditorGUILayout.Slider((float)_districtSettings.Size, 0.0f, 1.0f);


        //define prefab settings
        GUI.SetNextControlName("Size");
        GUILayout.Label("Prefab amount");
        if (EditorGUILayout.IntField(_prefabAmount).KeyPressed("Size", KeyCode.Return, out _prefabAmount))
        {
            Mathf.Clamp(_prefabAmount, 0, _prefabAmount + 1);
            _buildingPrefabs = Resize(_buildingPrefabs, _prefabAmount);
        }

        _foldoutObjects = EditorGUILayout.Foldout(_foldoutObjects, "Prefabs", foldoutStyle);

        if (!_foldoutObjects)
        {
            return;
        }

        //allow editing of prefabs
        for (int i = 0; i < _buildingPrefabs.Count; i++)
        {
            var prefab = _buildingPrefabs[i];
            string name = "Prefab " + (i + 1) + ":";
            _buildingPrefabs[i] = (GameObject) EditorGUILayout.ObjectField(name, prefab, typeof (GameObject), true);
        }

        
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
}
