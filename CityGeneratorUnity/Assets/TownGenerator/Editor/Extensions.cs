    using UnityEngine;
using System.Collections;

public static class Extensions
{
    public static bool KeyPressed<T>(this T s, string controlName, KeyCode key, out T fieldValue)
    {
        fieldValue = s;
        if (GUI.GetNameOfFocusedControl() == controlName)
        {
            if ((Event.current.type == EventType.KeyUp) && (Event.current.keyCode == key))
            {
                return true;
            }
        }

        return false;

    }
}
