using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public enum NodeType
{
    Reliable,
    Neutral,
    Misinformed
}

public class Node : MonoBehaviour
{
    public static event Action CutNode;
    public static event Action<NodeType, Node> NodeTypeChanged;

    //TODO move link prefab to manager
    public GameObject linkPrefab;

    public List<Node> connectedNodes;
    public NodeType type;
    public int influence;
    public float connectionRadius;
    public Node influenceSource;

    private TMP_Text _debugText;

    public InformationSource Source { get; set; }

    //Node changed type
    public void ChangeNodeType(NodeType newType)
    {
        type = newType;
        NodeTypeChanged?.Invoke(newType, this);
    }

    //Connects to a node. This is the first step in creating a node link.
    private void ConnectFirst(Node adjacentNode)
    {
        if (!adjacentNode) return;

        //Caution check for duplicate node connections.
        if (connectedNodes.Contains(adjacentNode)) return;

        //TODO check for maximum connections already

        //Register the new connection to this node
        connectedNodes.Add(adjacentNode);
        //Then Register on the connected node
        adjacentNode.ConnectPair(this);

        //Create a link between the two
        Link newLink = Instantiate(linkPrefab).GetComponent<Link>();
        newLink.SetupLink(this, adjacentNode);
    }

    //Connects to a node. This is the second (Final) step in creating a node link.
    private void ConnectPair(Node pairedNode)
    {
        //Acknowledge the connection between these nodes <3
        connectedNodes.Add(pairedNode);
    }

    private void Awake()
    {
        //All nodes start as Neutral
        ChangeNodeType(NodeType.Neutral);
        _debugText = GetComponentInChildren<TMP_Text>();
    }

    public void FindConnections(int maxConnections)
    {
        //Get all nearby Nodes
        List<Node> nearbyNodes = new List<Node>();
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, connectionRadius);

        foreach (Collider2D coll2D in nearbyColliders)
        {
            nearbyNodes.Add(coll2D.GetComponent<Node>());

            //Stop connecting after maximum reached
            if (nearbyNodes.Count > maxConnections) break;
        }

        //Remove self from nearby nodes
        Node col = GetComponent<Node>();
        if (nearbyNodes.Contains(col))
        {
            nearbyNodes.Remove(col);
        }

        //Connect to each node
        foreach (Node node in nearbyNodes)
        {
            ConnectFirst(node);
        }
    }

    public void CheckPower()
    {
        _debugText.text = influence.ToString();
        if (influence >= 10)
        {
            // This node is now influenced by bad information. RIP
            ChangeNodeType(NodeType.Misinformed);
        }

        if (influence <= -10)
        {
            // We have become a spreader of truths.
            ChangeNodeType(NodeType.Reliable);
        }

        if (type is NodeType.Misinformed or NodeType.Reliable && influence is < 5 and > -5)
        {
            // Slowly changing our ways
            ChangeNodeType(NodeType.Neutral);
        }
    }

    public void BreakConnection(Node endNode)
    {
        connectedNodes.Remove(endNode);
    }
    
    public void NodePromotion()
    {
        influence -= 10;
        CheckPower();
    }

    public void NodeDemotion()
    {
        Debug.Log(transform.position + " has been demoted!");
    }
}


public class ComparisonX : IComparer<Node>
{
    public int Compare(Node x, Node y)
    {
        if (x == null || y == null)
        {
            return 0;
        }

        if (x.transform.position.x <= y.transform.position.x) return -1;
        return 1;
    }
}
