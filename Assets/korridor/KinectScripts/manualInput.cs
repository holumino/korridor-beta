using UnityEngine;
using System.Collections;
using System;

public class manualInput : MonoBehaviour {

	public Vector3 headPos4 { get; set; }
	public float move_factor=0.01f;
	public Vector3 offset;

	private Vector3 manual_pos;
	
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {	
		
		headPos4=new Vector3(manual_pos.x+offset.x,manual_pos.y+offset.y,manual_pos.z+offset.z);
		
		if(Input.GetKey(KeyCode.LeftArrow))
		{
			manual_pos=new Vector3 (manual_pos.x+move_factor,manual_pos.y,manual_pos.z);
			
		}
		if(Input.GetKey(KeyCode.RightArrow))
		{
			manual_pos=new Vector3 (manual_pos.x-move_factor,manual_pos.y,manual_pos.z);
		}
		
		if(Input.GetKey(KeyCode.UpArrow))
		{
			manual_pos=new Vector3 (manual_pos.x,manual_pos.y+move_factor,manual_pos.z);
		}
		
		if(Input.GetKey(KeyCode.DownArrow))
		{
			manual_pos=new Vector3 (manual_pos.x,manual_pos.y-move_factor,manual_pos.z);
		}
		
		if(Input.GetKey(KeyCode.LeftShift))
		{
			manual_pos=new Vector3 (manual_pos.x,manual_pos.y,manual_pos.z+move_factor);
		}	
		
		if(Input.GetKey(KeyCode.RightShift))
		{
			manual_pos=new Vector3 (manual_pos.x,manual_pos.y,manual_pos.z-move_factor);
		}	
		
	}
	
}
