using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Navigation : MonoBehaviour
{
    public NavNode myStart;
    public NavNode myEnd;

    public bool startDefined;
    public bool mapChoiceEnabled;
    public Material startMaterial;
    public Material endMaterial;
    public Text lenghtText;

    NavPath myPath = new NavPath();
    UIController uiController;
    MachinePanelControl machinePanelControl;
    IndoorGMLParser parser;
    Material currentMaterial;

    private void Start()
    {
        parser = GameObject.FindGameObjectWithTag("Controller").GetComponent<IndoorGMLParser>();
        uiController = GameObject.FindGameObjectWithTag("UIController").GetComponent<UIController>();
        machinePanelControl = GameObject.FindGameObjectWithTag("UIController").GetComponent<MachinePanelControl>();
    }

    public void SetStartEnd(GameObject marker)
    {
        if (!startDefined)
        {
            myStart = marker.GetComponent<NavNode>();
            startDefined = true;
            uiController.NavigationPanelControl(1);
        }
        else
        {
            myEnd = marker.GetComponent<NavNode>();
            startDefined = false;
            StartPathCalculation();
            uiController.NavigationPanelControl(2);
        }
    }

    public void StartPathCalculation()
    {
        mapChoiceEnabled = false;
        myPath = Dijstraka(myStart, myEnd);

        lenghtText.text = "Length: " + Math.Round(myPath.totalDistance, 2).ToString() + "m";

        foreach (GameObject transition in parser.transitions) //deactivate all transitions
        {
            transition.SetActive(false);
        }
        foreach (GameObject state in parser.states)
        {
            state.SetActive(false);
        }

        myStart.gameObject.SetActive(true);
        myEnd.gameObject.SetActive(true);

        for (int node = 0; node < myPath.Nodes.Count - 1; node++) //search every combination of two nodes
        {
            string node1 = myPath.Nodes[node].ToString().Remove(myPath.Nodes[node].ToString().Length - 10); //shorten String because string would look like "S01 (NavNode)"
            string node2 = myPath.Nodes[node + 1].ToString().Remove(myPath.Nodes[node + 1].ToString().Length - 10);

            foreach (string[] transition in parser.transitionsConnectionList) //for every combination look inside transition list for the corresponding transition
            {
                if (transition[1] == node1 && transition[2] == node2) //get transition name
                {
                    foreach (GameObject transitionObject in parser.transitions) //get corresponding GameObject
                    {
                        if (transitionObject.name == transition[0])
                        {
                            transitionObject.SetActive(true);
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }

    public NavPath Dijstraka(NavNode start, NavNode end)
    {
        List<NavNode> allNodes = new List<NavNode>();

        foreach (GameObject state in parser.states) //Add NavNode Object to m_Nodes List
        {
            allNodes.Add(state.GetComponent<NavNode>());
        }

        if (start == null || end == null) //make sure start and end are defined
        {
            throw new ArgumentNullException();
        }

        NavPath path = new NavPath();

        if (start == end) //when start = end, than define path with only start-node
        {
            path.Nodes.Add(start);
            return path;
        }

        List<NavNode> unvisited = new List<NavNode>();
        Dictionary<NavNode, NavNode> previous = new Dictionary<NavNode, NavNode>(); //dictionary for optimal pathes
        Dictionary<NavNode, float> distances = new Dictionary<NavNode, float>(); //dictionary for distances

        for (int i = 0; i < allNodes.Count; i++)
        {
            NavNode node = allNodes[i];
            unvisited.Add(node); //add all nodes to list of unvisited nodes
            distances.Add(node, float.MaxValue); //all distances start with value infinite
        }

        distances[start] = 0f; //distance of startnode = 0

        while (unvisited.Count != 0) //do for all unvisited nodes
        {
            unvisited = unvisited.OrderBy(node => distances[node]).ToList(); //order unvisited nodes by distance from short ► long

            NavNode current = unvisited[0]; //node with shortest distance
            unvisited.Remove(current); //mark as visited

            if (current == end) //check if current node equals end-node
            {
                while (previous.ContainsKey(current)) //construction of path
                {
                    path.Nodes.Insert(0, current); //add node to list of shortest path
                    current = previous[current]; //get connected node
                }

                path.Nodes.Insert(0, current); //add start-node
                break;
            }

            for (int i = 0; i < current.Connections.Count; i++) //go through connections of current node
            {
                NavNode neighbor = current.Connections[i];
                float length = Vector3.Distance(current.transform.position, neighbor.transform.position); //get distance between these nodes
                float alt = distances[current] + length; //alt = distance from start to connected neighbor

                if (alt < distances[neighbor]) //check if distance from start to neighbor start is shorter than the previous calculated distance
                {
                    distances[neighbor] = alt; //set new distance in dictionary
                    previous[neighbor] = current; //set neighbor for optimal path
                }
            }
        }

        path.Bake(); //calculate lenght
        return path;
    }

    public void Reset()
    {
        lenghtText.text = "Length: ";

        foreach (GameObject transition in parser.transitions) //activate all transitions
        {
            transition.SetActive(true);
        }

        foreach (GameObject state in parser.states) //activate all states and apply default color and size
        {
            state.SetActive(true);
            state.transform.localScale = Vector3.one * parser.stateSize;
            state.GetComponent<Renderer>().material = Resources.Load("Materials/State_DEFAULT", typeof(Material)) as Material;
        }

        foreach (GameObject machine in parser.machines) //default color for machines
        {
            machine.GetComponent<Renderer>().material = Resources.Load("Materials/Machine_INSPECTION", typeof(Material)) as Material;
        }

        uiController.navigationStartEndToggles[0].isOn = true;
        uiController.navigationStartEndToggles[2].isOn = true;
        startDefined = false;
        mapChoiceEnabled = true;
        uiController.NavigationPanelControl(0);
    }

    public void NavigationMachineChoice(bool start) //called by OK-Buttons in Navigation Panel
    {
        string[] machineStateArray;

        if (start)
        {
            machineStateArray = parser.machineStates[uiController.navigationMachineChoice[0].value];
            currentMaterial = startMaterial; //define material
            parser.machines[uiController.navigationMachineChoice[0].value].GetComponent<Renderer>().material = currentMaterial; //Color choosen machine
        }
        else
        {
            machineStateArray = parser.machineStates[uiController.navigationMachineChoice[1].value];
            machinePanelControl.machineChoice.value = uiController.navigationMachineChoice[1].value;
            machinePanelControl.SetMachine();
            currentMaterial = endMaterial; //define material
            parser.machines[uiController.navigationMachineChoice[1].value].GetComponent<Renderer>().material = currentMaterial; //color choosen machine
        }

        foreach (GameObject state in parser.states)
        {
            if (state.name == machineStateArray[1])
            {
                state.GetComponent<Renderer>().material = currentMaterial;
                SetStartEnd(state);

                state.transform.localScale = Vector3.one * parser.stateSize * 1.3f;
                break;
            }


        }
        mapChoiceEnabled = true;
    }

}
