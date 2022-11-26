using System.Collections.Generic;
using UnityEngine;

public class NodeNetworkManager : MonoBehaviour
{
    public static List<Node> AllNodes { get; private set; }
    private static bool _startTicking;

    private void OnEnable()
    {
        NodeSpawner.NewNode += AddNode;
        GameController.StartGame += StartLogic;
    }

    private static void ChangeNodeType(NodeType type, Node node)
    {
        if (type == NodeType.Neutral)
        {
            Destroy(node.Source);
            node.Source = null;
        }
        else
        {
            MakeNodeSource(node,
                !node.influenceSource
                    ? GameController.Instance.GameVariables.defaultReliablePower
                    : node.influenceSource.Source.informationPower);
        }
    }

    private void OnDisable()
    {
        NodeSpawner.NewNode -= AddNode;
        GameController.StartGame -= StartLogic;
        Node.NodeTypeChanged -= ChangeNodeType;
    }

    private void Awake()
    {
        AllNodes = new List<Node>();
    }

    private static void AddNode(Node node)
    {
        AllNodes.Add(node);
    }

    public static int NumberNodes()
    {
        return AllNodes.Count;
    }

    private void StartLogic(GameVars gameVars)
    {
        // First choose the source nodes and set them
        // At this point, all nodes should be valid so we should connect them all
        foreach (Node node in AllNodes)
        {
            node.FindConnections(gameVars.maxDefaultNodeConnections);
        }

        SetupInformationSources(gameVars);

        _startTicking = true;

        // We don't need to be registered for this event until set up is complete
        Node.NodeTypeChanged += ChangeNodeType;
    }

    private static void SetupInformationSources(GameVars gameVars)
    {
        // Sort AllNodes to be in order of left to right on player screen
        AllNodes.Sort(new ComparisonX());

        //Generate Reliable Sources
        for (var i = 0; i < gameVars.numReliableSources; i++)
        {
            AllNodes[i].influence = -10;
            AllNodes[i].CheckPower();
            MakeNodeSource(AllNodes[i], gameVars.defaultReliablePower);
        }

        //Generate Misinformed Sources
        for (var i = NumberNodes() - 1; i >= NumberNodes() - gameVars.numBadSources; i--)
        {
            AllNodes[i].influence = 10;
            AllNodes[i].CheckPower();
            MakeNodeSource(AllNodes[i], gameVars.defaultMisinformationPower);
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis - not called frequently 
    private static void MakeNodeSource(Node node, int power)
    {
        InformationSource infoSource = node.gameObject.AddComponent<InformationSource>();
        infoSource.Attach(node, power);
    }

    private void Update()
    {
        // GAME JAM LOGIC COMMENTS AT BOTTOM
        if (!_startTicking) return;
        var tick = Time.deltaTime;

        foreach (Node node in AllNodes)
        {
            switch (node.type)
            {
                case NodeType.Reliable:
                case NodeType.Misinformed:
                    node.Source.AddTime(tick);
                    break;
            }
        }
        
        foreach (Node node in AllNodes)
        {
            switch (node.type)
            {
                case NodeType.Neutral:
                    node.CheckPower();
                    break;
            }
        }

        // Every node should be checked: 
        // Source nodes: tick time towards next spread based on power and time. Check if source parent is still a source

        // When a source node timer ticks over, we spread to all adjacent nodes (check that they are not same type source nodes) 1 power
        // Reset source node timer and check the neutral nodes if they will become sources or not. Add a source parent once sourced. 

        // When something changes in the node we handle that separately to this logic - i.e. by changing the threshold for the node spread
    }
}