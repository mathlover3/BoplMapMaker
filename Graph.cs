
//based off of https://www.geeksforgeeks.org/detect-cycle-in-a-graph/
// A C# Program to detect cycle in a graph
using MapMaker;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{

    private int V;
    private Dictionary<int, List<GraphNode>> adj;
    private Dictionary<int, List<GraphNode>> backwordsConnectsons;
    private Dictionary<int, bool> visited = new();
    private List<LogicGate> gates = new();
    private List<GraphNode> triggers = new();
    public Graph(int V)
    {
        this.V = V;

        adj = new Dictionary<int, List<GraphNode>>();
        backwordsConnectsons = new Dictionary<int, List<GraphNode>>();
        for (int i = 0; i < V; i++)
            adj.Add(i, new List<GraphNode>());
        for (int i = 0; i < V; i++)
            backwordsConnectsons.Add(i, new List<GraphNode>());
    }

    // Function to check if cycle exists
    private bool isCyclicUtil(int i, bool[] visited,
                              bool[] recStack)
    {
        // Mark the current node as visited and
        // part of recursion stack
        if (recStack[i])
            return true;

        if (visited[i])
            return false;

        visited[i] = true;

        recStack[i] = true;
        List<GraphNode> children = adj[i];

        foreach (GraphNode c in children) if (
            isCyclicUtil(c.id, visited, recStack)) return true;

        recStack[i] = false;

        return false;
    }
    public int NumberOfInputsToNode(int node)
    {
        return adj[node].Count;
    }

    public void addEdge(int sou, int dest, GameObject destOwner, LogicGate destGate, GameObject souOwner, LogicGate souGate)
    {
        var node = new GraphNode
        {
            id = dest,
            Owner = destOwner,
            Gate = destGate,
        };
        adj[sou].Add(node);
        var node2 = new GraphNode
        {
            id = sou,
            Owner = souOwner,
            Gate = souGate,
        };
        backwordsConnectsons[dest].Add(node2);
        if (souGate == null)
        {
            triggers.Add(node2);
        }
    }

    // Returns true if the graph contains a
    // cycle, else false
    public bool isCyclic()
    {
        // Mark all the vertices as not visited and
        // not part of recursion stack
        bool[] visited = new bool[V];
        bool[] recStack = new bool[V];

        // Call the recursive helper function to
        // detect cycle in different DFS trees
        for (int i = 0; i < V; i++)
            if (isCyclicUtil(i, visited, recStack))
                return true;

        return false;
    }
    //propeagtes forwords until it reaches a gate with a input from a gate that hasnt been registerd yet/a gate thats already registerd
    private void ForwordProbagate(GraphNode node)
    {
        //if we were already visited return
        if (visited.ContainsKey(node.id) && visited[node.id])
        {
            return;
        }
        //for all of the nodes in frount of us
        foreach (var OutputNode in adj[node.id])
        {
            //we check if its already been visited and if so return
            if (visited.ContainsKey(OutputNode.id) && visited[OutputNode.id])
            {
                return;
            }
            else
            {
                //if its not visited already then we check if all of its parents have been visited
                foreach (var parentNode in backwordsConnectsons[node.id])
                {
                    //if a parent hasnt been visited yet then we back propagate
                    if (!(visited.ContainsKey(parentNode.id) && visited[parentNode.id]))
                    {
                        BackPropagate(parentNode);
                    }
                }
                //once all of the parents are visited we can add this node as visited and add the gate to the list
                gates.Add(node.Gate);
                visited.Add(node.id, true);
            }
        }
    }
    //back Propagates untill it finds a node already visited (witch shouldnt happen as then it would have forword proagated alreaty and this would be activated
    //unless theres a loop in witch case all bets are off) or a node with no inputs. (on the graph witch doesnt include triggers so a gate with all inputs being triggers)
    private void BackPropagate(GraphNode node)
    {
        //if its not visited already then we check if all of its parents have been visited
        foreach (var parentNode in backwordsConnectsons[node.id])
        {
            //if a parent hasnt been visited yet then we back propagate
            if (!(visited.ContainsKey(parentNode.id) && visited[parentNode.id]))
            {
                BackPropagate(parentNode);
            }
            else
            {
                throw new Exception("WE FOUND A GATE THAT WAS ALREADY VISITED IN BACKPROPAGATE WHEN IT SHOULDNT HAVE BEEN!!! IUAOGAGUBFHJAUEF PLS SEND YOUR MAP FILE SO I CAN FIX THIS!");
            }
        }
        //once all of the parents are visited we can add this node as visited and add the gate to the list (if its not null)
        if (node.Gate != null)
        {
            gates.Add(node.Gate);

        }
        visited.Add(node.id, true);
    }
    public List<LogicGate> BuildListOfGates()
    {
        visited = new();
        gates = new();
        foreach (var trigger in triggers)
        {
            ForwordProbagate(trigger);
        }
        return gates;
    }
}
public class GraphNode
{
    public int id;
    public GameObject Owner;
    public LogicGate Gate;
}