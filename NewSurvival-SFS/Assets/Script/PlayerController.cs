using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerController : MonoBehaviour {
    ////public float forwardSpeed = 10;
    ////public float backwardSpeed = 8;
    ////public float rotationSpeed = 40;

    ////// Dirty flag for checking if movement was made or not
    ////public bool MovementDirty { get; set; }

    ////void Start()
    ////{
    ////    MovementDirty = false;
    ////}

    ////void Update()
    ////{
    ////    // Forward/backward makes player model move
    ////    float translation = Input.GetAxis("Vertical");
    ////    if (translation != 0)
    ////    {
    ////        this.transform.Translate(0, 0, translation * Time.deltaTime * forwardSpeed);
    ////        MovementDirty = true;
    ////    }

    ////    // Left/right makes player model rotate around own axis
    ////    float rotation = Input.GetAxis("Horizontal");
    ////    if (rotation != 0)
    ////    {
    ////        this.transform.Rotate(Vector3.up, rotation * Time.deltaTime * rotationSpeed);
    ////        MovementDirty = true;
    ////    }
    ////}
    public float MovementSpeed = 6;
    Vector3 Movement;
    Rigidbody PlayerRigidbody;
    Animator Anim;
#if !MOBILE_INPUT
    //int FloorMask;                      // A layer mask so that a ray can be cast just at gameobjects on the floor layer.
    float CamRayLength = 100f;          // The length of the ray from the camera into the scene.
#endif
    public bool MovementDirty { get; set; }
    void Start()
    {
        MovementDirty = false;
    }
    void Awake()
    {
        //FloorMask = LayerMask.GetMask("Floor");
        Anim = GetComponent<Animator>();
        PlayerRigidbody = GetComponent<Rigidbody>();
    }
    void Update()
    {

    }
    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Move(h, v);
        Turning();
        Animating(h, v);
    }
    void Move(float h, float v)
    {
        Movement.Set(h, 0f, v);
        Movement = Movement.normalized * MovementSpeed * Time.deltaTime;
        PlayerRigidbody.MovePosition(transform.position + Movement);
        MovementDirty = true;
    }
    void Turning()
    {
#if !MOBILE_INPUT
        Ray CamRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit FloorHit;
        if (Physics.Raycast(CamRay, out FloorHit, CamRayLength))//, FloorMask
        {
            Vector3 playerToMouse = FloorHit.point - transform.position;
            playerToMouse.y = 0f;
            Quaternion NewRotation = Quaternion.LookRotation(playerToMouse);
            PlayerRigidbody.MoveRotation(NewRotation);
            MovementDirty = true;
        }
#endif
    }
    void Animating(float h, float v)
    {
        bool walking = h != 0 || v != 0;
        Anim.SetBool("IsWalking", walking);
    }
}
