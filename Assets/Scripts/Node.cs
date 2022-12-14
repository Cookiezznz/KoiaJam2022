using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


public enum NodeType
{
    Reliable,
    Neutral,
    Misinformed,
    None
}

public class Node : MonoBehaviour
{
    public static event Action<NodeType, Node> NodeTypeChanged;

    //TODO move link prefab to manager
    public GameObject linkPrefab;

    public List<Node> connectedNodes;
    public NodeType type;
    public float influence;
    public float connectionRadius;
    public Node influenceSource;

    [SerializeField] private GameObject circleObject;
    public SpriteRenderer circleSprite;
    public GameObject verifiedSprite;

    public InformationSource Source { get; set; }

    //Node changed type
    private void ChangeNodeType(NodeType newType)
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
        circleSprite = circleObject.GetComponent<SpriteRenderer>();

        //All nodes start as Neutral
        ChangeNodeType(NodeType.Neutral);
        GameController.StopGame += KillSelf;
    }

    private void KillSelf(string s)
    {
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        GameController.StopGame -= KillSelf;
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
        if (influence >= 10f && type != NodeType.Misinformed)
        {
            // This node is now influenced by bad information. RIP
            ChangeNodeType(NodeType.Misinformed);
            influence = 10f;
        }

        if (influence <= -10f && type != NodeType.Reliable)
        {
            // We have become a spreader of truths.
            ChangeNodeType(NodeType.Reliable);
            influence = -10f;
        }

        if (type is NodeType.Misinformed or NodeType.Reliable && influence is < 5f and > -5f)
        {
            // Slowly changing our ways
            ChangeNodeType(NodeType.Neutral);
        }
    }

    public void BreakConnection(Node endNode)
    {
        connectedNodes.Remove(endNode);
    }

    public bool CanBePromoted()
    {
        return type == NodeType.Neutral && connectedNodes.Any(node => node.type == NodeType.Reliable);
    }

    public bool CanBeVerified()
    {
        if (!Source) return false;

        return !Source.IsVerified();
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

    public int GetNumConnectedNodesNotOfType(NodeType nodeType)
    {
        return connectedNodes.Count(node => node.type != nodeType);
    }

    public void TemporaryDisconnect(Node pairNode)
    {
        // Here we want to disconnect these two nodes temporarily so they must dereference each other
        connectedNodes.Remove(pairNode);
    }
    
    public void ManuallyReconnect(Node pairNode)
    {
        // Here we want to disconnect these two nodes temporarily so they must dereference each other
        connectedNodes.Add(pairNode);
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