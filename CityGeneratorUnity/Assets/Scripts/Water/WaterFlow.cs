// Water Flow FREE version: 1.0.2
// Author: Gold Experience Team (http://www.ge-team.com/)
// Support: geteamdev@gmail.com
// Please direct any bugs/comments/suggestions to geteamdev@gmail.com

#region Namespaces

using UnityEngine;
using System.Collections;

#endregion

/***************
* WaterFlow class
* This class animates UV offset
**************/

public class WaterFlow : MonoBehaviour {
	
	#region Variables
	
		// UV speed
	public float m_SpeedU = 0.1f;
	public float m_SpeedV = -0.1f;
	
	#endregion
	
	// ######################################################################
	// MonoBehaviour Functions
	// ######################################################################
	
	#region Component Segments
	
	// Update is called once per frame
	void Update () {

		// Update new UV speed
		float newOffsetU = Time.time * m_SpeedU;
		float newOffsetV = Time.time * m_SpeedV;
		
		// Check if there is renderer component
		if (this.GetComponent<Renderer>())
		{
			// Update main texture offset
			GetComponent<Renderer>().material.mainTextureOffset = new Vector2(newOffsetU, newOffsetV);
		}
	}
	
	#endregion {Component Segments}
}