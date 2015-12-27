using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ThirdPersonCharacter : MonoBehaviour
{
    //Exposed parameters
    public float WalkSpeed = 5.0f;
    public float RotationSpeed = 5.0f;

    //components
    private Rigidbody _rigidBody;
    private Animation _animation;

	// Use this for initialization
	void Start ()
	{
	    _rigidBody = GetComponent<Rigidbody>();
	    _rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

	    _animation = GetComponent<Animation>();

    }
	
	// Update is called once per frame
	void Update ()
	{
	    var speed = WalkSpeed;
        if(Input.GetKey(KeyCode.LeftShift))
	    {
	        speed *= 2;
	    }

        //Update movement
	    var forwardMovement = Input.GetAxis("Vertical") * speed;
        var sideMovement = Input.GetAxis("Horizontal") * speed;

        transform.position += transform.forward*forwardMovement*Time.deltaTime;
        transform.position += transform.right * sideMovement * Time.deltaTime;

        //Update rotation around Y if left mouse is down
        if (Input.GetMouseButton(1))
	    {
            var rotation = transform.localEulerAngles;
            rotation.y += Input.GetAxis("Mouse X") * RotationSpeed;
            transform.localEulerAngles = rotation;
	    }

        //update animation
	    if (Mathf.Abs(forwardMovement) > 0.1f)
	    {
	        _animation.Play("Walk");
	        _animation["Walk"].normalizedSpeed = forwardMovement/WalkSpeed;
	    }
	    else
	    {
            _animation.Play("Idle1");
        }

	}
}
