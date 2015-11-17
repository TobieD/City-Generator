using UnityEngine;
using System.Linq;
using UnityEditor;

[CustomEditor(typeof(TownZone))]
public class ZoneEdit : Editor
{
    private TownZone _townZone;

    private int _selectedIndex = 0;
    string[] districtTypes;

    public override void OnInspectorGUI()
    {
        //access script
        _townZone = (TownZone) target;

        //Access all possible district types

        var townGen = TownGenerator.GetInstance();

        if (townGen.PrefabsPerZone == null)
        {
            EditorGUILayout.LabelField("No prefabs set!\nPlease build a city first.");

            return;
        }

        //district selection
        districtTypes = townGen.PrefabsPerZone.Keys.ToArray();
        _selectedIndex = EditorGUILayout.Popup(_selectedIndex, districtTypes);

        //Show bounds visualization
        _townZone.bDrawBounds = EditorGUILayout.Toggle("Show Bounds", _townZone.bDrawBounds);
        
        //change settings
       if(GUILayout.Button("Rebuild"))
       {
            _townZone.SetZoneType(districtTypes[_selectedIndex]);

       }



    }
}
