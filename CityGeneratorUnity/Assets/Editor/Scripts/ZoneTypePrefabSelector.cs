using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public class ZoneTypePrefabSelector
{
    public string ZoneType;
    private bool _foldout = false, _foldoutObjects = false;

    private List<GameObject> _buildingPrefabs = new List<GameObject>();
    private int _prefabAmount = 5;

    public ZoneTypePrefabSelector(string zonetype)
    {
        ZoneType = zonetype;
        _buildingPrefabs = Resize(_buildingPrefabs, _prefabAmount);
    }

    public void DrawGUI()
    {
        _foldout = EditorGUILayout.Foldout(_foldout, ZoneType);

        if (!_foldout)
        {
            return;
        }

        //define prefab amount
        GUI.SetNextControlName("Size");
        if (EditorGUILayout.IntField("Amount: ", _prefabAmount).KeyPressed("Size", KeyCode.Return, out _prefabAmount))
        {

            Mathf.Clamp(_prefabAmount, 0, _prefabAmount + 1);
            _buildingPrefabs = Resize(_buildingPrefabs, _prefabAmount);
        }

        _foldoutObjects = EditorGUILayout.Foldout(_foldoutObjects, "Prefabs");

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
}
