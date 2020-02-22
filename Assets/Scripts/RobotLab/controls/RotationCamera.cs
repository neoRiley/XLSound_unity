using UnityEngine;
using System.Collections;

public class RotationCamera : MonoBehaviourSingleton<RotationCamera>
{	
	public Camera camera;
	public float initX = 0; // initial X rotation
	public float initY = 0; // initial Y rotation
	
	public bool canUpdate = false;

	public float distance = 10f;
	public float maxDistance = 20f;
	public float minDistance = 1f;
	public float yMinLimit = -90f;
	public float yMaxLimit = 90f;

	public float zoomSensitivity = 0.5f;
	public float xSpeed = 0.125f;// amount of scale to apply to the deltax
	public float ySpeed = 0.1f;// amoutn of scale to apply to the deltay
	public float xStrafeSpeed = 0.125f;// amount of scale to apply to the deltax
	public float yStrafeSpeed = 0.1f;// amoutn of scale to apply to the deltay

	private Vector3 target; // position of what we're orbiting
	private GameObject rotXContainer;
	private GameObject rotYContainer;
	private GameObject worldContainer;

	private float x = 0.0f; // x amt of rotation
	private float y = 0.0f; // y amt of rotation
	private float xs = 0.0f; // x amt of rotation
	private float ys = 0.0f; // y amt of rotation
	
	private Vector2 posDelta = Vector2.zero;
	private Vector2 strafeDelta = Vector2.zero;
	private float zoomDelta = 0f;
	
	private float xSmooth = 0.0f;
	private float ySmooth = 0.0f;

	private float xStrafeSmooth = 0.0f;
	private float yStrafeSmooth = 0.0f;

	public void Start ()
	{
		Init(gameObject.transform.position);
	}

	public void Init(Vector3 _target)
	{
		target = _target;
		if( camera == null ) camera = Camera.main;

		if( worldContainer != null ) Destroy(worldContainer.gameObject);
		rotYContainer = new GameObject("rotYContainer");
		rotXContainer = new GameObject("rotXContainer");
		worldContainer = new GameObject("worldContainer");


		rotYContainer.transform.parent = rotXContainer.transform;
		rotXContainer.transform.parent = worldContainer.transform;

		Reset(true);
	}
	
	public void Reset(bool p_canUpdate)
	{
		camera.transform.parent = rotYContainer.transform;
		worldContainer.transform.position = target;
		camera.transform.localPosition = Vector3.zero;

		Vector3 pos = camera.transform.localPosition;
		pos.z = -distance;
		camera.transform.localPosition = pos;

		camera.transform.localRotation = Quaternion.identity;

		rotX = initX;
		rotYContainer.transform.rotation = Quaternion.identity;
		rotXContainer.transform.rotation = Quaternion.identity;
		rotYContainer.transform.Rotate(initX, 0, 0, Space.Self);
		rotXContainer.transform.Rotate(0, initY, 0, Space.Self);

		canUpdate = p_canUpdate;
	}

	public void SetTarget(GameObject obj)
	{
		SetTarget(obj.transform.position);
	}

	public void SetTarget(Transform obj)
	{
		SetTarget(obj.position);
	}

	public void SetTarget(Vector3 _target)
	{
		target = _target;
		worldContainer.transform.position = target;
	}

	public void rotateOnX(float amt)
	{
		rotYContainer.transform.Rotate(amt,0,0,Space.Self);
	}
		
	public void rotateOnY(float amt)
	{
		rotXContainer.transform.Rotate(0, amt, 0, Space.Self);
	}
		
	public void UpdateCameraDistance(float val)
	{
		distance = val;	
	}

	public void OnGUI()
	{
		Event e = Event.current;

		if( e.isMouse && e.delta != Vector2.zero && e.type == EventType.MouseDrag && Input.GetMouseButton(0) ) 
		{
			posDelta = e.delta;
		}
		else if( e.delta != Vector2.zero && e.type == EventType.ScrollWheel )
		{			
			// scale percent to world size
			zoomDelta = -e.delta.y * zoomSensitivity;
		}
		else if( InputManager.instance.isPinching )
		{
			distance += -InputManager.instance.PinchDistance*pinchSensitivity;
		}
		else if( e.isMouse && e.delta != Vector2.zero && e.type == EventType.MouseDrag && Input.GetMouseButton(1) )
		{
			strafeDelta = e.delta;
		}
	}
	
	private float rotX = 0;
	public float ySmoothSensitivity = 0.02f;
	public float xSmoothSensitivity = 0.02f;
	public float pinchSensitivity = 0.01f;
	public void Update ()
	{
		if (target != null && canUpdate) 
		{
			x = posDelta.x * xSpeed;
			y = posDelta.y * ySpeed;
			xs = strafeDelta.x * xStrafeSpeed;
			ys = strafeDelta.y * yStrafeSpeed;
			
			if( !InputManager.instance.isMouseDown )
			{
				xSmooth = Mathf.Lerp(xSmooth, x, 0.05f);
				ySmooth = Mathf.Lerp(ySmooth, y, 0.05f);

				xStrafeSmooth = Mathf.Lerp(xStrafeSmooth, xs, 0.05f);
				yStrafeSmooth = Mathf.Lerp(yStrafeSmooth, ys, 0.05f);
			}
			else
			{
				xSmooth = x;
				ySmooth = y;
				xStrafeSmooth = xs;
				yStrafeSmooth = ys;
			}				


			rotX += ySmooth*ySmoothSensitivity;
			rotX = ConversionUtils.ClampAngle(rotX, yMinLimit, yMaxLimit);
			Vector3 rot = rotYContainer.transform.localEulerAngles;
#if UNITY_IOS && !UNITY_EDITOR
			rot.x = -rotX;
#else
			rot.x = rotX;
#endif

			rotYContainer.transform.localEulerAngles = rot;
			rotXContainer.transform.Rotate(0, xSmooth*xSmoothSensitivity, 0, Space.Self);
			rotXContainer.transform.Translate (-xStrafeSmooth*xSmoothSensitivity, yStrafeSmooth*ySmoothSensitivity, 0, Space.Self);

			// mouse wheel
			distance -= zoomDelta;
			distance = Mathf.Clamp(distance, minDistance, maxDistance);

			Vector3 pos = camera.transform.localPosition;
			pos.z = -distance;
			//camera.transform.localPosition = pos;
			// move around instead of dollying the camera
			rotXContainer.transform.Translate(0,0,zoomDelta);
			
			// lerp all delta values back to zero for smooth easing
			posDelta.x = Mathf.Lerp(posDelta.x, 0f, 0.15f);
			posDelta.x = posDelta.x < 0.01f && posDelta.x > -0.01f ? 0 : posDelta.x;
			
			posDelta.y = Mathf.Lerp(posDelta.y, 0, 0.15f);
			posDelta.y = posDelta.y < 0.01f && posDelta.y > -0.01f ? 0 : posDelta.y;

			strafeDelta.x = Mathf.Lerp(strafeDelta.x, 0f, 0.15f);
			strafeDelta.x = strafeDelta.x < 0.01f && strafeDelta.x > -0.01f ? 0 : strafeDelta.x;
			
			strafeDelta.y = Mathf.Lerp(strafeDelta.y, 0, 0.15f);
			strafeDelta.y = strafeDelta.y < 0.01f && strafeDelta.y > -0.01f ? 0 : strafeDelta.y;
			
			zoomDelta = Mathf.Lerp(zoomDelta, 0, 0.05f);
			zoomDelta = zoomDelta < 0.01f && zoomDelta > -0.01f ? 0 : zoomDelta;
		}
		else if( camera != null && canUpdate )
		{
			Vector3 pos = camera.transform.localPosition;
			pos.z = -distance;
			camera.transform.localPosition = pos;
		}
	}
}

