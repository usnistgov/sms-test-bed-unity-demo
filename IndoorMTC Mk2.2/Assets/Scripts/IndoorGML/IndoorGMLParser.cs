using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IndoorGMLParser : MonoBehaviour
{
    public List<GameObject> workshopObjects;
    public List<GameObject> states = new List<GameObject>();
    public List<GameObject> transitions = new List<GameObject>();
    public List<string[]> transitionsConnectionList = new List<string[]>(); //[transition name, state name (1), state name (2), length)
    public List<GameObject> machines = new List<GameObject>();
    public List<string[]> machineRefs = new List<string[]>(); //[machine name, type, reference name, filepath]
    public List<string[]> machineStates = new List<string[]>(); //[machine, state]


    public GameObject player;
    public GameObject startText;
    public GameObject uIController;
    public Dropdown[] machineChoiceOption = new Dropdown[3];
    public static Bounds sceneBound;
    public float stateSize;
    public float lineWidth;
    public bool inspection;

    string refFileURL;
    string fileUrl;
    static private List<List<Vector3>> outLines;
    Material lineMaterial;
    UIController uiController;
    Navigation navigation;

    private void Start()
    {
        Application.targetFrameRate = 60;
        uiController = uIController.GetComponent<UIController>();
        navigation = GameObject.FindGameObjectWithTag("Controller").GetComponent<Navigation>();

        inspection = (SceneManager.GetActiveScene().name == "IndoorMTC Inspection");
    }

    public void Load(string filePath)
    {
        //Clear all Lists for new input
        workshopObjects.Clear();
        states.Clear();
        transitions.Clear();
        transitionsConnectionList.Clear();
        machineRefs.Clear();
        machines.Clear();
        machineStates.Clear();

        fileUrl = filePath;

        if (inspection)
        {
            //initialize Navigation script
            navigation.startDefined = false;
            navigation.myStart = null;
            navigation.myEnd = null;
            navigation.mapChoiceEnabled = false;

            uiController.InspectionUIEnabler(true); //activate GUI Buttons "Navigation" & "Machine"
        }
        else
        {
            uiController.IOSwitchFirstPersonButton(false);
        }

        startText.SetActive(false);

        sceneBound = new Bounds();
        outLines = new List<List<Vector3>>();


#pragma warning disable IDE0063 // Use simple 'using' statement
        using (XmlReader reader = XmlReader.Create(fileUrl))
#pragma warning restore IDE0063 // Use simple 'using' statement

            while (reader.Read())
            {
                if (IsStartElement(reader, "connection")) //get MTConnect URL and start gathering data
                {
                    GetComponent<MTConnect>().mtcURL = reader.ReadInnerXml();
                    StartCoroutine(GetComponent<MTConnect>().GetText());
                }

                if (IsStartElement(reader, "human")) //read position of human
                {
                    while (IsEndElement(reader, "human") == false)
                    {
                        reader.Read();

                        if (!inspection) //no representation of Human in Inspection scene
                        {
                            if (IsStartElement(reader, "pos"))
                            {
                                string[] playerPosVector3 = reader.ReadInnerXml().Trim().Split(' ');
                                player.transform.position = new Vector3(float.Parse(playerPosVector3[0]), float.Parse(playerPosVector3[2]), float.Parse(playerPosVector3[1]));

                                uiController.IOSwitchFirstPersonButton(true);
                            }

                            if (IsStartElement(reader, "room")) //id of current room of human
                            {
                                player.GetComponent<PlayerController>().currentRoomId = reader.GetAttribute("roomId");
                            }
                        }
                    }
                }

                if (IsStartElement(reader, "cellSpaceBoundaryMember")) //doors
                    OnCellSpaceBoundaryMember(reader);

                if (IsStartElement(reader, "cellSpaceMember")) //rooms
                    OnCellSpace(reader);

                if (inspection)
                {
                    if (IsStartElement(reader, "stateMember")) //states
                        OnState(reader);

                    if (IsStartElement(reader, "transitionMember")) //transitions
                        OnTransition(reader);
                }
            }
    }

    #region read GML data
    private void OnTransition(XmlReader reader) //read transition data
    {
        string[] localTransitionConnection = new string[4];
        int i = 1;
        List<Vector3> localLinePositions = new List<Vector3>();

        while (IsEndElement(reader, "transitionMember") == false)
        {
            reader.Read();
            if (string.IsNullOrWhiteSpace(localTransitionConnection[0]))
            {
                reader.Read();
                localTransitionConnection[0] = reader.GetAttribute("gml:id");
            }

            if (IsStartElement(reader, "connects"))
            {
                localTransitionConnection[i] = reader.GetAttribute("xlink:href").TrimStart('#');
                i++;
            }

            if (IsStartElement(reader, "pos"))
            {
                reader.Read();

                Vector3 unityVector3d = GetPos3D(reader);
                localLinePositions.Add(unityVector3d);
            }
            else if (IsStartElement(reader, "posList"))
            {
                reader.Read();
                localLinePositions = GetPosList3D(reader);
            }
        }

        //setup Line in Unity
        GameObject transition = new GameObject();
        var lineRenderer = transition.AddComponent<LineRenderer>();
        lineRenderer.positionCount = localLinePositions.Count();
        lineRenderer.SetPositions(localLinePositions.ToArray());
        lineRenderer.useWorldSpace = false;
        lineRenderer.tag = CommonObjs.TAG_TRANSITION;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        transition.name = localTransitionConnection[0];
        transition.transform.parent = CommonObjs.gmlRootTransition.transform;

        //set width
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        //set color
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;

        lineRenderer.numCapVertices = 5; //round ends

        transitions.Add(transition);

        //go trough list of states and set connected state
        foreach (GameObject fromState in states)
        {
            if (fromState.name == localTransitionConnection[1])
            {
                foreach (GameObject toState in states)
                {
                    if (toState.name == localTransitionConnection[2])
                    {
                        fromState.GetComponent<NavNode>().Connections.Add(toState.GetComponent<NavNode>());
                        break;
                    }
                }
                break;
            }
        }

        localTransitionConnection[3] = (localLinePositions[0] - localLinePositions[1]).magnitude.ToString(); //calculate length
        transitionsConnectionList.Add(localTransitionConnection); //add transition to list
    }

    private void OnState(XmlReader reader) //read state info
    {
        string localName = null;

        while (IsEndElement(reader, "stateMember") == false)
        {
            reader.Read();

            //read name of state
            if (string.IsNullOrWhiteSpace(localName))
            {
                reader.Read();
                localName = reader.GetAttribute("gml:id");
            }

            //read position of state
            if (IsStartElement(reader, "pos"))
            {
                reader.Read();

                Vector3 unityVector3d = GetPos3D(reader);

                //create Object in Unity
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = unityVector3d;
                sphere.transform.localScale = new Vector3(1, 1, 1) * stateSize;
                sphere.name = localName;
                sphere.tag = CommonObjs.TAG_STATE;
                sphere.transform.parent = CommonObjs.gmlRootState.transform;

                //add NavNode Script
                Assembly asm = Assembly.Load("Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                Type type = asm.GetType("NavNode");
                sphere.AddComponent(type);

                states.Add(sphere); //add state to list
            }
        }
    }

    private void OnCellSpace(XmlReader reader) //read room data
    {
        GameObject solid = new GameObject();

        string cellname = null;
        string localType = null;
        GameObject genPolygon;
        bool isWorkshop = false;


        while (IsEndElement(reader, "cellSpaceMember") == false)
        {
            reader.Read();

            if (string.IsNullOrWhiteSpace(localType))
            {
                reader.Read();
                localType = reader.GetAttribute("gml:id");

                if (localType.Equals("TransitionSpace"))
                {
                    solid.tag = CommonObjs.TAG_TRANSITIONSPACE;
                    solid.transform.parent = CommonObjs.gmlRootTransitionSpace.transform;
                }
                else if (localType.Equals("GeneralSpace"))
                {
                    solid.tag = CommonObjs.TAG_GENERALSPACE;
                    solid.transform.parent = CommonObjs.gmlRootGeneralSpace.transform;
                }
                else
                {
                    solid.tag = CommonObjs.TAG_CELLSPACE;
                    solid.transform.parent = CommonObjs.gmlRootCellSpace.transform;
                }
            }

            if (IsStartElement(reader, "name"))
            {
                cellname = reader.ReadInnerXml();
                isWorkshop = cellname.Contains("Workshop");
            }

            if (IsStartElement(reader, "Solid"))
            {
                int faceCnt = 1;
                solid.name = cellname;

                reader.Read();
                while (IsEndElement(reader, "Solid") == false)
                {

                    reader.Read();
                    if (IsStartElement(reader, "Polygon") || IsStartElement(reader, "PolygonPatch"))
                    {
                        var polygon = OnPolygon(reader);
                        genPolygon = Poly2Mesh.CreateGameObject(polygon, isWorkshop);

                        genPolygon.name = string.Format("{0}_Face:{1}", localType, faceCnt++);

                        genPolygon.transform.parent = solid.transform;

                        if (localType.Equals("TransitionSpace"))
                        {
                            genPolygon.GetComponent<Renderer>().material = CommonObjs.machineFront;
                        }
                        else if (localType.Equals("GeneralSpace"))
                        {
                            genPolygon.GetComponent<Renderer>().material = CommonObjs.materialGeneralSpace;
                        }
                        if (isWorkshop)
                        {
                            genPolygon.GetComponent<Renderer>().material = CommonObjs.materialWorkshop;
                            workshopObjects.Add(genPolygon);
                        }
                        else
                        {
                            genPolygon.GetComponent<Renderer>().material = CommonObjs.materialCellSpace;
                        }

                    }

                    #region machines 

                    if (IsStartElement(reader, "Objects"))
                    {
                        while (IsEndElement(reader, "Objects") == false)
                        {
                            reader.Read();


                            if (IsStartElement(reader, "Machine")) //read machine data
                            {
                                string machineName = reader.GetAttribute("name");
                                string[] machineState = new string[2];

                                if (reader.GetAttribute("extRef") == "true")
                                {
                                    reader.Read();
                                    Vector3 machinePosition = new Vector3();

                                    string machineFileURL = fileUrl;
                                    string machineFileName;

                                    machineFileURL = machineFileURL.Substring(8);
                                    string last = machineFileURL[machineFileURL.Length - 1].ToString();
                                    float machineRotation = 0;

                                    while (last != @"/") //delete IndoorGML Filename from URL
                                    {
                                        machineFileURL = machineFileURL.TrimEnd(machineFileURL[machineFileURL.Length - 1]);
                                        last = machineFileURL[machineFileURL.Length - 1].ToString();
                                    }

                                    while (IsEndElement(reader, "Machine") == false)
                                    {
                                        reader.Read();

                                        if (IsStartElement(reader, "ref"))
                                        {
                                            while (IsEndElement(reader, "ref") == false)
                                            {
                                                reader.Read();

                                                if (IsStartElement(reader, "objectfile"))
                                                {
                                                    machineFileName = reader.GetAttribute("file");
                                                    refFileURL = machineFileURL + machineFileName + "/";
                                                    machineFileURL += machineFileName + "/" + machineFileName + ".obj";

                                                }

                                                if (IsStartElement(reader, "picture"))
                                                {
                                                    machineRefs.Add(new string[4] { machineName, "picture", null, refFileURL + reader.GetAttribute("file") });
                                                }

                                                if (IsStartElement(reader, "documents"))
                                                {
                                                    while (IsEndElement(reader, "documents") == false)
                                                    {
                                                        reader.Read();

                                                        if (IsStartElement(reader, "document"))
                                                        {
                                                            machineRefs.Add(new string[4] { machineName, "document", reader.GetAttribute("name"), refFileURL + reader.GetAttribute("file") });
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (IsStartElement(reader, "pos"))
                                        {
                                            reader.Read();
                                            machinePosition = GetPos3D(reader);
                                        }

                                        if (IsStartElement(reader, "rot"))
                                        {
                                            float.TryParse(reader.ReadInnerXml(), out machineRotation);
                                        }

                                        if (IsStartElement(reader, "partialboundedBy"))
                                        {
                                            machineState[1] = reader.GetAttribute("xlink:href");
                                        }
                                    }

                                    GameObject machineObj = new GameObject { name = machineName };

                                    machineObj.AddComponent(typeof(MeshRenderer));
                                    MeshFilter filter = machineObj.AddComponent(typeof(MeshFilter)) as MeshFilter;
                                    ObjImporter importer = new ObjImporter();
                                    Mesh mesh = importer.ImportFile(machineFileURL);
                                    filter.mesh = mesh;
                                    mesh.RecalculateNormals();

                                    machineObj.AddComponent(typeof(MeshCollider));
                                    machineObj.GetComponent<MeshCollider>().sharedMesh = mesh;
                                    machineObj.GetComponent<MeshCollider>().convex = true;
                                    machineObj.transform.position = machinePosition;
                                    machineObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                                    machineObj.transform.localEulerAngles = new Vector3(0, machineRotation, 0);

                                    machineObj.transform.parent = solid.transform;
                                    Assembly asm = Assembly.Load("Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                                    Type type = asm.GetType("MachineStatus");
                                    machineObj.AddComponent(type);
                                    machines.Add(machineObj);

                                    if (inspection)
                                    {
                                        machineState[0] = machineName;
                                        machineStates.Add(machineState);
                                        machineObj.GetComponent<Renderer>().material = Resources.Load("Materials/Machine_INSPECTION", typeof(Material)) as Material;

                                        foreach (Dropdown dropdown in machineChoiceOption)
                                        {
                                            dropdown.options.Add(new Dropdown.OptionData() { text = machineName });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
            }

            if (IsStartElement(reader, "partialboundedBy"))
            {
                string[] boundedBy = new string[2];
                boundedBy[0] = localType;
                boundedBy[1] = reader.GetAttribute("xlink:href");

                if (!inspection)
                {
                    player.GetComponent<PlayerController>().roomBoundaries.Add(boundedBy);
                }
            }
        }
    }

    private void OnCellSpaceBoundaryMember(XmlReader reader)
    {
        List<Vector2> localUVs = new List<Vector2>();
        Poly2Mesh.Polygon localPolygon = new Poly2Mesh.Polygon();
        string localFileName = string.Empty;
        string localName = string.Empty;
        while (IsEndElement(reader, "cellSpaceBoundaryMember") == false)
        {
            reader.Read();

            if (string.IsNullOrWhiteSpace(localName))
            {
                reader.Read();
                localName = reader.GetAttribute("gml:id");
            }

            if (IsStartElement(reader, "Polygon") || IsStartElement(reader, "PolygonPatch"))
            {
                localPolygon = OnPolygon(reader);
            }

            if (IsStartElement(reader, "TextureImage"))
            {
                reader.Read();
                localFileName = reader.Value;
            }

            if (IsStartElement(reader, "TextureCoordinate"))
            {
                localUVs = new List<Vector2>();
                while (IsEndElement(reader, "TextureCoordinate") == false)
                {
                    reader.Read();

                    if (IsStartElement(reader, "pos"))
                    {
                        reader.Read();

                        localUVs.Add(GetPos2D(reader));
                    }
                }
            }
        }


        if (localUVs.Count() > 2)
        {
            localPolygon.outsideUVs = localUVs;
            localPolygon.holesUVs = new List<List<Vector2>>();
            localPolygon.outsideUVs.Reverse();
        }

        localPolygon.outside.Reverse();

        for (int i = 0; i < localPolygon.holes.Count(); i++)
        {
            localPolygon.holes[i].Reverse();
            localPolygon.holesUVs.Add(new List<Vector2>());
        }

        // Texture 구멍 무시.
        if (string.IsNullOrWhiteSpace(localFileName) == false)
        {
            localPolygon.holes = new List<List<Vector3>>();
            localPolygon.holesUVs = new List<List<Vector2>>();
        }

        GameObject cellSpaceBoundary = Poly2Mesh.CreateGameObject(localPolygon, true);
        cellSpaceBoundary.name = localName;
        cellSpaceBoundary.GetComponent<MeshCollider>().enabled = true;
        cellSpaceBoundary.GetComponent<MeshCollider>().convex = true;
        cellSpaceBoundary.GetComponent<MeshCollider>().isTrigger = true;


        if (string.IsNullOrWhiteSpace(localFileName))
        {
            cellSpaceBoundary.transform.Translate(localPolygon.planeNormal * 0.01f);
        }
        else
        {
            //cellSpaceBoundary.transform.Translate(localPolygon.planeNormal * 0.005f);
        }

        if (string.IsNullOrWhiteSpace(localFileName) == false)
        {
            //Debug.Log(GetDirectoryName() + "\\" + localFileName);
            IEnumerator tmpRunner = ApplyTexture(cellSpaceBoundary, Path.GetDirectoryName(fileUrl) + "\\" + localFileName);
            StartCoroutine(tmpRunner);
        }
        else
        {
            cellSpaceBoundary.GetComponent<Renderer>().material = CommonObjs.materialCellSpaceBoundary;
        }

        cellSpaceBoundary.tag = CommonObjs.TAG_CELLSPACEBOUNDARY;
        cellSpaceBoundary.transform.parent = CommonObjs.gmlRootCellSpaceBoundary.transform;

    }
    #endregion

    #region helping methods
    IEnumerator ApplyTexture(GameObject target, string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        try
        {
            Texture myTexture = DownloadHandlerTexture.GetContent(www);
            target.GetComponent<Renderer>().material = CommonObjs.materialTextureSurface;
            target.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", myTexture);
        }
        catch
        {
            Debug.Log("ERROR File: " + url);
        }
    }

    public void OnRenderObject()
    {
        CreateLineMaterial();
        // Apply the line material
        lineMaterial.SetPass(0);

        if (outLines == null || outLines.Count() == 0)
            return;

        GL.PushMatrix();
        // Set transformation matrix for drawing to
        // match our transform
        GL.MultMatrix(transform.localToWorldMatrix);

        // Draw lines
        for (int i = 0; i < outLines.Count(); ++i)
        {
            GL.Begin(GL.LINES);
            for (int j = 0; j < outLines[i].Count() - 1; j++)
            {
                GL.Vertex3(outLines[i][j].x, outLines[i][j].y, outLines[i][j].z);
                GL.Vertex3(outLines[i][j + 1].x, outLines[i][j + 1].y, outLines[i][j + 1].z);
            }
            GL.End();
        }
        GL.PopMatrix();
    }

    void CreateLineMaterial()
    {
        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
            lineMaterial.color = new Color(0, 0, 0);
        }
    }

    bool IsStartElement(XmlReader reader, string tag)
    {
        return reader.IsStartElement() && reader.LocalName.Equals(tag);
    }

    bool IsEndElement(XmlReader reader, string tag)
    {
        return reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals(tag);
    }

    private Poly2Mesh.Polygon OnPolygon(XmlReader reader)
    {
        List<Vector3> localOutlines = new List<Vector3>();
        List<List<Vector3>> localHoles = new List<List<Vector3>>();

        while (IsEndElement(reader, "Polygon") == false && IsEndElement(reader, "PolygonPatch") == false)
        {
            reader.Read();

            if (IsStartElement(reader, "exterior"))
            {
                // pos  localExterior  exterior
                localOutlines = new List<Vector3>();
                while (IsEndElement(reader, "exterior") == false)
                {
                    reader.Read();

                    if (IsStartElement(reader, "pos"))
                    {
                        reader.Read();

                        Vector3 unityVector3d = GetPos3D(reader);

                        localOutlines.Add(unityVector3d);
                    }
                    else if (IsStartElement(reader, "posList"))
                    {
                        reader.Read();
                        localOutlines = GetPosList3D(reader);
                    }
                }
            }

            if (IsStartElement(reader, "interior"))
            {
                localHoles.Add(new List<Vector3>());
                while (IsEndElement(reader, "interior") == false)
                {
                    reader.Read();
                    if (IsStartElement(reader, "pos"))
                    {
                        reader.Read();

                        Vector3 unityVector3d = GetPos3D(reader);

                        localHoles.Last().Add(unityVector3d);
                    }
                    else if (IsStartElement(reader, "posList"))
                    {
                        reader.Read();
                        int lastHoleIdx = localHoles.Count;
                        localHoles[lastHoleIdx - 1] = GetPosList3D(reader);
                    }
                }
            }
        }

        Poly2Mesh.Polygon polygon = new Poly2Mesh.Polygon
        {
            outside = localOutlines,
            holes = localHoles
        };

        outLines.Add(localOutlines);

        for (int i = 0; i < localHoles.Count(); i++)
        {
            outLines.Add(localHoles[i]);
        }

        return polygon;
    }

    private static Vector3 GetPos2D(XmlReader reader)
    {
        string[] gmlVector3d = reader.Value.Trim().Split(' ');
        Vector2 unityVector2d = new Vector2();

        // Unity3D Vector Style.
        float.TryParse(gmlVector3d[0], out unityVector2d.x);
        float.TryParse(gmlVector3d[1], out unityVector2d.y);
        return unityVector2d;
    }

    private List<Vector3> GetPosList3D(XmlReader reader)
    {
        string[] gmlVector3d = reader.Value.Trim().Split(' ');
        List<Vector3> vector3s = new List<Vector3>();
        for (int i = 0; i < gmlVector3d.Length; i += 3)
        {
            string[] vs = new string[3];
            vs[0] = gmlVector3d[i];
            vs[1] = gmlVector3d[i + 1];
            vs[2] = gmlVector3d[i + 2];

            vector3s.Add(GetPos3DCore(vs));
        }

        return vector3s;
    }

    private Vector3 GetPos3D(XmlReader reader)
    {
        string[] gmlVector3d = reader.Value.Trim().Split(' ');
        Vector3 relativeUnityVector3d = GetPos3DCore(gmlVector3d);

        return relativeUnityVector3d;
    }

    private Vector3 GetPos3DCore(string[] gmlVector3d)
    {
        double.TryParse(gmlVector3d[0], out double unityVectorX);
        double.TryParse(gmlVector3d[1], out double unityVectorZ);
        double.TryParse(gmlVector3d[2], out double unityVectorY);

        double deltaX = unityVectorX;
        double deltaY = unityVectorY;
        double deltaZ = unityVectorZ;

        Vector3 scaledVector = new Vector3(Convert.ToSingle(deltaX),
            Convert.ToSingle(deltaY),
            Convert.ToSingle(deltaZ));

        sceneBound.Encapsulate(scaledVector);
        return scaledVector;
    }

    #endregion
}
