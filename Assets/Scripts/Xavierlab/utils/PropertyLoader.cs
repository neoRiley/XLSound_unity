using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteInEditMode]
public class PropertyLoader : MonoBehaviour, IDragHandler
{
    private RectTransform rectTransform;


    private Vector3 movePos;

	void Start () 
    {
        Debug.Log("OnStart: " + transform.GetInstanceID());
        if( Application.isPlaying)
        {

        }
    }


    private void OnEnable()
    {

        //Debug.Log("OnEnable: " + transform.GetInstanceID() + ", isPlaying: " + Application.isPlaying + ", width: " + Screen.width + ", height: " + Screen.height + ", ratio: " + ((float)(Screen.height / Screen.width)).ToString());
        if (!Application.isPlaying) MoveMe();
    }


    private void MoveMe()
    {
        rectTransform = GetComponent<RectTransform>();
        Vector3 pos = rectTransform.anchoredPosition;

        if ( PlayerPrefs.HasKey(transform.GetInstanceID().ToString() + "_rectTransform_x"))
        {
            pos.x = PlayerPrefs.GetFloat(transform.GetInstanceID().ToString() + "_rectTransform_x");
            PlayerPrefs.DeleteKey(transform.GetInstanceID().ToString() + "_rectTransform_x");
        }

        if (PlayerPrefs.HasKey(transform.GetInstanceID().ToString() + "_rectTransform_y"))
        {
            pos.y = PlayerPrefs.GetFloat(transform.GetInstanceID().ToString() + "_rectTransform_y");
            PlayerPrefs.DeleteKey(transform.GetInstanceID().ToString() + "_rectTransform_y");
        }

        rectTransform.anchoredPosition = pos;
    }


    private void DragMe(Vector2 originalPress, Vector3 dragAmt, Vector2 newPos)
    {
        float scale = 3840.0f / Screen.width;
        rectTransform = GetComponent<RectTransform>();
        Vector3 pos = rectTransform.anchoredPosition;
        pos.x += dragAmt.x * scale;
        pos.y += dragAmt.y * scale;
        rectTransform.anchoredPosition = pos;

        PlayerPrefs.SetFloat(transform.GetInstanceID().ToString() + "_rectTransform_x", rectTransform.anchoredPosition.x);
        PlayerPrefs.SetFloat(transform.GetInstanceID().ToString() + "_rectTransform_y", rectTransform.anchoredPosition.y);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("Dragging: " + eventData.pressPosition + ", delta:  " + eventData.delta + ", " + eventData.position);
        DragMe(eventData.pressPosition, eventData.delta, eventData.position);
    }
}
