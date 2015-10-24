using UnityEngine;
using System.Collections;



public class Multi_DynamicController : MonoBehaviour {
	
	Multi_SelectionManager manager;
	private Vector3 screenPoint;
	private Vector3 offset;
	private Vector3 curPosition;
	private Vector3 curScreenPoint;
	
	void Start(){
		
		GameObject selectionManager = GameObject.Find("Mapping_Script");
		manager = selectionManager.GetComponent<Multi_SelectionManager>();

	}

	void OnMouseDown(){
		
		//Assign current controller index to selectionManager.activeIndex
		string name = gameObject.name;
		
		manager.indexChanged = true;
		manager.activeIndex =  int.Parse(name[1].ToString())-1;
//		Debug.Log (int.Parse(name[1].ToString()) - 1);
		
		screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
    	offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));

	}
	
	void OnMouseDrag(){
		
		curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        gameObject.transform.position = curPosition;
		
	}
	
		void OnGUI()
	{
	//GUI.Box (new Rect(0, 50, 300, 50), screenPoint.ToString("###############"));
	 // GUI.Box (new Rect(0, 50, 600, 50), curPosition.ToString("##.000"));
	}
}
