using System;
using UnityEngine;
using System.Collections;

public class Singleton<T>
{
    private static T _instance = default(T);

    public static T GetInstance()
    {
        if (_instance == null)
        {
            _instance = (T)Activator.CreateInstance(typeof(T), new object[] { });
        }

        return _instance;
    }
}
