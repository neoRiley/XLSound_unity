using UnityEngine;
using System.Collections;

public class TargetClickHandler : MonoBehaviour {


	void Start () {
	
	}

	public void OnMouseUpAsButton()
	{
		RotationCamera.instance.SetTarget(transform.position);
	}

}
