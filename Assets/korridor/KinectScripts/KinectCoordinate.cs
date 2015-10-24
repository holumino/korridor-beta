using UnityEngine;
using System.Collections;
using System;

public class KinectCoordinate : MonoBehaviour {

	public enum Mode : int { leftRight,frontBack }
	public Mode mode=Mode.frontBack;
	public enum Smoothing : int { Default, Medium, Aggressive }
	public Smoothing smoothing = Smoothing.Default;

	public Vector3 headPos4 { get; set; }

	public Vector3 KinectOffset;

	public KinectManager _KinectManager;
	
	private KinectWrapper.NuiSkeletonFrame skeletonFrame;
	private KinectWrapper.NuiTransformSmoothParameters smoothParameters;
	private int trackedJoint = (int)KinectWrapper.SkeletonJoint.HEAD;
	private bool KinectInitialized = false;
	private bool manual=false;
	private Vector3 manual_pos;
	
	void Start () {
//		int hr = 0;
//		
//		try
//		{
//			hr = KinectWrapper.NuiInitialize(KinectWrapper.NuiInitializeFlags.UsesSkeleton);
//			if (hr != 0)
//			{
//				throw new Exception("NuiInitialize Failed");
//			}
//			
//			hr = KinectWrapper.NuiSkeletonTrackingEnable(IntPtr.Zero, 8);  // 0, 12,8
//			if (hr != 0)
//			{
//				throw new Exception("Cannot initialize Skeleton Data");
//			}
//			
//			// init skeleton structures
//			skeletonFrame = new KinectWrapper.NuiSkeletonFrame() 
//			{ 
//				SkeletonData = new KinectWrapper.NuiSkeletonData[KinectWrapper.Constants.NuiSkeletonCount] 
//			};
//			
//			// values used to pass to smoothing function
//			smoothParameters = new KinectWrapper.NuiTransformSmoothParameters();
//			
//			switch(smoothing)
//			{
//			case Smoothing.Default:
//				smoothParameters.fSmoothing = 0.5f;
//				smoothParameters.fCorrection = 0.5f;
//				smoothParameters.fPrediction = 0.5f;
//				smoothParameters.fJitterRadius = 0.05f;
//				smoothParameters.fMaxDeviationRadius = 0.04f;
//				break;
//			case Smoothing.Medium:
//				smoothParameters.fSmoothing = 0.5f;
//				smoothParameters.fCorrection = 0.1f;
//				smoothParameters.fPrediction = 0.5f;
//				smoothParameters.fJitterRadius = 0.1f;
//				smoothParameters.fMaxDeviationRadius = 0.1f;
//				break;
//			case Smoothing.Aggressive:
//				smoothParameters.fSmoothing = 0.7f;
//				smoothParameters.fCorrection = 0.3f;
//				smoothParameters.fPrediction = 1.0f;
//				smoothParameters.fJitterRadius = 1.0f;
//				smoothParameters.fMaxDeviationRadius = 1.0f;
//				break;
//			}
//			
//		}
//		catch (Exception e)
//		{
//			Debug.LogError(e.Message + " - " + KinectWrapper.GetNuiErrorString(hr));
//			return;
//		}
//		
//		
//		Debug.Log("Waiting for users.");
//		
//		KinectInitialized = true;
	}
	
	// Update is called once per frame
	void Update () {	

			uint id = _KinectManager.GetPlayer1ID ();
			int index = (int)KinectWrapper.NuiSkeletonPositionIndex.Head;
			headPos4 = _KinectManager.GetJointPosition (id, index);
			Debug.Log ("id "+id);

//			if(KinectWrapper.PollSkeleton(ref smoothParameters, ref skeletonFrame))
//			{
//				for (int i = 0; i < KinectWrapper.Constants.NuiSkeletonCount; i++)
//				{
//					KinectWrapper.NuiSkeletonData skeletonData = skeletonFrame.SkeletonData[i];
//					
//					if (skeletonData.eTrackingState == KinectWrapper.NuiSkeletonTrackingState.SkeletonTracked)
//					{
//						if (skeletonData.eSkeletonPositionTrackingState[trackedJoint] == KinectWrapper.NuiSkeletonPositionTrackingState.Tracked)
//						{
//							headPos4 = skeletonData.SkeletonPositions[trackedJoint];
//						}
//					}
//				}
//			}

		switch(mode)
		{
			case Mode.leftRight:
			headPos4 = new Vector3(headPos4.z+KinectOffset.z, headPos4.y+KinectOffset.y, headPos4.x+KinectOffset.x);
				break;

			case Mode.frontBack:
			headPos4 = new Vector3(headPos4.x+KinectOffset.x, headPos4.y+KinectOffset.y, headPos4.z+KinectOffset.z);
				break;
		}


	}
	
	void OnApplicationQuit()
	{
		if(KinectInitialized)
		{
			// Shutdown OpenNI
			KinectWrapper.NuiShutdown();			
		}
	}
}
