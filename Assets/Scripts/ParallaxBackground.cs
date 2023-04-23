using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ParallaxBackground : MonoBehaviour
{
    public float parallaxSpeed = 0.02f;
    private RawImage background;
    private Transform cam;
    private float initPos;

    // Start is called before the first frame update
    void Start()
    {
        background = GetComponent<RawImage>();
        cam = Camera.main.transform;
        initPos = cam.position.x;
    }

    // Update is called once per frame
    void Update()
    {   
        float dist = (cam.position.x - initPos)/50;
        float finalSpeed = parallaxSpeed*dist;
        background.uvRect = new Rect(finalSpeed,0,1,1);
    }
}
