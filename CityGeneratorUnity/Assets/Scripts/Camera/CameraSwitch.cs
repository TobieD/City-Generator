using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.ThirdPerson;

public class CameraSwitch : MonoBehaviour
{
    public Camera FlyCamera;
    public Camera ThirdPersonCamera;


    private FlyCam _flyControl;
    private ThirdPersonUserControl _thirdPersonControl;


	// Use this for initialization
	void Start ()
	{

	    if (FlyCamera == null || ThirdPersonCamera == null)
	    {
            Debug.Log("No Camera found");
            return;;
	    }


	    _flyControl = FlyCamera.GetComponent<FlyCam>();
	    if (_flyControl == null)
	    {
	        Debug.Log("No FlyCam script found on FlyCamera Gameobject");
	        return;
	    }

        _flyControl.isActive = true;
        FlyCamera.enabled = true;

        _thirdPersonControl = GameObject.FindWithTag("Player").GetComponent<ThirdPersonUserControl>();
        if (_thirdPersonControl == null)
        {
            Debug.Log("No ThirdPersonControl script found on ThirdPersonCamera Gameobject");
            return;
        }
	    _thirdPersonControl.isActive = false;
	    ThirdPersonCamera.enabled = false;
	    



	}
	
	// Update is called once per frame
	void Update ()
    {
	    if (_flyControl == null || _thirdPersonControl == null)
	    {
	        return;
	    }

            //Switch control mode
        if (Input.GetKeyDown(KeyCode.F1))
	    {
	        _flyControl.isActive = !_flyControl.isActive;
            _thirdPersonControl.isActive = !_thirdPersonControl.isActive;

            ThirdPersonCamera.enabled = !ThirdPersonCamera.enabled;
            FlyCamera.enabled = !FlyCamera.enabled;


        }
	}

   
}
