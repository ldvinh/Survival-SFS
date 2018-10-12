using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CameraController : MonoBehaviour {
    public Transform Target = null;
    public float Smoothing = 10.0f;
    Vector3 Offset;
    void Start() {
        if (Target != null)
        {
            Offset = transform.position - Target.position;
        }

    }
    void Update() {
        Vector3 targetCamPos = Target.position + Offset;
        transform.position = Vector3.Lerp(transform.position, targetCamPos, Smoothing * Time.deltaTime);
    }

}
