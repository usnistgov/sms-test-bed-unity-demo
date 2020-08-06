using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;

public static class CommonObjs
{
    public static string TAG_STATE = "TAG_STATE";
    public static string TAG_CELLSPACE = "TAG_CELLSPACE";
    public static string TAG_GENERALSPACE = "TAG_GENERALSPACE";
    public static string TAG_TRANSITIONSPACE = "TAG_TRANSITIONSPACE";
    public static string TAG_TRANSITION = "TAG_TRANSITION";
    public static string TAG_CELLSPACEBOUNDARY = "TAG_CELLSPACEBOUNDARY";
    public static string TAG_TEXTURESURFACE = "TAG_TEXTURESURFACE";


    public static string ROOT = "Root";
    public static string ROOT_CELLSPACE = "CellSpace";
    public static string ROOT_GENERALSPACE = "GeneralSpace";
    public static string ROOT_TRANSITIONSPACE = "TransitionSpace";
    public static string ROOT_CELLSPACEBOUNDARY = "CellSpaceBoundary";
    public static string ROOT_STATE = "State";
    public static string ROOT_TRANSITION = "Transition";

    public static GameObject gmlRoot;
    public static GameObject gmlRootCellSpace;
    public static GameObject gmlRootGeneralSpace;
    public static GameObject gmlRootTransitionSpace;
    public static GameObject gmlRootCellSpaceBoundary;
    public static GameObject gmlRootState;
    public static GameObject gmlRootTransition;

    public static Material materialCellSpace;
    public static Material materialGeneralSpace;
    public static Material machineFront;
    public static Material materialCellSpaceBoundary;
    public static Material materialState;
    public static Material materialTextureSurface;
    public static Material materialWorkshop;
    public static Material materialState_START;
    public static Material materialState_TARGET;

    public static Shader shaderCullOFF;
    public static Shader shaderCullON;


    public static void Init()
    {
        gmlRoot = new GameObject("Root");
        gmlRootCellSpace = new GameObject("CellSpace");
        gmlRootGeneralSpace = new GameObject("GeneralSpace");
        gmlRootTransitionSpace = new GameObject("TransitionSpace");
        gmlRootCellSpaceBoundary = new GameObject("CellSpaceBoundary");
        gmlRootState = new GameObject("State");
        gmlRootTransition = new GameObject("Transition");

        gmlRootCellSpace.transform.parent = gmlRoot.transform;
        gmlRootGeneralSpace.transform.parent = gmlRoot.transform;
        gmlRootTransitionSpace.transform.parent = gmlRoot.transform;
        gmlRootCellSpaceBoundary.transform.parent = gmlRoot.transform;
        gmlRootState.transform.parent = gmlRoot.transform;
        gmlRootTransition.transform.parent = gmlRoot.transform;

        // Materials
        materialCellSpace = Resources.Load("Materials/CellSpaceFP", typeof(Material)) as Material;
        machineFront = Resources.Load("Materials/MachineFront", typeof(Material)) as Material;
        materialCellSpaceBoundary = Resources.Load("Materials/CellSpaceBoundary", typeof(Material)) as Material;
        materialWorkshop = Resources.Load("Materials/CellSpaceFP", typeof(Material)) as Material;
        materialState_START = Resources.Load("Materials/State_START", typeof(Material)) as Material;
        materialState_TARGET = Resources.Load("Materials/State_TARGET", typeof(Material)) as Material;
    }
}
