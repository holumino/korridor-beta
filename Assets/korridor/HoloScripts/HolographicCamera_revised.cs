	using UnityEngine;
	using System.Collections;
	using System;
	
	//[ExecuteInEditMode]	
	public class HolographicCamera_revised : MonoBehaviour {
		
		public Vector3 HeadPosition { get; set; }
		public float ScreenWidth ;
		public float ScreenHeight;
		public Matrix4x4 m { get; set; }

		public KinectCoordinate kinect;
	
		private int mirror=-1;
		private bool track=false,show=false;

		void Start () {
	
			HeadPosition = new Vector3(0,0,.5f);		
	
		}
		
		void Update () {
		 
			if (track){
			HeadPosition = new Vector3(mirror*kinect.headPos4.x, kinect.headPos4.y, kinect.headPos4.z );
		
			}
			
			 if(Input.GetKeyDown("v"))
			{	
			if (track)
				{
					track=false;
					HeadPosition = new Vector3(0,0,.5f);	
					Cursor.visible = true;
				}
				else
				{	
					track=true;
					Cursor.visible = false;
				}
			}
			
			if(Input.GetKeyDown("m"))
			{	
			if (mirror==-1)
				{
					mirror=1;	
				}
				else
				{	
					mirror=-1;
				}
			}

		}
		
		private float left = -0.2F;
	    private float right = 0.2F;
	    private float top = 0.2F;
	    private float bottom = -0.2F;
	    
		void LateUpdate() {
	        Camera cam = GetComponent<Camera>();
			left = cam.nearClipPlane * (-(ScreenWidth/2) - HeadPosition.x) / HeadPosition.z;
	        right = cam.nearClipPlane * (ScreenWidth/2 - HeadPosition.x) / HeadPosition.z;
	        bottom = cam.nearClipPlane * (-(ScreenHeight/2) - HeadPosition.y) / HeadPosition.z;
	        top = cam.nearClipPlane * (ScreenHeight/2 - HeadPosition.y) / HeadPosition.z;
			cam.transform.position = new Vector3(-HeadPosition.x, HeadPosition.y, HeadPosition.z);
			
			
			cam.transform.LookAt(new Vector3(-HeadPosition.x, HeadPosition.y, 0f)); // control 0 position
			//cam.transform.LookAt (new Vector3(-target.position.x,-target.position.y,0));
	        m = PerspectiveOffCenter(left, right, bottom, top, cam.nearClipPlane, cam.farClipPlane);
	        cam.projectionMatrix = m;
			//cam.transform.eulerAngles = new Vector3(camera_rotation.x,camera_rotation.y,camera_rotation.z);		
	    }
		
	    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far) {
	        float x = 2.0F * near / (right - left);
	        float y = 2.0F * near / (top - bottom);
	        float a = (right + left) / (right - left);
	        float b = (top + bottom) / (top - bottom);
	        float c = -(far + near) / (far - near);
	        float d = -(2.0F * far * near) / (far - near);
	        float e = -1.0F;
	        Matrix4x4 m = new Matrix4x4();
	        m[0, 0] = x;
	        m[0, 1] = 0;
	        m[0, 2] = a;
	        m[0, 3] = 0;
	        m[1, 0] = 0;
	        m[1, 1] = y;
	        m[1, 2] = b;
	        m[1, 3] = 0;
	        m[2, 0] = 0;
	        m[2, 1] = 0;
	        m[2, 2] = c;
	        m[2, 3] = d;
	        m[3, 0] = 0;
	        m[3, 1] = 0;
	        m[3, 2] = e;
	        m[3, 3] = 0;
	        return m;
	    }
		

	}
	
