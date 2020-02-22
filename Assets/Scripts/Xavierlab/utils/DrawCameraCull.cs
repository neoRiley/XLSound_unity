using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DrawCameraCull : MonoBehaviour
{
	#if UNITY_EDITOR
	public static Vector2 GetMainGameViewSize()
	{
		System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
		System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		System.Object Res = GetSizeOfMainGameView.Invoke(null,null);
		return (Vector2)Res;
	}
	#endif

	//public static DrawCameraCull instance;
	public Color color = new Color(0.1f, 0.14f, 0.8f, 0.5f);
	public bool isZoomed = false;

	protected float unitsWidth = 8.95f;
	public float zoomedOrthoSize = 2f;

	protected float camVertExtent;
	protected float camHorzExtent;

	protected Transform playerTransform;
	protected Camera cam;
	protected Vector2 topRightCorner;
	protected Vector2 screenExtents;

    public Vector2 size;
    public float aspect;
    public GameObject targetSurface;

	void Awake(){
		//instance = this;
	}

	public void Start(){
		
		cam = GetComponent<Camera>();

		topRightCorner = new Vector2(1, 1);
		screenExtents = cam.ViewportToWorldPoint(topRightCorner);


        SceneManager.sceneLoaded += (Scene arg0, LoadSceneMode arg1) =>
        {
            CheckCameraView();
        };


        CheckCameraView();
	}

    public void ToggleZoom(){
		isZoomed = !isZoomed;

		if( isZoomed ) {
			cam.orthographicSize = zoomedOrthoSize;
			UpdateExtents();
		} else {
			cam.transform.position = new Vector3(0,0,-10);
			CheckCameraView();
		}
	}

	public void Reset(){
		isZoomed = false;
		cam.transform.position = new Vector3(0,0,-10);
		CheckCameraView();
	}

	protected float camX;
	protected float camY;
	protected float leftBound;
	protected float rightBound;
	protected float bottomBound;
	protected float topBound;
	

	#if UNITY_EDITOR
	public virtual void OnDrawGizmos()
	{	
		//if( Application.isPlaying ) return;

		CheckCameraView();

		Gizmos.color = color;

		Matrix4x4 temp = Gizmos.matrix;
		Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
		if (GetComponent<Camera>().orthographic) {
			float spread = GetComponent<Camera>().farClipPlane - GetComponent<Camera>().nearClipPlane;
			float center = (GetComponent<Camera>().farClipPlane + GetComponent<Camera>().nearClipPlane)*0.5f;
            projectionSize = new Vector3(GetComponent<Camera>().orthographicSize * 2 * GetComponent<Camera>().aspect, GetComponent<Camera>().orthographicSize * 2, 0);
            Gizmos.DrawWireCube(new Vector3(0,0,center), new Vector3(GetComponent<Camera>().orthographicSize*2*GetComponent<Camera>().aspect, GetComponent<Camera>().orthographicSize*2, spread));
		} else {
			Gizmos.DrawFrustum(Vector3.zero, GetComponent<Camera>().fieldOfView, GetComponent<Camera>().farClipPlane, GetComponent<Camera>().nearClipPlane, GetComponent<Camera>().aspect);
		}
        size = GetMainGameViewSize();
        aspect = GetComponent<Camera>().aspect;

        if( drawProjection ) CreatePlane();

        Gizmos.matrix = temp;
	}

    public bool drawProjection = false;
    public Vector3 projectionSize;
    public Vector3 hitLocation;
    public Color povColor = Color.blue;
    protected void CreatePlane()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        RaycastHit hit;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        Debug.DrawRay(ray.origin, cam.transform.forward, Color.cyan, 5.0f);

        Physics.Raycast(ray, out hit);

        if( hit.collider != null )
        {
            //Debug.Log("<color='orange'><b>HIT: </b></color>:" + hit.collider.gameObject.tag);
            Gizmos.color = povColor;
            Vector3 hp = hit.point;
            hp.y += 0.01f;
            Gizmos.DrawCube(cam.transform.InverseTransformPoint(hp), projectionSize);
            hitLocation = hp;

            //GameObject projection = GameObject.CreatePrimitive(PrimitiveType.Quad);
            //projection.transform.parent = cam.transform;
            //projection.transform.position = hit.point;
        }
    }

#endif



	protected void CheckCameraView(){
		
	}

	protected void UpdateExtents(){
		//if( !Application.isPlaying || Application.loadedLevel == 0 ) return;

		// update the extents
		camVertExtent = cam.orthographicSize;
		camHorzExtent = cam.aspect * camVertExtent;

		leftBound   = -screenExtents.x + camHorzExtent;
		rightBound  = screenExtents.x - camHorzExtent;
		bottomBound = -screenExtents.y + camVertExtent;
		topBound    = screenExtents.y - camVertExtent;
	}
}

