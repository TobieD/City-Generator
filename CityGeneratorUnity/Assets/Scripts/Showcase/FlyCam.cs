using UnityEngine;
using System.Collections;
using UnityEditor;

public class FlyCam : MonoBehaviour
{
    //Exposed Variables
    public float Speed = 1.0f; //standard speed
    public float MaxSpeed = 100.0f; //max speed when running
    public float MouseSensitivity = 0.05f; //mouse sensitivity

    private float _shiftAdd = 5.0f; //will be multiplied by how long shift is held
    private float _runTime = 0.0f; //duration shift is held

    public bool isActive = true;


    private Vector3 _lastMousePos = new Vector3(255,255,255); //centered to screen

	// Update is called once per frame
    private void FixedUpdate()
	{
        if (!isActive)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
            UnityEditor.EditorApplication.isPlaying = false;
        }

        //mouse input + camera rotation
        _lastMousePos = Input.mousePosition - _lastMousePos;
        _lastMousePos = new Vector3(-_lastMousePos.y * MouseSensitivity, _lastMousePos.x * MouseSensitivity, 0);
        _lastMousePos = new Vector3(transform.eulerAngles.x + _lastMousePos.x,
            transform.eulerAngles.y + _lastMousePos.y, 0);
        
        if (Input.GetMouseButton(1))
	    {
            transform.eulerAngles = _lastMousePos;
        }

        _lastMousePos = Input.mousePosition;


        //keyboard input
        var direction = GetDirection();

        //speed up
	    if (Input.GetKey(KeyCode.LeftShift))
	    {
	        _runTime += Time.deltaTime;

	        direction = direction*(_runTime*_shiftAdd);

            //clamp speed
	        direction.x = Mathf.Clamp(direction.x, -MaxSpeed, MaxSpeed);
            direction.y = Mathf.Clamp(direction.y, -MaxSpeed, MaxSpeed);
            direction.z = Mathf.Clamp(direction.z, -MaxSpeed, MaxSpeed);
        }
	    else
	    {
	        _runTime = Mathf.Clamp(_runTime*0.5f, 1.0f, 1000.0f);
	        direction = direction*Speed * Time.deltaTime;
	    }

        //move 
        transform.Translate(direction);


        //reset to default
	    if (Input.GetKey(KeyCode.C))
	    {
	        transform.Translate(Vector3.zero);
            transform.eulerAngles = Vector3.zero;
	    }

	}

    

    private Vector3 GetDirection()
    {
        var velocity = new Vector3();

        //forward
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W))
        {
            velocity += new Vector3(0, 0, 1);
        }

        //Backwards
        if (Input.GetKey(KeyCode.S))
        {
            velocity += new Vector3(0, 0, -1);
        }

        //left
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q))
        {
            velocity += new Vector3(-1, 0, 0);
        }

        //Right
        if (Input.GetKey(KeyCode.D))
        {
            velocity += new Vector3(1, 0, 0);
        }

        //Up
        if (Input.GetKey(KeyCode.R))
        {
            velocity += new Vector3(0, 1, 0);
        }

        //Down
        if (Input.GetKey(KeyCode.F))
        {
            velocity += new Vector3(0, -1, 0);
        }


        return velocity;
    }

   }
