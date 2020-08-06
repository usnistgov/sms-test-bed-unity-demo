using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float walkingSpeed;
    public float walkingSpeedFast;
    public float turnSpeed;
    public List<string[]> roomBoundaries = new List<string[]>(); //[room; door]
    public string currentRoomId;
    
    bool teleport;
    Vector3 teleportPosition;
    float yaw;
    float speed;
    string newDoorName;
    CameraController cameraController;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        cameraController = Camera.main.GetComponent<CameraController>();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift)) //run
            speed = walkingSpeedFast * Time.deltaTime;
        else //walk
            speed = walkingSpeed * Time.deltaTime;

        if (cameraController.firstPersonView && cameraController.moving == false && Input.GetMouseButton(1)) //turning
        {
            yaw += turnSpeed * Input.GetAxis("Mouse X");
            transform.eulerAngles = new Vector3(transform.localRotation.x, yaw, 0);
        }

        if (Input.GetKey(KeyCode.E) && teleport) //teleport = true when near a door
        {
            transform.position = teleportPosition;

            foreach (string[] possibledoors in roomBoundaries)//renew Room ID after teleporting through door
            {
                if (possibledoors[1] == newDoorName)
                {
                    currentRoomId = possibledoors[0];
                    break;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (cameraController.firstPersonView && cameraController.moving == false)
        {
            //Movement
            if (Input.GetKey(KeyCode.W))
                gameObject.transform.position += transform.forward * speed;
            if (Input.GetKey(KeyCode.A))
                gameObject.transform.position += -transform.right * speed;
            if (Input.GetKey(KeyCode.S))
                gameObject.transform.position += -transform.forward * speed;
            if (Input.GetKey(KeyCode.D))
                gameObject.transform.position += transform.right * speed;
        }
    }

    private void OnTriggerEnter(Collider door)
    {
        foreach (string[] possibledoors in roomBoundaries)
        {
            if (possibledoors[0] == currentRoomId && "#" + door.name == possibledoors[1])
            {
                teleport = true;

                Mesh mesh = door.GetComponent<MeshFilter>().mesh;
                Vector3[] normals = mesh.normals;
                teleportPosition = - normals[0] * 0.5f + transform.position;


                if (door.name.Contains("-REVERSE"))
                { 
                    newDoorName = "#" + door.name.Remove(door.name.Length - 8);
                }
                else
                {
                    newDoorName = "#" + door.name + "-REVERSE";
                }
                break;
            }
        }
    }

    private void OnTriggerExit(Collider door)
    {
        teleport = false;
    }
}
