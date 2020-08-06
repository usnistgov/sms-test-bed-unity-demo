using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NavPath
{
	public List<NavNode> pathNodes = new List<NavNode> ();
	public float totalDistance;

	public  List<NavNode> Nodes 
	{	get { return pathNodes; }	}

	public float Lenght
	{ get {	return totalDistance; } }

	public void Bake () //calculate total Distance
	{
		List<NavNode> calculated = new List<NavNode> ();
		totalDistance = 0f;
		for ( int i = 0; i < pathNodes.Count; i++ )
		{
			NavNode node = pathNodes [ i ];
			for ( int j = 0; j < node.Connections.Count; j++ )
			{
				NavNode connection = node.Connections [ j ];
				
				if ( pathNodes.Contains ( connection ) && !calculated.Contains ( connection ) ) //check if connection is part of shortest path and havent been already calculated
				{
					totalDistance += Vector3.Distance ( node.transform.position, connection.transform.position ); //add distance to neighbor to total distance
				}
			}
			calculated.Add ( node ); //mark node as calculated
		}
	}
}
