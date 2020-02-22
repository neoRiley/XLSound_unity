using UnityEngine;
using System.Collections;

public delegate void FingerUp(Vector3 value, int fingerID, RaycastHit hitObject, Vector3 direction);
public delegate void FingerDown(Vector3 value, int fingerID, RaycastHit hitObject);
public delegate void FingerMove(Vector3 value, int fingerID);
public delegate void FingerStationary(Vector3 value, int fingerID);
public delegate void ArrowDown(KeyCode value);
public delegate void TouchesReceived();

public class InputManager : MonoBehaviourSingleton<InputManager>
{	
	public event FingerUp FingerUpEvent;
	public event FingerDown FingerDownEvent;
	public event FingerMove FingerMoveEvent;
	public event FingerStationary FingerStationaryEvent;
	public event TouchesReceived TouchesReceivedEvent;
	public event ArrowDown ArrowDownEvent;

	public bool isMouseDown = false;
	public bool isUp = false;
	public bool isDown = false;
	public bool isLeft = false;
	public bool isRight = false;

	public bool isPinching = false;
	public bool isPinchingEnabled = true;
	public int touchCount = 0;
	public float accelDeadZone = 1.5f;

	protected Vector3 calibratedAccel = Vector3.zero;

	protected Vector3 pos_0;
	protected Vector3 pos_down;
	protected Vector3 pos_up;

	protected float pinchDelta = 0;
	protected float pinchDistance = 0;
	public float PinchDistance{
		get{ return pinchDistance; }
	}

	void Update ()
	{
#if UNITY_IOS && !UNITY_EDITOR
//#if UNITY_IOS || UNITY_EDITOR

		touchCount = Input.touchCount;
		if( Input.touchCount > 0 )
		{


			if( Input.touchCount == 2 && isPinchingEnabled ){
				if( !isPinching ){
					isPinching = true;

					pinchDelta = Vector2.Distance(Input.touches[0].position, Input.touches[1].position);
				} else {
					pinchDistance = Vector2.Distance(Input.touches[0].position, Input.touches[1].position) - pinchDelta;
					pinchDelta = Vector2.Distance(Input.touches[0].position, Input.touches[1].position);
				}
			} else {
				isPinching = false;
			}

			if( TouchesReceivedEvent != null ) TouchesReceivedEvent();
			
			foreach ( Touch t in Input.touches )
			{
				pos_0 = t.position;
				pos_0.z = t.fingerId;

				if( t.phase == TouchPhase.Began )
				{
					isMouseDown = true;
					DoRayCast(pos_0);
					pos_down = pos_0;
					if(FingerDownEvent != null) FingerDownEvent(t.position, t.fingerId, hit);
				}
				else if( t.phase == TouchPhase.Ended )
				{
					isMouseDown = false;
					DoRayCast(pos_0);
					pos_up = pos_0;
					Vector2 dir = pos_up - pos_down;// subtracting start/end vectors gives us heading/directional vector
					if(FingerUpEvent != null) FingerUpEvent(t.position, t.fingerId, hit, dir);
				}
				else if( t.phase == TouchPhase.Moved )
				{
					isMouseDown = true;
					if(FingerMoveEvent != null) FingerMoveEvent(t.position, t.fingerId);
				}
				else if( t.phase == TouchPhase.Stationary )
				{
					isMouseDown = true;
					if(FingerStationaryEvent != null) FingerStationaryEvent(t.position, t.fingerId);
				}
			}
		}
		else if( Input.touchCount == 0)
		{
			isPinching = false;

			if( isMouseDown ) 
			{
				isMouseDown = false;
				if(FingerUpEvent != null) FingerUpEvent(Vector3.zero, -1, hit, Vector3.zero);
			}
		}


		calibratedAccel = calibrationMatrix.MultiplyVector(Input.acceleration) *10;

		ResetArrowFlags();

		if( Mathf.Abs(calibratedAccel.x) >= accelDeadZone || Mathf.Abs(calibratedAccel.y) >= accelDeadZone )
		{
			if( Mathf.Abs(calibratedAccel.x) > Mathf.Abs(calibratedAccel.y) ) // they're leaning right/left
			{
				if( calibratedAccel.x > 0 ) isRight = true;
				else isLeft = true;
			} else { // they're leaning forward/back
				if( calibratedAccel.y > 0 ) isUp = true;
				else isDown = true;
			}
		}

#endif		
		if( Input.GetMouseButtonDown(0) )
		{
			DoRayCast(Input.mousePosition);
			pos_down = Input.mousePosition;
			isMouseDown = true;
			if(FingerDownEvent != null) FingerDownEvent(Input.mousePosition, -1, hit);
		}
		else if( Input.GetMouseButtonUp(0) && isMouseDown )
		{
			isMouseDown = false;
			pos_up = Input.mousePosition;
			Vector2 dir = pos_up - pos_down;// subtracting start/end vectors gives us heading/directional vector
			if(FingerUpEvent != null) FingerUpEvent(Input.mousePosition, -1, hit, dir);
		}



		if(Input.GetKeyDown(KeyCode.UpArrow)){ ResetArrowFlags(); isUp = true; if( ArrowDownEvent!= null ){ ArrowDownEvent(KeyCode.UpArrow); }} 
		if(Input.GetKeyUp(KeyCode.UpArrow)) isUp = false;

		if(Input.GetKeyDown(KeyCode.DownArrow)){ ResetArrowFlags(); isDown = true; if( ArrowDownEvent!= null ){ ArrowDownEvent(KeyCode.DownArrow); } }
		if(Input.GetKeyUp(KeyCode.DownArrow)) isDown = false;

		if(Input.GetKeyDown(KeyCode.RightArrow)){ ResetArrowFlags(); isRight = true; if( ArrowDownEvent!= null ){ ArrowDownEvent(KeyCode.RightArrow); } }
		if(Input.GetKeyUp(KeyCode.RightArrow)) isRight = false;

		if(Input.GetKeyDown(KeyCode.LeftArrow)){ ResetArrowFlags(); isLeft = true; if( ArrowDownEvent!= null ){ ArrowDownEvent(KeyCode.LeftArrow); } }
		if(Input.GetKeyUp(KeyCode.LeftArrow)) isLeft = false;

	}

#if UNITY_IOS || UNITY_EDITOR
	private Matrix4x4 calibrationMatrix;
	public void SetCalibration()
	{
		Quaternion rotateQuaternion = Quaternion.FromToRotation(new Vector3(0.0f, 0.0f, -1.0f), Input.acceleration);
		
		//create identity matrix ... rotate our matrix to match up with down vec
		calibrationMatrix = Matrix4x4.TRS(Vector3.zero, rotateQuaternion, new Vector3(1.0f, 1.0f, 1.0f)).inverse;
	}
#endif


	protected void ResetArrowFlags()
	{
		isUp = false;
		isDown = false;
		isRight = false;
		isLeft = false;
	}
	
	protected Ray ray;
	public RaycastHit hit;
	public bool DoRayCast(Vector3 pos)
	{
		if(Camera.main)
		{
			ray = Camera.main.ScreenPointToRay(new Vector3(pos.x, pos.y, 0));
			if( Physics.Raycast( ray, out hit ) )
			{
				return true;
			}
		}
		
		return false;
	}
}

