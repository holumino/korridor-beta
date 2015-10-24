using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Multi_Homography : MonoBehaviour {
	
	Multi_SelectionManager manager;
    
	public GameObject[] plane;
	public int[] index= new int[4];
	public int total;
	
	private float[] matrix = new float[16];
    private Vector3[] source ;
    private Vector3[] destination;
    private Matrix4x4 MVP = new Matrix4x4();
 
	private Camera camera;
  	private float scale=1f;
	
	void Start () {
	
	    camera  =GameObject.Find("Main Camera").GetComponent<Camera>();
		manager =GameObject.Find("Mapping_Script").GetComponent<Multi_SelectionManager>();
		source=new Vector3[total];
		destination=new Vector3[total];
	 
		// configure perspective matrix from Kinect
		//holomatrix=GameObject.Find("Texture_Camera").GetComponent<HolographicCamera_revised>();
		
	     
	}
	
	// Update is called once per frame
	void Update () {
		
       
			// update perspective matrix from Kinect
   			//MVP = holomatrix.m * camera.worldToCameraMatrix;
			  MVP  = camera.projectionMatrix*camera.worldToCameraMatrix;
//			  Debug.Log(total);
			for (int i = 0; i < total; i++) {
                source[i] = MVP.MultiplyPoint(manager.staticControllers[i].transform.position);

                destination[i] = MVP.MultiplyPoint(manager.dynamicControllers[i].transform.position);	
		
            }
	
			for (int i=0; i<plane.Length; i++)
			{
					FindHomography(index[i],ref source, ref destination, ref matrix);
				    setTexture(plane[i].GetComponent<Renderer>().material);
			}      
			
			
					       
	}
	
	void setTexture(Material temp) {
		 	temp.SetVector("matrixRow_1", new Vector4(matrix[0], matrix[4], matrix[8], matrix[12]));
            temp.SetVector("matrixRow_2", new Vector4(matrix[1], matrix[5], matrix[9], matrix[13]));
            temp.SetVector("matrixRow_3", new Vector4(matrix[2], matrix[6], matrix[10], matrix[14]));
            temp.SetVector("matrixRow_4", new Vector4(matrix[3], matrix[7], matrix[11], matrix[15]));
	}
	
	void FindHomography(int i, ref Vector3[] src, ref Vector3[] dest, ref float[] homography) 
	{
		
		// originally by arturo castro - 08/01/2010  
	    //  
	    // create the equation system to be solved  
	    //  
	    // from: Multiple View Geometry in Computer Vision 2ed  
	    //       Hartley R. and Zisserman A.  
	    //  
	    // x' = xH  
	    // where H is the homography: a 3 by 3 matrix  
	    // that transformed to inhomogeneous coordinates for each point  
	    // gives the following equations for each point:  
	    //  
	    // x' * (h31*x + h32*y + h33) = h11*x + h12*y + h13  
	    // y' * (h31*x + h32*y + h33) = h21*x + h22*y + h23  
	    //  
	    // as the homography is scale independent we can let h33 be 1 (indeed any of the terms)  
	    // so for 4 points we have 8 equations for 8 terms to solve: h11 - h32  
	    // after ordering the terms it gives the following matrix  
	    // that can be solved with gaussian elimination:  
		
		float[,] P = new float [,]{
			
	 
        
        {-src[i].x, -src[i].y, -1,   0,   0,  0, src[i].x*dest[i].x, src[i].y*dest[i].x, -dest[i].x }, // h11  
        {  0,   0,  0, -src[i].x, -src[i].y, -1, src[i].x*dest[i].y, src[i].y*dest[i].y, -dest[i].y }, // h12  
        
        {-src[i+1].x, -src[i+1].y, -1,   0,   0,  0, src[i+1].x*dest[i+1].x, src[i+1].y*dest[i+1].x, -dest[i+1].x }, // h13  
        {  0,   0,  0, -src[i+1].x, -src[i+1].y, -1, src[i+1].x*dest[i+1].y, src[i+1].y*dest[i+1].y, -dest[i+1].y }, // h21  
          
        {-src[i+2].x, -src[i+2].y, -1,   0,   0,  0, src[i+2].x*dest[i+2].x, src[i+2].y*dest[i+2].x, -dest[i+2].x }, // h22  
        {  0,   0,  0, -src[i+2].x, -src[i+2].y, -1, src[i+2].x*dest[i+2].y, src[i+2].y*dest[i+2].y, -dest[i+2].y }, // h23  
          
        {-src[i+3].x, -src[i+3].y, -1,   0,   0,  0, src[i+3].x*dest[i+3].x, src[i+3].y*dest[i+3].x, -dest[i+3].x }, // h31  
        {  0,   0,  0, -src[i+3].x, -src[i+3].y, -1, src[i+3].x*dest[i+3].y, src[i+3].y*dest[i+3].y, -dest[i+3].y }, // h32  
    	};  
		
		GaussianElimination(ref P,9);  
      
    	// gaussian elimination gives the results of the equation system  
    	// in the last column of the original matrix.  
    	// opengl needs the transposed 4x4 matrix:  
	    float[] aux_H={ P[0,8],P[3,8],0,P[6,8], // h11  h21 0 h31  
	        P[1,8],P[4,8],0,P[7,8], // h12  h22 0 h32  
	        0      ,      0,0,0,       // 0    0   0 0  
	        P[2,8],P[5,8],0,1};      // h13  h23 0 h33  
	      
	    for(int a=0;a<16;a++) homography[a] = aux_H[a];  
		
	}

    

	void GaussianElimination (ref float[,] A, int n)
	{
		// originally by arturo castro - 08/01/2010  
	    //  
	    // ported to c from pseudocode in  
	    // http://en.wikipedia.org/wiki/Gaussian_elimination  
	      
	    int i = 0;  
	    int j = 0;  
	    int m = n-1;  
	    while (i < m && j < n){  
	        // Find pivot in column j, starting in row i:  
	        int maxi = i;  
	        for(int k = i+1; k<m; k++){  
	            if(Mathf.Abs(A[k,j]) > Mathf.Abs(A[maxi,j])){  
	                maxi = k;  
	            }  
	        }  
	        if (A[maxi,j] != 0){  
	            //swap rows i and maxi, but do not change the value of i  
	            if(i!=maxi)  
	                for(int k=0;k<n;k++){  
	                    float aux = A[i,k];  
	                    A[i,k]=A[maxi,k];  
	                    A[maxi,k]=aux;  
	                }  
	            //Now A[i,j] will contain the old value of A[maxi,j].  
	            //divide each entry in row i by A[i,j]  
	            float A_ij=A[i,j];  
	            for(int k=0;k<n;k++){  
	                A[i,k]/=A_ij;  
	            }  
	            //Now A[i,j] will have the value 1.  
	            for(int u = i+1; u< m; u++){  
	                //subtract A[u,j] * row i from row u  
	                float A_uj = A[u,j];  
	                for(int k=0;k<n;k++){  
	                    A[u,k]-=A_uj*A[i,k];  
	                }  
	                //Now A[u,j] will be 0, since A[u,j] - A[i,j] * A[u,j] = A[u,j] - 1 * A[u,j] = 0.  
	            }  
	              
	            i++;  
	        }  
	        j++;  
	    }  
	      
	    //back substitution  
	    for(int k=m-2;k>=0;k--){  
	        for(int l=k+1;l<n-1;l++){  
	            A[k,m]-=A[k,l]*A[l,m];  
	            //A[i*n+j]=0;  
	        }  
	    }  
	}
	
		 
}
