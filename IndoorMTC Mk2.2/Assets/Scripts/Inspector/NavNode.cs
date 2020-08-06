using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NavNode : MonoBehaviour
{
    GameObject controller;
    float stateSize;
    Navigation navigation;
    IndoorGMLParser parser;
    Material startMaterial;
    Material endMaterial;
    Material currentMaterial;

    private void Start()
    {
        parser = GameObject.FindGameObjectWithTag("Controller").GetComponent<IndoorGMLParser>();
        controller = GameObject.FindGameObjectWithTag("Controller");
        stateSize = controller.GetComponent<IndoorGMLParser>().stateSize;
        navigation = controller.GetComponent<Navigation>();
        startMaterial = Resources.Load("Materials/State_START", typeof(Material)) as Material;
        endMaterial = Resources.Load("Materials/State_TARGET", typeof(Material)) as Material;
    }

    [SerializeField]
    protected List<NavNode> m_Connections = new List<NavNode>(); //connectet Nodes

    private void OnMouseDown()
    {
        if (navigation.mapChoiceEnabled)
        {

            if (!navigation.startDefined) //Choose Color for Start/End
            { currentMaterial = startMaterial; }
            else            
            { currentMaterial = endMaterial; }


           GetComponent<Renderer>().material = currentMaterial; 

            foreach (string[] machinestate in parser.machineStates)
            {
                if (machinestate[1] == gameObject.name)
                {
                    foreach (GameObject machine in parser.machines)
                    {
                        if (machine.name == machinestate[0])
                        {
                            machine.GetComponent<Renderer>().material = currentMaterial;
                            break;
                        }
                    }
                    break;
                }
            }
            navigation.SetStartEnd(gameObject);
            transform.localScale = Vector3.one * stateSize * 1.3f;
        }
    }

    public virtual List<NavNode> Connections
    { get { return m_Connections;  } }

    public NavNode this[int index]
    { get { return m_Connections[index]; } }

}
