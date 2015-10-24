	using UnityEngine;
	using System.Collections;
	using System;
	
	//[ExecuteInEditMode]	
	public class Holo_overhead : MonoBehaviour {
		
		public enum Smoothing : int { Default, Medium, Aggressive }
		public Vector3 HeadPosition { get; set; }
		public Vector3 KinectOffset;
		public Smoothing smoothing = Smoothing.Default;
		public float ScreenWidth ;
		public float ScreenHeight;	
		public Matrix4x4 m { get; set; }
		public float ani_Xvalue { get; set; }
		public float ani_Yvalue { get; set; }
	    public float scaleFactor=1;
	
		private Transform target;
		private string w,h;
		private int mirror=-1;
		private bool track=false,show=false,left_mode=false;
		private GameObject cube;
		private KinectCoordinate kinect;
		
		// Use this for initialization
		void Start () {
			cube=GameObject.Find ("mainCube");
			//cube.renderer.enabled = false;
			ScreenWidth =ScreenWidth*scaleFactor;
			ScreenHeight =ScreenHeight*scaleFactor;
			w=ScreenWidth.ToString("####.000");
			h=ScreenHeight.ToString("####.000");

			HeadPosition = new Vector3(0,0,.5f);		
			
		    kinect=new KinectCoordinate();
	        GameObject temp = GameObject.Find ("Scripts");
	        kinect =temp.GetComponent<KinectCoordinate>();
		}
		
		// Update is called once per frame
		void Update () {
			if (track){

			HeadPosition = new Vector3(mirror*kinect.headPos4.x + KinectOffset.x, -1.6f+kinect.headPos4.z+KinectOffset.z, kinect.headPos4.y + KinectOffset.y);
			HeadPosition=  new Vector3 (HeadPosition.x*scaleFactor,HeadPosition.y*scaleFactor,HeadPosition.z*scaleFactor);
			}
			if (track&&left_mode){
			HeadPosition = new Vector3(mirror*kinect.headPos4.z + KinectOffset.z, kinect.headPos4.y + KinectOffset.y, kinect.headPos4.x + KinectOffset.x);
			HeadPosition=  new Vector3 (HeadPosition.x*scaleFactor,HeadPosition.y*scaleFactor,HeadPosition.z*scaleFactor);
			}
			
			if(Input.GetKeyDown("s"))
			{	
			if (show)
				{
					show=false;
	//				cube.renderer.enabled = false;
				}
				else
				{	
					show=true;
					Cursor.visible = true;
			//		cube.renderer.enabled = true;
				}
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
			if(Input.GetKeyDown("l"))
			{	
			if (left_mode)
				{
					left_mode=false;	
				}
				else
				{	
					left_mode=true;
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
		
		// Make sure to kill the Kinect on quitting.
		
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
			//cam.transform.eulerAngles = new Vector3(0,0,90f);		
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
		
		void OnGUI()
		{
		GUI.skin.box.fontSize = 20;
		//GUI.Box (new Rect(0, 0, 300, 50), headPos4.x.ToString("######.00"));		
		//x=GUI.TextField(new Rect(5,20,100,50),x,30)
		if (show)
		{	
		h=GUI.TextField(new Rect(5,260,100,50),h,30);  GUI.Box (new Rect(100, 260, 50, 50), "h");	
		w=GUI.TextField(new Rect(5,340,100,50),w,30);  GUI.Box (new Rect(100, 340, 50, 50), "w");	
		}
	
		 float.TryParse(w,out ScreenWidth );
		 float.TryParse(h,out ScreenHeight);
		}
	}
	
