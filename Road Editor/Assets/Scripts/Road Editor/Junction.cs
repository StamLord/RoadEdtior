using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Junction
{
    private RoadEditorManager_Task manager;

    public int index {get; private set;}
    public Vector3 position {get; private set;}
    public List<Junction> neighbors = new List<Junction>(); 
    public List<int> neighborIndexes = new List<int>();  // Used when saving/loading. May replace neighbors completely
    public GameObject worldObject {get; private set;}

    public Junction(RoadEditorManager_Task manager, int index, Vector3 position, List<Junction> neighbors = null, List<int> neighborIndexes = null, GameObject worldObject = null)
    {
        this.manager = manager;
        this.index = index;
        this.position = position;
        this.neighborIndexes = neighborIndexes;

        SetWorldObject(worldObject);
        
        if(neighbors != null)
            this.neighbors = neighbors;
    }

    private void RemoveAllConnections()
    {
        // Remove this node from all neighbors
        foreach(Junction n in neighbors)
            n.neighbors.Remove(this);
    }

    public void SetWorldObject(GameObject gameObject)
    {
        worldObject = gameObject;
        if(worldObject != null)
            this.worldObject.name = "Junction " + index;
    }

    public void DestroyJunction()
    {
        RemoveAllConnections();
        GameObject.Destroy(worldObject);
    }

    public List<int> BreadthFirstSearch()
    {
        List<int> visited = new List<int>();
        Queue<int> queue = new Queue<int>();

        queue.Enqueue(index);

        while(queue.Count > 0)
        {
            int j = queue.Dequeue();

            if(visited.Contains(j))
                continue;

            visited.Add(j);

            foreach(Junction neighbor in neighbors)
                if(visited.Contains(neighbor.index) == false)
                    queue.Enqueue(neighbor.index);
        }

        return visited;
    }
}