using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;


public class KinectManager : MonoBehaviour
{
	public enum Smoothing : int { None, Default, Medium, Aggressive }
	
	
	// Public Bool to determine how many players there are. Default of one user.
	public bool TwoUsers = false;
	
	// Public Bool to determine if the sensor is used in near mode.
	public bool NearMode = false;

	// Public Bool to determine whether to display the color map too
	public bool DisplayColorMap = false;
	
	// Public Bool to determine whether to display user map after calibration.
	public bool DisplayUserMap = false;
	
	// Public Bool to determine whether to display the skeleton lines on user map.
	public bool DisplaySkeletonLines = false;
	
	/// How high off the ground is the sensor (in meters).
	public float SensorHeight = 0;

	// Kinect elevation angle (in degrees)
	public int SensorAngle = 0;
	
	// Minimum user distance in order to process skeleton data
	public float MinUserDistance = 1.0f;
	
	// Selection of smoothing parameters
	public Smoothing smoothing = Smoothing.Default;
	
	// Public Bool to determine the use of additional filters
	public bool UseBoneOrientationsFilter = false;
	public bool UseClippedLegsFilter = false;
	public bool UseBoneOrientationsConstraint = false;
	public bool UseSelfIntersectionConstraint = false;
	
	// Lists of GameObjects that will be controlled by which player.
	public List<GameObject> Player1Avatars;
	public List<GameObject> Player2Avatars;
	
	// Calibration poses for each player, if needed
	public KinectWrapper.Gestures Player1CalibrationPose;
	public KinectWrapper.Gestures Player2CalibrationPose;
	
	// List of Gestures to detect
	public List<KinectWrapper.Gestures> DetectGestures;
	
	// Bool to keep track of whether Kinect has been initialized
	bool KinectInitialized = false; 
	
	// Bools to keep track of who is currently calibrated.
	bool Player1Calibrated = false;
	bool Player2Calibrated = false;
	
	bool AllPlayersCalibrated = false;
	
	// Values to track which ID (assigned by the Kinect) is player 1 and player 2.
	uint Player1ID;
	uint Player2ID;
	
	// Lists of AvatarControllers that will let the models get updated.
	List<AvatarController> Player1Controllers;
	List<AvatarController> Player2Controllers;
	
	// User Map vars.
	Texture2D usersLblTex;
	Color[] usersMapColors;
	Rect usersMapRect;
	int usersMapSize;

	Texture2D usersClrTex;
	//Color[] usersClrColors;
	Rect usersClrRect;
	
	//short[] usersLabelMap;
	short[] usersDepthMap;
	float[] usersHistogramMap;
	
	// List of all users
	List<uint> allUsers;
	
	// GUI Text to show messages.
	GameObject CalibrationText;
	
	// Image stream handles for the kinect
	private IntPtr colorStreamHandle;
	private IntPtr depthStreamHandle;
	
	// Color image data, if used
	private Color32[] colorImage;
	
	// Skeleton related structures
	private KinectWrapper.NuiSkeletonFrame skeletonFrame;
	private KinectWrapper.NuiTransformSmoothParameters smoothParameters;
	
	// Skeleton tracking states, positions and joints' orientations
	private Vector3 player1Pos, player2Pos;
	private Matrix4x4 player1Ori, player2Ori;
	private bool[] player1JointsTracked, player2JointsTracked;
	private Vector3[] player1JointsPos, player2JointsPos;
	private Matrix4x4[] player1JointsOri, player2JointsOri;
	private KinectWrapper.NuiSkeletonBoneOrientation[] jointOrientations;
	
	// Calibration gesture data for each player
	private KinectWrapper.GestureData player1CalibrationData;
	private KinectWrapper.GestureData player2CalibrationData;
	
	// Lists of gesture data, for each player
	private List<KinectWrapper.GestureData> player1Gestures = new List<KinectWrapper.GestureData>();
	private List<KinectWrapper.GestureData> player2Gestures = new List<KinectWrapper.GestureData>();
	
	private Matrix4x4 kinectToWorld, flipMatrix;
	private static KinectManager instance;
	
    // Timer for controlling Filter Lerp blends.
    private float lastNuiTime;

	// Filters
	private BoneOrientationsFilter[] boneOrientationFilter;
	private ClippedLegsFilter[] clippedLegsFilter;
	private BoneOrientationsConstraint boneConstraintsFilter;
	private SelfIntersectionConstraint selfIntersectionConstraint;

	
    public static KinectManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	public static bool IsKinectInitialized()
	{
		return instance != null ? instance.KinectInitialized : false;
	}
	
	public static bool IsCalibrationNeeded()
	{
		return false;
	}
	
	public bool IsUserDetected()
	{
		return KinectInitialized && (allUsers.Count > 0);
	}
	
	public uint GetPlayer1ID()
	{
		return Player1ID;
	}
	
	public uint GetPlayer2ID()
	{
		return Player2ID;
	}

	public Vector3 GetUserPosition(uint UserId)
	{
		if(UserId == Player1ID)
			return player1Pos;
		else if(UserId == Player2ID)
			return player2Pos;
		
		return Vector3.zero;
	}
	
	public Quaternion GetUserOrientation(uint UserId, bool flip)
	{
		if(UserId == Player1ID)
			return ConvertMatrixToQuat(player1Ori, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter, flip);
		else if(UserId == Player2ID)
			return ConvertMatrixToQuat(player2Ori, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter, flip);
		
		return Quaternion.identity;
	}
	
	public bool IsJointTracked(uint UserId, int joint)
	{
		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsTracked.Length ? player1JointsTracked[joint] : false;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsTracked.Length ? player2JointsTracked[joint] : false;
		
		return false;
	}
	
	public Vector3 GetJointPosition(uint UserId, int joint)
	{
		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsPos.Length ? player1JointsPos[joint] : Vector3.zero;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsPos.Length ? player2JointsPos[joint] : Vector3.zero;
		
		return Vector3.zero;
	}
	
	public Quaternion GetJointOrientation(uint UserId, int joint, bool flip)
	{
		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsOri.Length ? 
				ConvertMatrixToQuat(player1JointsOri[joint], joint, flip) : Quaternion.identity;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsOri.Length ? 
				ConvertMatrixToQuat(player2JointsOri[joint], joint, flip) : Quaternion.identity;
		
		return Quaternion.identity;
	}
	
	public Vector3 GetJointDirFwd(uint UserId, int joint)
	{
		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsOri.Length ? (Vector3)player1JointsOri[joint].GetColumn(2) : Vector3.zero;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsOri.Length ? (Vector3)player2JointsOri[joint].GetColumn(2) : Vector3.zero;
		
		return Vector3.zero;
	}
	
	public Vector3 GetJointDirUp(uint UserId, int joint)
	{
		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsOri.Length ? (Vector3)player1JointsOri[joint].GetColumn(1) : Vector3.zero;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsOri.Length ? (Vector3)player2JointsOri[joint].GetColumn(1) : Vector3.zero;
		
		return Vector3.zero;
	}
	
	public Vector3 GetJointDirRight(uint UserId, int joint)
	{
		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsOri.Length ? (Vector3)player1JointsOri[joint].GetColumn(0) : Vector3.zero;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsOri.Length ? (Vector3)player2JointsOri[joint].GetColumn(0) : Vector3.zero;
		
		return Vector3.zero;
	}
	
	public void DetectGesture(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index >= 0)
			DeleteGesture(UserId, gesture);
		
		KinectWrapper.GestureData gestureData = new KinectWrapper.GestureData();
		
		gestureData.userId = UserId;
		gestureData.gesture = gesture;
		gestureData.state = 0;
		gestureData.joint = 0;
		gestureData.progress = 0f;
		gestureData.complete = false;
		
		if(UserId == Player1ID)
			player1Gestures.Add(gestureData);
		else if(UserId == Player2ID)
			player2Gestures.Add(gestureData);
	}
	
	public bool ResetGesture(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index < 0)
			return false;
		
		KinectWrapper.GestureData gestureData = (UserId == Player1ID) ? player1Gestures[index] : player2Gestures[index];
		
		gestureData.state = 0;
		gestureData.joint = 0;
		gestureData.progress = 0f;
		gestureData.complete = false;

		if(UserId == Player1ID)
			player1Gestures[index] = gestureData;
		else if(UserId == Player2ID)
			player2Gestures[index] = gestureData;
		
		return true;
	}
	
	public bool DeleteGesture(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index < 0)
			return false;
		
		if(UserId == Player1ID)
			player1Gestures.RemoveAt(index);
		else if(UserId == Player2ID)
			player2Gestures.RemoveAt(index);
		
		return true;
	}
	
	public void ClearGestures(uint UserId)
	{
		if(UserId == Player1ID)
		{
			player1Gestures.Clear();
		}
		else if(UserId == Player2ID)
		{
			player2Gestures.Clear();
		}
	}
	
	public void SetAvatarControllers()
	{
		if(Player1Controllers != null)
		{
			Player1Controllers.Clear();
	
			foreach(GameObject avatar in Player1Avatars)
			{
				AvatarController controller = avatar.GetComponent<AvatarController>();
				controller.RotateToInitialPosition();
				controller.Start();
				
				Player1Controllers.Add(controller);
			}
		}
		
		if(Player2Controllers != null)
		{
			Player2Controllers.Clear();
			
			foreach(GameObject avatar in Player2Avatars)
			{
				AvatarController controller = avatar.GetComponent<AvatarController>();
				controller.RotateToInitialPosition();
				controller.Start();
				
				Player2Controllers.Add(controller);
			}
		}
	}
	
	public void ClearKinectUsers()
	{
		if(!KinectInitialized)
			return;

		// remove current users
		for(int i = allUsers.Count - 1; i >= 0; i--)
		{
			uint userId = allUsers[i];
			RemoveUser(userId);
		}
		
		ResetFilters();
	}
	
	public void ResetFilters()
	{
		if(!KinectInitialized)
			return;
		
		// clear kinect vars
		player1Pos = Vector3.zero; player2Pos = Vector3.zero;
		player1Ori = Matrix4x4.identity; player2Ori = Matrix4x4.identity;
		
		int skeletonJointsCount = (int)KinectWrapper.NuiSkeletonPositionIndex.Count;
		for(int i = 0; i < skeletonJointsCount; i++)
		{
			player1JointsTracked[i] = false; player2JointsTracked[i] = false;
			player1JointsPos[i] = Vector3.zero; player2JointsPos[i] = Vector3.zero;
			player1JointsOri[i] = Matrix4x4.identity; player2JointsOri[i] = Matrix4x4.identity;
		}
		
		if(boneOrientationFilter != null)
		{
			for(int i = 0; i < boneOrientationFilter.Length; i++)
				if(boneOrientationFilter[i] != null)
					boneOrientationFilter[i].Reset();
		}
		
		if(clippedLegsFilter != null)
		{
			for(int i = 0; i < clippedLegsFilter.Length; i++)
				if(clippedLegsFilter[i] != null)
					clippedLegsFilter[i].Reset();
		}
	}
	

	void Start()
	{
		int hr = 0;
		
		try
		{
			hr = KinectWrapper.NuiInitialize(KinectWrapper.NuiInitializeFlags.UsesDepthAndPlayerIndex | 
				KinectWrapper.NuiInitializeFlags.UsesSkeleton | 
				(DisplayColorMap ? KinectWrapper.NuiInitializeFlags.UsesColor : 0));
            if (hr != 0)
			{
            	throw new Exception("NuiInitialize Failed");
			}
			
			hr = KinectWrapper.NuiSkeletonTrackingEnable(IntPtr.Zero, 8);  // 0, 12,8
			if (hr != 0)
			{
				throw new Exception("Cannot initialize Skeleton Data");
			}
			
			depthStreamHandle = IntPtr.Zero;
			hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.DepthAndPlayerIndex, 
				KinectWrapper.Constants.ImageResolution, 0, 2, IntPtr.Zero, ref depthStreamHandle);
			if (hr != 0)
			{
				throw new Exception("Cannot open depth stream");
			}
			
			colorStreamHandle = IntPtr.Zero;
			if(DisplayColorMap)
			{
				hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.Color, 
					KinectWrapper.Constants.ImageResolution, 0, 2, IntPtr.Zero, ref colorStreamHandle);
				if (hr != 0)
				{
					throw new Exception("Cannot open color stream");
				}
			}

			// set kinect elevation angle
			KinectWrapper.NuiCameraSetAngle((long)SensorAngle);
			
			// init skeleton structures
			skeletonFrame = new KinectWrapper.NuiSkeletonFrame() 
							{ 
								SkeletonData = new KinectWrapper.NuiSkeletonData[KinectWrapper.Constants.NuiSkeletonCount] 
							};
			
			// values used to pass to smoothing function
			smoothParameters = new KinectWrapper.NuiTransformSmoothParameters();
			
			switch(smoothing)
			{
				case Smoothing.Default:
					smoothParameters.fSmoothing = 0.5f;
					smoothParameters.fCorrection = 0.5f;
					smoothParameters.fPrediction = 0.5f;
					smoothParameters.fJitterRadius = 0.05f;
					smoothParameters.fMaxDeviationRadius = 0.04f;
					break;
				case Smoothing.Medium:
					smoothParameters.fSmoothing = 0.5f;
					smoothParameters.fCorrection = 0.1f;
					smoothParameters.fPrediction = 0.5f;
					smoothParameters.fJitterRadius = 0.1f;
					smoothParameters.fMaxDeviationRadius = 0.1f;
					break;
				case Smoothing.Aggressive:
					smoothParameters.fSmoothing = 0.7f;
					smoothParameters.fCorrection = 0.3f;
					smoothParameters.fPrediction = 1.0f;
					smoothParameters.fJitterRadius = 1.0f;
					smoothParameters.fMaxDeviationRadius = 1.0f;
					break;
			}
			
			// init the bone orientation filter
			boneOrientationFilter = new BoneOrientationsFilter[KinectWrapper.Constants.NuiSkeletonMaxTracked];
			for(int i = 0; i < boneOrientationFilter.Length; i++)
			{
				boneOrientationFilter[i] = new BoneOrientationsFilter();
				boneOrientationFilter[i].Init();
			}
			
			// init the clipped legs filter
			clippedLegsFilter = new ClippedLegsFilter[KinectWrapper.Constants.NuiSkeletonMaxTracked];
			for(int i = 0; i < boneOrientationFilter.Length; i++)
			{
				clippedLegsFilter[i] = new ClippedLegsFilter();
			}

			// init the bone orientation constraints
			boneConstraintsFilter = new BoneOrientationsConstraint();
			boneConstraintsFilter.AddDefaultConstraints();
			// init the self intersection constraints
			selfIntersectionConstraint = new SelfIntersectionConstraint();
			
			// create arrays for joint positions and joint orientations
			int skeletonJointsCount = (int)KinectWrapper.NuiSkeletonPositionIndex.Count;
			player1JointsTracked = new bool[skeletonJointsCount];
			player2JointsTracked = new bool[skeletonJointsCount];
			
			player1JointsPos = new Vector3[skeletonJointsCount];
			player2JointsPos = new Vector3[skeletonJointsCount];
			
			player1JointsOri = new Matrix4x4[skeletonJointsCount];
			player2JointsOri = new Matrix4x4[skeletonJointsCount];
			
			//create the transform matrix that converts from kinect-space to world-space
			Quaternion quat = new Quaternion();
			quat.eulerAngles = new Vector3(-SensorAngle, 0.0f, 0.0f);
			
			// transform matrix - kinect to world
			kinectToWorld.SetTRS(new Vector3(0.0f, SensorHeight, 0.0f), quat, Vector3.one);
			flipMatrix = Matrix4x4.identity;
			flipMatrix[2, 2] = -1;
			
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		catch (Exception e)
		{
			Debug.LogError(e.Message + " - " + KinectWrapper.GetNuiErrorString(hr));
			return;
		}

        // Initialize depth & label map related stuff
        usersMapSize = KinectWrapper.GetDepthWidth() * KinectWrapper.GetDepthHeight();
        usersLblTex = new Texture2D(KinectWrapper.GetDepthWidth(), KinectWrapper.GetDepthHeight());
        usersMapColors = new Color[usersMapSize];
        usersMapRect = new Rect(Screen.width, Screen.height - usersLblTex.height / 2, -usersLblTex.width / 2, usersLblTex.height / 2);
		
		if(DisplayColorMap)
		{
	        usersClrTex = new Texture2D(KinectWrapper.GetDepthWidth(), KinectWrapper.GetDepthHeight());
	        //usersClrColors = new Color[usersMapSize];
	        usersClrRect = new Rect(Screen.width, Screen.height - usersClrTex.height / 2, -usersClrTex.width / 2, usersClrTex.height / 2);
			usersMapRect.x -= usersClrTex.width / 2;
			
			colorImage = new Color32[usersMapSize];
		}
		
        usersDepthMap = new short[usersMapSize];
        usersHistogramMap = new float[5000];
		
        // Initialize user list to contain ALL users.
        allUsers = new List<uint>();
        
		// Pull the AvatarController from each of the players Avatars.
		Player1Controllers = new List<AvatarController>();
		Player2Controllers = new List<AvatarController>();
		
		// Add each of the avatars' controllers into a list for each player.
		foreach(GameObject avatar in Player1Avatars)
		{
			Player1Controllers.Add(avatar.GetComponent<AvatarController>());
		}
		
		foreach(GameObject avatar in Player2Avatars)
		{
			Player2Controllers.Add(avatar.GetComponent<AvatarController>());
		}
		
		// GUI Text.
		CalibrationText = GameObject.Find("CalibrationText");
		if(CalibrationText != null)
		{
			CalibrationText.GetComponent<GUIText>().text = "WAITING FOR USERS";
		}
		
		Debug.Log("Waiting for users.");
			
		KinectInitialized = true;
	}
	
	void Update()
	{
		AllPlayersCalibrated = false;
		Player1Calibrated = false;

		if(KinectInitialized)
		{
	        // If the players aren't all calibrated yet, draw the user map.
			if(allUsers.Count == 0 /**!AllPlayersCalibrated*/ || DisplayUserMap)
			{
				if(depthStreamHandle != IntPtr.Zero &&
					KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref usersDepthMap))
				{
		        	UpdateUserMap();
				}
				
				if(DisplayColorMap && colorStreamHandle != IntPtr.Zero &&
					KinectWrapper.PollColor(colorStreamHandle, ref colorImage))
				{
					UpdateColorMap();
				}
			}
			
			if(KinectWrapper.PollSkeleton(ref smoothParameters, ref skeletonFrame))
			{
				ProcessSkeleton();
			}
			
			// Update player 1's models if he/she is calibrated and the model is active.
			if(Player1Calibrated)
			{
				foreach (AvatarController controller in Player1Controllers)
				{
					if(controller.Active)
					{
						controller.UpdateAvatar(Player1ID, NearMode);
					}
					
					// Check for complete gestures
					foreach(KinectWrapper.GestureData gestureData in player1Gestures)
					{
						if(gestureData.complete)
						{
							if(controller.GestureComplete(Player1ID, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos))
							{
								ResetGesture(Player1ID, gestureData.gesture);
							}
						}
						else if(gestureData.progress > 0f)
						{
							controller.GestureInProgress(Player1ID, gestureData.gesture, gestureData.progress, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos);
						}
					}
				}
			}
			
			// Update player 2's models if he/she is calibrated and the model is active.
			if(Player2Calibrated)
			{
				foreach (AvatarController controller in Player2Controllers)
				{
					if(controller.Active)
					{
						controller.UpdateAvatar(Player2ID, NearMode);
					}

					// Check for complete gestures
					foreach(KinectWrapper.GestureData gestureData in player2Gestures)
					{
						if(gestureData.complete)
						{
							if(controller.GestureComplete(Player2ID, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos))
							{
								ResetGesture(Player2ID, gestureData.gesture);
							}
						}
						else if(gestureData.progress > 0f)
						{
							controller.GestureInProgress(Player2ID, gestureData.gesture, gestureData.progress, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos);
						}
					}
				}
			}
		}
		
		// Kill the program with ESC.
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
	
	// Make sure to kill the Kinect on quitting.
	void OnApplicationQuit()
	{
		if(KinectInitialized)
		{
			// Shutdown OpenNI
			KinectWrapper.NuiShutdown();
			instance = null;
		}
	}
	
	// Draw the Histogram Map on the GUI.
    void OnGUI()
    {
		if(KinectInitialized)
		{
	        if (allUsers.Count == 0 /**!AllPlayersCalibrated*/ && DisplayUserMap)
	        {
	            GUI.DrawTexture(usersMapRect, usersLblTex);
				
				if(DisplayColorMap)
				{
					GUI.DrawTexture(usersClrRect, usersClrTex);
				}
	        }
		}
    }
	
	// Update the User Map
    void UpdateUserMap()
    {
        // Flip the texture as we convert label map to color array
        int flipIndex, i;
        int numOfPoints = 0;
		Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);

        // Calculate cumulative histogram for depth
        for (i = 0; i < usersMapSize; i++)
        {
            // Only calculate for depth that contains users
            if ((usersDepthMap[i] & 7) != 0)
            {
                usersHistogramMap[usersDepthMap[i] >> 3]++;
                numOfPoints++;
            }
        }
		
        if (numOfPoints > 0)
        {
            for (i = 1; i < usersHistogramMap.Length; i++)
	        {   
		        usersHistogramMap[i] += usersHistogramMap[i-1];
	        }
			
            for (i = 0; i < usersHistogramMap.Length; i++)
	        {
                usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
	        }
        }

        // Create the actual users texture based on label map and depth histogram
        for (i = 0; i < usersMapSize; i++)
        {
            flipIndex = usersMapSize - i - 1;
			
			short userMap = (short)(usersDepthMap[i] & 7);
			short userDepth = (short)(usersDepthMap[i] >> 3);
			
            if (userMap == 0)
            {
                usersMapColors[flipIndex] = Color.clear;
            }
            else
            {
                // Create a blending color based on the depth histogram
				float histDepth = usersHistogramMap[userDepth];
                Color c = new Color(histDepth, histDepth, histDepth, 0.9f);
                
				switch(userMap % 4)
                {
                    case 0:
                        usersMapColors[flipIndex] = Color.red * c;
                        break;
                    case 1:
                        usersMapColors[flipIndex] = Color.green * c;
                        break;
                    case 2:
                        usersMapColors[flipIndex] = Color.blue * c;
                        break;
                    case 3:
                        usersMapColors[flipIndex] = Color.magenta * c;
                        break;
                }
            }
        }
		
		// Draw it!
        usersLblTex.SetPixels(usersMapColors);
        usersLblTex.Apply();
    }
	
	// Update the Color Map
	void UpdateColorMap()
	{
        usersClrTex.SetPixels32(colorImage);
        usersClrTex.Apply();
	}
	
	// Assign UserId to player 1 or 2.
    void CalibrateUser(uint UserId, ref KinectWrapper.NuiSkeletonData skeletonData)
    {
		// reset skeleton filters
		ResetFilters();
		
		// If player 1 hasn't been calibrated, assign that UserID to it.
		if(!Player1Calibrated)
		{
			// Check to make sure we don't accidentally assign player 2 to player 1.
			if (!allUsers.Contains(UserId))
			{
				if(CheckForCalibrationPose(UserId, ref Player1CalibrationPose, ref player1CalibrationData, ref skeletonData))
				{
					Player1Calibrated = true;
					Player1ID = UserId;
					
					allUsers.Add(UserId);
					
					foreach(AvatarController controller in Player1Controllers)
					{
						controller.SuccessfulCalibration(UserId);
					}
	
					// add the gestures to detect, if any
					foreach(KinectWrapper.Gestures gesture in DetectGestures)
					{
						DetectGesture(UserId, gesture);
					}
					
					// If we're not using 2 users, we're all calibrated.
					if(!TwoUsers)
					{
						AllPlayersCalibrated = true;
					}
				}
			}
		}
		// Otherwise, assign to player 2.
		else if(TwoUsers && !Player2Calibrated)
		{
			if (!allUsers.Contains(UserId))
			{
				if(CheckForCalibrationPose(UserId, ref Player2CalibrationPose, ref player2CalibrationData, ref skeletonData))
				{
					Player2Calibrated = true;
					Player2ID = UserId;
					
					allUsers.Add(UserId);
					
					foreach(AvatarController controller in Player2Controllers)
					{
						controller.SuccessfulCalibration(UserId);
					}
					
					// add the gestures to detect, if any
					foreach(KinectWrapper.Gestures gesture in DetectGestures)
					{
						DetectGesture(UserId, gesture);
					}
					
					// All users are calibrated!
					AllPlayersCalibrated = true;
				}
			}
		}
		
		// If all users are calibrated, stop trying to find them.
		if(AllPlayersCalibrated)
		{
			Debug.Log("");
			
			if(CalibrationText != null)
			{
				CalibrationText.GetComponent<GUIText>().text = "";
			}
		}
    }
	
	// Remove a lost UserId
	void RemoveUser(uint UserId)
	{
		// If we lose player 1...
		if(UserId == Player1ID)
		{
			// Null out the ID and reset all the models associated with that ID.
			Player1ID = 0;
			Player1Calibrated = false;
			
			foreach(AvatarController controller in Player1Controllers)
			{
				controller.RotateToCalibrationPose(UserId, IsCalibrationNeeded());
			}
			
			player1CalibrationData.userId = 0;
		}
		
		// If we lose player 2...
		if(UserId == Player2ID)
		{
			// Null out the ID and reset all the models associated with that ID.
			Player2ID = 0;
			Player2Calibrated = false;
			
			foreach(AvatarController controller in Player2Controllers)
			{
				controller.RotateToCalibrationPose(UserId, IsCalibrationNeeded());
			}
			
			player2CalibrationData.userId = 0;
		}
		
		// clear gestures list for this user
		ClearGestures(UserId);

        // remove from global users list
        allUsers.Remove(UserId);
		
		if(CalibrationText != null)
		{
			CalibrationText.GetComponent<GUIText>().text = "WAITING FOR USERS";
		}
		
		// Try to replace that user!
		Debug.Log("Waiting for users.");

		AllPlayersCalibrated = false;
	}
	
	// Process the skeleton data
	void ProcessSkeleton()
	{
		List<uint> lostUsers = new List<uint>();
		lostUsers.AddRange(allUsers);
		
		// calculate the time since last update
		float currentNuiTime = Time.realtimeSinceStartup;
		float deltaNuiTime = currentNuiTime - lastNuiTime;
		
		for (int i = 0; i < KinectWrapper.Constants.NuiSkeletonCount; i++)
		{
			KinectWrapper.NuiSkeletonData skeletonData = skeletonFrame.SkeletonData[i];
			uint userId = skeletonData.dwTrackingID;
			
			if (skeletonData.eTrackingState == KinectWrapper.NuiSkeletonTrackingState.SkeletonTracked)
			{
				if(!AllPlayersCalibrated)
				{
					CalibrateUser(userId, ref skeletonData);
				}

				//// get joints orientations
				//KinectWrapper.NuiSkeletonBoneOrientation[] jointOrients = new KinectWrapper.NuiSkeletonBoneOrientation[(int)KinectWrapper.NuiSkeletonPositionIndex.Count];
				//KinectWrapper.NuiSkeletonCalculateBoneOrientations(ref skeletonData, jointOrients);
				
				int stateTracked = (int)KinectWrapper.NuiSkeletonPositionTrackingState.Tracked;
				int stateNotTracked = (int)KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked;
				
				int [] mustBeTrackedJoints = { 
					(int)KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft,
					(int)KinectWrapper.NuiSkeletonPositionIndex.FootLeft,
					(int)KinectWrapper.NuiSkeletonPositionIndex.AnkleRight,
					(int)KinectWrapper.NuiSkeletonPositionIndex.FootRight,
				};
				
				// get the skeleton position
				Vector3 skeletonPos = kinectToWorld.MultiplyPoint3x4(skeletonData.Position);
				
				if(userId == Player1ID && skeletonPos.z >= MinUserDistance)
				{
					// get player position
					player1Pos = skeletonPos;
					
					// fixup skeleton to improve avatar appearance.
					if(UseClippedLegsFilter && clippedLegsFilter[0] != null)
					{
						clippedLegsFilter[0].FilterSkeleton(ref skeletonData, deltaNuiTime);
					}
	
					if(UseSelfIntersectionConstraint && selfIntersectionConstraint != null)
					{
						selfIntersectionConstraint.Constrain(ref skeletonData);
					}
	
					// get joints' position and rotation
					for (int j = 0; j < (int)KinectWrapper.NuiSkeletonPositionIndex.Count; j++)
					{
						player1JointsTracked[j] = Array.BinarySearch(mustBeTrackedJoints, j) >= 0 ? (int)skeletonData.eSkeletonPositionTrackingState[j] == stateTracked :
							(int)skeletonData.eSkeletonPositionTrackingState[j] != stateNotTracked;
						
						if(player1JointsTracked[j])
						{
							player1JointsPos[j] = kinectToWorld.MultiplyPoint3x4(skeletonData.SkeletonPositions[j]);
							//player1JointsOri[j] = jointOrients[j].absoluteRotation.rotationMatrix * flipMatrix;
						}
					}
					
					// draw the skeleton on top of texture
					if(DisplaySkeletonLines && (allUsers.Count == 0 /**!AllPlayersCalibrated*/ || DisplayUserMap))
					{
						DrawSkeleton(usersLblTex, ref player1JointsPos, ref player1JointsTracked);
					}
					
					// calculate joint orientations
					KinectWrapper.GetSkeletonJointOrientation(ref player1JointsPos, ref player1JointsTracked, ref player1JointsOri);
					
					// filter orientation constraints
					if(UseBoneOrientationsConstraint && boneConstraintsFilter != null)
					{
						boneConstraintsFilter.Constrain(ref skeletonData, ref player1JointsOri, false);
					}
					
                    // filter joint orientations.
                    // it should be performed after all joint position modifications.
	                if(UseBoneOrientationsFilter && boneOrientationFilter[0] != null)
	                {
	                    boneOrientationFilter[0].UpdateFilter(ref skeletonData, ref player1JointsOri);
	                }
	
					// get player rotation
					player1Ori = player1JointsOri[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter];
					
					// check for gestures
					int listGestureSize = player1Gestures.Count;
					
					for(int g = 0; g < listGestureSize; g++)
					{
						KinectWrapper.GestureData gestureData = player1Gestures[g];
						KinectWrapper.CheckForGesture(userId, ref gestureData, Time.realtimeSinceStartup, 
							ref player1JointsPos, ref player1JointsTracked);
						player1Gestures[g] = gestureData;
					}
				}
				else if(userId == Player2ID && skeletonPos.z >= MinUserDistance)
				{ 
					// get player position
					player2Pos = skeletonPos;
					
					// fixup skeleton to improve avatar appearance.
					if(UseClippedLegsFilter && clippedLegsFilter[1] != null)
					{
						clippedLegsFilter[1].FilterSkeleton(ref skeletonData, deltaNuiTime);
					}
	
					if(UseSelfIntersectionConstraint && selfIntersectionConstraint != null)
					{
						selfIntersectionConstraint.Constrain(ref skeletonData);
					}
	
					// get joints' position and rotation
					for (int j = 0; j < (int)KinectWrapper.NuiSkeletonPositionIndex.Count; j++)
					{
						player2JointsTracked[j] = Array.BinarySearch(mustBeTrackedJoints, j) >= 0 ? (int)skeletonData.eSkeletonPositionTrackingState[j] == stateTracked :
							(int)skeletonData.eSkeletonPositionTrackingState[j] != stateNotTracked;
						
						if(player2JointsTracked[j])
						{
							player2JointsPos[j] = kinectToWorld.MultiplyPoint3x4(skeletonData.SkeletonPositions[j]);
						}
					}
					
					// draw the skeleton on top of texture
					if(DisplaySkeletonLines && (allUsers.Count == 0 /**!AllPlayersCalibrated*/ || DisplayUserMap))
					{
						DrawSkeleton(usersLblTex, ref player2JointsPos, ref player2JointsTracked);
					}
					
					// calculate joint orientations
					KinectWrapper.GetSkeletonJointOrientation(ref player2JointsPos, ref player2JointsTracked, ref player2JointsOri);
					
					// filter orientation constraints
					if(UseBoneOrientationsConstraint && boneConstraintsFilter != null)
					{
						boneConstraintsFilter.Constrain(ref skeletonData, ref player2JointsOri, false);
					}
					
                    // filter joint orientations.
                    // it should be performed after all joint position modifications.
	                if(UseBoneOrientationsFilter && boneOrientationFilter[1] != null)
	                {
	                    boneOrientationFilter[1].UpdateFilter(ref skeletonData, ref player2JointsOri);
	                }
	
					// get player rotation
					player2Ori = player2JointsOri[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter];
					
					// check for gestures
					int listGestureSize = player2Gestures.Count;
					
					for(int g = 0; g < listGestureSize; g++)
					{
						KinectWrapper.GestureData gestureData = player2Gestures[g];
						KinectWrapper.CheckForGesture(userId, ref gestureData, Time.realtimeSinceStartup, 
							ref player2JointsPos, ref player2JointsTracked);
						player2Gestures[g] = gestureData;
					}
				}
				
				lostUsers.Remove(userId);
			}
		}
		
		// update the nui-timer
		lastNuiTime = currentNuiTime;
		
		// remove the lost users if any
		if(lostUsers.Count > 0)
		{
			foreach(uint userId in lostUsers)
			{
				RemoveUser(userId);
			}
			
			lostUsers.Clear();
		}
	}
	
	// draws the skeleton in the given texture
	private void DrawSkeleton(Texture2D aTexture, ref Vector3[] playerJointsPos, ref bool[] playerJointsTracked)
	{
		int jointsCount = (int)KinectWrapper.NuiSkeletonPositionIndex.Count;
		
		for(int i = 0; i < jointsCount; i++)
		{
			int parent = KinectWrapper.GetSkeletonJointParent(i);
			
			if(playerJointsTracked[i] && playerJointsTracked[parent])
			{
				Vector3 posParent = KinectWrapper.MapSkeletonPointToDepthPoint(playerJointsPos[parent]);
				Vector3 posJoint = KinectWrapper.MapSkeletonPointToDepthPoint(playerJointsPos[i]);
				
				posParent.y = KinectWrapper.Constants.ImageHeight - posParent.y - 1;
				posJoint.y = KinectWrapper.Constants.ImageHeight - posJoint.y - 1;
				posParent.x = KinectWrapper.Constants.ImageWidth - posParent.x - 1;
				posJoint.x = KinectWrapper.Constants.ImageWidth - posJoint.x - 1;
				
				//Color lineColor = playerJointsTracked[i] && playerJointsTracked[parent] ? Color.red : Color.yellow;
				DrawLine(aTexture, (int)posParent.x, (int)posParent.y, (int)posJoint.x, (int)posJoint.y, Color.yellow);
			}
		}
		
//		DrawLine(aTexture, 0, 0, 638, 479, Color.red);
//		DrawLine(aTexture, 1, 0, 639, 479, Color.red);
//
//		DrawLine(aTexture, 0, 479, 638, 0, Color.yellow);
//		DrawLine(aTexture, 1, 479, 639, 0, Color.yellow);
		
		aTexture.Apply();
	}
	
	// draws a line in a texture
	private void DrawLine(Texture2D a_Texture, int x1, int y1, int x2, int y2, Color a_Color)
	{
		int width = KinectWrapper.Constants.ImageWidth;
		int height = KinectWrapper.Constants.ImageHeight;
		
		int dy = y2 - y1;
		int dx = x2 - x1;
	 
		int stepy = 1;
		if (dy < 0) 
		{
			dy = -dy; 
			stepy = -1;
		}
		
		int stepx = 1;
		if (dx < 0) 
		{
			dx = -dx; 
			stepx = -1;
		}
		
		dy <<= 1;
		dx <<= 1;
	 
		if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
			a_Texture.SetPixel(x1, y1, a_Color);
		
		if (dx > dy) 
		{
			int fraction = dy - (dx >> 1);
			
			while (x1 != x2) 
			{
				if (fraction >= 0) 
				{
					y1 += stepy;
					fraction -= dx;
				}
				
				x1 += stepx;
				fraction += dy;
				
				if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
					a_Texture.SetPixel(x1, y1, a_Color);
			}
		}
		else 
		{
			int fraction = dx - (dy >> 1);
			
			while (y1 != y2) 
			{
				if (fraction >= 0) 
				{
					x1 += stepx;
					fraction -= dy;
				}
				
				y1 += stepy;
				fraction += dx;
				
				if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
					a_Texture.SetPixel(x1, y1, a_Color);
			}
		}
		
	}
	
	// convert the matrix to quaternion, taking care of the mirroring
	private Quaternion ConvertMatrixToQuat(Matrix4x4 mOrient, int joint, bool flip)
	{
		Vector4 vZ = mOrient.GetColumn(2);
		Vector4 vY = mOrient.GetColumn(1);

		if(!flip)
		{
			vZ.y = -vZ.y;
			vY.x = -vY.x;
			vY.z = -vY.z;
		}
		else
		{
			vZ.x = -vZ.x;
			vZ.y = -vZ.y;
			vY.z = -vY.z;
		}
		
		if(vZ.x != 0.0f || vZ.y != 0.0f || vZ.z != 0.0f)
			return Quaternion.LookRotation(vZ, vY);
		else
			return Quaternion.identity;
	}
	
	// return the index of gesture in the list, or -1 if not found
	private int GetGestureIndex(uint UserId, KinectWrapper.Gestures gesture)
	{
		if(UserId == Player1ID)
		{
			int listSize = player1Gestures.Count;
			for(int i = 0; i < listSize; i++)
			{
				if(player1Gestures[i].gesture == gesture)
					return i;
			}
		}
		else if(UserId == Player2ID)
		{
			int listSize = player2Gestures.Count;
			for(int i = 0; i < listSize; i++)
			{
				if(player2Gestures[i].gesture == gesture)
					return i;
			}
		}
		
		return -1;
	}
	
	// check if the calibration pose is complete for given user
	private bool CheckForCalibrationPose(uint userId, ref KinectWrapper.Gestures calibrationGesture, 
		ref KinectWrapper.GestureData gestureData, ref KinectWrapper.NuiSkeletonData skeletonData)
	{
		if(calibrationGesture == KinectWrapper.Gestures.None)
			return true;
		
		// init gesture data if needed
		if(gestureData.userId == 0)
		{
			gestureData.userId = userId;
			gestureData.gesture = calibrationGesture;
			gestureData.state = 0;
			gestureData.joint = 0;
			gestureData.progress = 0f;
			gestureData.complete = false;
		}
		
		// get temporary joints' position
		int skeletonJointsCount = (int)KinectWrapper.NuiSkeletonPositionIndex.Count;
		bool[] jointsTracked = new bool[skeletonJointsCount];
		Vector3[] jointsPos = new Vector3[skeletonJointsCount];

		int stateTracked = (int)KinectWrapper.NuiSkeletonPositionTrackingState.Tracked;
		int stateNotTracked = (int)KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked;
		
		int [] mustBeTrackedJoints = { 
			(int)KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft,
			(int)KinectWrapper.NuiSkeletonPositionIndex.FootLeft,
			(int)KinectWrapper.NuiSkeletonPositionIndex.AnkleRight,
			(int)KinectWrapper.NuiSkeletonPositionIndex.FootRight,
		};
		
		for (int j = 0; j < skeletonJointsCount; j++)
		{
			jointsTracked[j] = Array.BinarySearch(mustBeTrackedJoints, j) >= 0 ? (int)skeletonData.eSkeletonPositionTrackingState[j] == stateTracked :
				(int)skeletonData.eSkeletonPositionTrackingState[j] != stateNotTracked;
			
			if(jointsTracked[j])
			{
				jointsPos[j] = kinectToWorld.MultiplyPoint3x4(skeletonData.SkeletonPositions[j]);
			}
		}
		
		// estimate the gesture progess
		KinectWrapper.CheckForGesture(userId, ref gestureData, Time.realtimeSinceStartup, 
			ref jointsPos, ref jointsTracked);
		
		// check if gesture is complete
		if(gestureData.complete)
		{
			gestureData.userId = 0;
			return true;
		}
		
		return false;
	}
	
}


