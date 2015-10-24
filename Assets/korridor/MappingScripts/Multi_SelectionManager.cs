using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Multi_SelectionManager: MonoBehaviour {
	
	public  List<GameObject> staticControllers { get; set; }
	public  List<GameObject> dynamicControllers { get; set; }
    public int activeIndex { get; set; }
	public bool indexChanged{ get; set; }
	
	public int total;
	
	private int lastActiveIndex;
	private bool dynamicControllerVisibility;
	private bool startHomography = true;
	private GameObject dynamicControllerChilds;
	private GameObject staticControllerChilds;
	
	// Use this for initialization
	void Start () {
		Cursor.visible = true;
		dynamicControllerChilds  = GameObject.Find("D");		
		staticControllerChilds   = GameObject.Find("S");

		//Define Lists
		staticControllers = new List<GameObject>();
		dynamicControllers = new List<GameObject>();
			
		//intiate activeIndex
		activeIndex = 0;
		lastActiveIndex = 0;
		indexChanged = false;
		
		//Dynamic Controller Visibility
		dynamicControllerVisibility = true;
		
		startHomography = true;
		
		
		//Turn off visibility of static controllers
		foreach(Transform childs in staticControllerChilds.transform){
			foreach(Transform child in childs.transform){
			staticControllers.Add(child.gameObject);
			child.GetComponent<Renderer>().enabled = false;
			}
		}	
		
		
		//Change the colour of the dynamic controllers and add the DynamicController Script as a Component
		foreach(Transform childs in dynamicControllerChilds.transform){
			foreach(Transform child in childs.transform){
			dynamicControllers.Add (child.gameObject);
			
			child.gameObject.AddComponent<Multi_DynamicController>();
			child.GetComponent<Renderer>().material.color = Color.red;
			}
		}
		
		total=staticControllers.Count;
		}
	
	// Update is called once per frame
	void Update () {
		
		if(indexChanged == true){
			
			//Change colour of active controller to green
			indexChanged = false;
			dynamicControllers[lastActiveIndex].GetComponent<Renderer>().material.color = Color.red;
			lastActiveIndex = activeIndex;
			dynamicControllers[activeIndex].GetComponent<Renderer>().material.color = Color.green;
						
		}
		
		//Toggle dynamic controllers visibility
		
		if(Input.GetKeyDown("v")){
			
			
			if(dynamicControllerVisibility == true){
				dynamicControllerVisibility = false;
				Cursor.visible = false;
				foreach(GameObject child in dynamicControllers){
					child.GetComponent<Renderer>().enabled = false;}
			}
			else {
				dynamicControllerVisibility = true;	
				Cursor.visible = true;
				foreach(GameObject child in dynamicControllers){
					child.GetComponent<Renderer>().enabled = true;		}
			}
		}		
		if (Input.GetKeyDown("r")){
			for (int i = 0; i <= dynamicControllers.Count-1; i++) {
		    dynamicControllers[i].transform.position=staticControllers[i].transform.position;}
		}
	}
}


