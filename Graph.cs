
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
    private Dictionary<int, List<int>> backwordsConnectsons;

    public Graph(int V)
    {
        this.V = V;

        adj = new Dictionary<int, List<GraphNode>>();
        backwordsConnectsons = new Dictionary<int, List<int>>();
        for (int i = 0; i < V; i++)
            adj.Add(i, new List<GraphNode>());
        for (int i = 0; i < V; i++)
            backwordsConnectsons.Add(i, new List<int>());
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

    public void addEdge(int sou, int dest, LogicGate gate, bool IsLuaGate)
    {
        var node = new GraphNode
        {
            id = dest,
            gate = gate,
            IsLuaGate = IsLuaGate,
        };
        adj[sou].Add(node);
        backwordsConnectsons[dest].Add(sou);
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
    //returns a order of gates 
}
public class GraphNode
{
    public int id;
    public LogicGate gate;
    public bool IsLuaGate;
}