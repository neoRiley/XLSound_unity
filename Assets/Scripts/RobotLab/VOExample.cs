using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections;
using XavierLab;

public class VOExample : MonoBehaviour
{
    public Texture SilentMB;
    public Texture STCh;
    public Texture E;
    public Texture AAh;
    public Texture UR;
    public Texture Ooh;
    public Texture LD;
    public Texture FV;

    public BoxCollider collider;

    MeshRenderer renderer;
    Dictionary<VOPositions, Texture> voImages;

    // Use this for initialization
    void Start()
    {
        renderer = GetComponent<MeshRenderer>();
        voImages = new Dictionary<VOPositions, Texture>
        {
            { VOPositions.AAh, AAh },
            { VOPositions.E, E },
            { VOPositions.FV, FV },
            { VOPositions.LD, LD },
            { VOPositions.Ooh, Ooh },
            { VOPositions.SilentMB, SilentMB },
            { VOPositions.STCh, STCh },
            { VOPositions.UR, UR }
        };

        renderer.sharedMaterial.mainTexture = voImages[VOPositions.SilentMB];
    }

    
    public virtual void OnMouseUpAsButton()
    {
        XLSound.PlayVOSound(Sounds.AttractVO_havingAGoodDay_v1, (VOPositions pos) =>
        {
            L.Log(LogEventType.EVENT, $"VO event: {pos}");
            renderer.enabled = true;
            renderer.sharedMaterial.mainTexture = voImages[pos];
        }, () =>
        {
            L.Log(LogEventType.STRING, $"VO Completed");
            //renderer.enabled = false;
        });
    }
}
