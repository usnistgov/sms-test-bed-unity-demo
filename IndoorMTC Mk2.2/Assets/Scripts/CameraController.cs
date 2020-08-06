using System;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    public float turnSpeed;     
    public float panSpeed;      
    public float panSpeedMouse;
    public float zoomSpeed;      
    public float zoomSpeedMouse;
    public float zoomSpeedMouseOrtho;
    public float autoRotSpeed;
    public float autoLinSpeed;

    public bool cameraActive = true;
    public Transform player;
    public Transform standardCamera;
    public GameObject cameraMovementPanel;
    public bool orthographic;

    Vector3 pos;
    float yaw;
    float pitch;
    float scroll;
    public bool moving;
    public bool firstPersonView;
    public Quaternion targetRot;
    public Vector3 targetPos;
    public Vector3 lastCameraPos;
    public Quaternion lastCameraRot;
    Vector3 targetDir;
    GameObject controller;


    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        controller = GameObject.FindGameObjectWithTag("Controller");        
    }

    void Update()
    {
        if (cameraActive)
        {
            orthographic = GetComponent<Camera>().orthographic;

            scroll = 0;

            if (gameObject.transform.localRotation == targetRot && gameObject.transform.localPosition == targetPos)
            {
                moving = false;
                yaw = transform.eulerAngles.y;
                pitch = transform.eulerAngles.x;
            }

            if (moving)
            {
                var stepRot = autoRotSpeed * Time.deltaTime;
                var stepLinear = autoLinSpeed * Time.deltaTime;
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRot, stepRot);

                gameObject.transform.localPosition = Vector3.MoveTowards(gameObject.transform.localPosition, targetPos, stepLinear);
            }
            else
            {

                if (Input.GetMouseButton(1) && orthographic == false)
                {
                    pitch -= turnSpeed * Input.GetAxis("Mouse Y");
                }

                if (firstPersonView)
                {
                    transform.localEulerAngles = new Vector3(pitch, 0, 0);
                }


                else
                {
                    //ROTATING
                    if (Input.GetMouseButton(1) && orthographic == false)
                    {
                        yaw += turnSpeed * Input.GetAxis("Mouse X");
                        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
                    }

                    //PANNING

                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        pos.y += panSpeed * Time.deltaTime;
                    }

                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        pos.y -= panSpeed * Time.deltaTime;
                    }

                    if (Input.GetKey(KeyCode.D))
                    {
                        pos.x += panSpeed * Time.deltaTime;
                    }

                    if (Input.GetKey(KeyCode.A))
                    {
                        pos.x -= panSpeed * Time.deltaTime;
                    }


                    if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
                    { pos.x = 0; }


                    Vector3 zoom = new Vector3();
                    if (orthographic)
                    {
                        if (Input.GetKey(KeyCode.W))
                        { pos.y += panSpeed * Time.deltaTime; }
                        if (Input.GetKey(KeyCode.S))
                        { pos.y -= panSpeed * Time.deltaTime; }
                        scroll = -Input.GetAxis("Mouse ScrollWheel") * zoomSpeedMouseOrtho;
                        GetComponent<Camera>().orthographicSize += scroll;
                    }
                    else
                    {
                        if (Input.GetKey(KeyCode.W))
                        { scroll = 0.1f; }
                        if (Input.GetKey(KeyCode.S))
                        { scroll = -0.1f; }

                        //ZOOM
                        scroll += Input.GetAxis("Mouse ScrollWheel") * zoomSpeedMouse;
                        zoom = scroll * zoomSpeed * transform.forward;

                    }

                    if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
                    { pos.y = 0; }

                    if (Input.GetMouseButton(2))
                    {
                            pos.x -= panSpeedMouse * Input.GetAxis("Mouse X");
                            pos.y -= panSpeedMouse * Input.GetAxis("Mouse Y");
                    }

                    transform.Translate(pos, Space.Self);
                    transform.Translate(zoom, Space.World);
                }
            }
        }
    }


    public void SwitchCamera()
    {
        firstPersonView = !firstPersonView;

        if (firstPersonView)
        {
            cameraMovementPanel.SetActive(false);
            GetComponent<Camera>().orthographic = false;
            lastCameraPos = gameObject.transform.position;
            lastCameraRot = gameObject.transform.rotation;

            gameObject.transform.parent = player.transform;

            targetPos = new Vector3(0, 1.7f, 0);
            targetRot = Quaternion.Euler(0, 0, 0);
            moving = true;

            foreach (GameObject workshopface in controller.GetComponent<IndoorGMLParser>().workshopObjects)
            {
                workshopface.GetComponent<MeshCollider>().enabled = true;
            }
            player.GetComponent<Rigidbody>().useGravity = true;
        }
        else
        {
            cameraMovementPanel.SetActive(true);
            gameObject.transform.parent = standardCamera.transform;
            targetPos = lastCameraPos;
            targetRot = lastCameraRot;
            moving = true;

            foreach (GameObject child in controller.GetComponent<IndoorGMLParser>().workshopObjects)
            {
                child.GetComponent<MeshCollider>().enabled = false;
            }
            player.GetComponent<Rigidbody>().useGravity = false;
        }
    }

    public void DoMoveViewPoint(int direction)
    {
        GetComponent<Camera>().orthographic = false;

        switch (direction)
        {
            case 1:
                targetDir = new Vector3(45, 45, 0);
                break;
            case 2:
                targetDir = new Vector3(0, 0, 0);
                break;
            case 3:
                targetDir = new Vector3(45, 315, 0);
                break;
            case 4:
                targetDir = new Vector3(0, 90, 0);
                break;
            case 5: //MAP
                targetDir = new Vector3(90, 0, 0);
                GetComponent<Camera>().orthographic = true;
                break;
            case 6:
                targetDir = new Vector3(0, 270, 0);
                break;
            case 7:
                targetDir = new Vector3(45, 135, 0);
                break;
            case 8:
                targetDir = new Vector3(0, 180, 0);
                break;
            case 9:
                targetDir = new Vector3(45, 225, 0);
                break;
            case 51:
                targetDir = new Vector3(90, 90, 0);
                break;
            case 52:
                targetDir = new Vector3(90, 180, 0);
                break;
            case 53:
                targetDir = new Vector3(90, 270, 0);
                break;
            default:
                targetDir = new Vector3(45, 45, 0);
                break;
        }

        float frustrumLength = Math.Max(IndoorGMLParser.sceneBound.size.z, IndoorGMLParser.sceneBound.size.x);
        if (direction == 5)
        {
            GetComponent<Camera>().orthographicSize = frustrumLength/1.8f;
        }

        frustrumLength = Math.Max(frustrumLength, IndoorGMLParser.sceneBound.size.y);

        float distance = frustrumLength * 0.6f / Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        targetPos = IndoorGMLParser.sceneBound.center - distance * (Quaternion.Euler(targetDir) * Vector3.forward);
        targetRot = Quaternion.Euler(targetDir);
        moving = true;
    }

}
