using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NodeMapManager : MonoBehaviour
{

    public List<Tilemap> blockers;
    private MapNode[,] mapNodes;

    public int sizeX, sizeY;
    public Vector2 origin;

    public static NodeMapManager map;

    public GameObject testMarker;
    public bool draw_test_markers;

    [Range(1,5)]
    public int cellSizeX = 2, cellSizeY = 2;

    private Dictionary<int, MapNode> node_claims;

    private List<MapNode> activeNodes;

    // Start is called before the first frame update
    void Start()
    {
        mapNodes = new MapNode[sizeX, sizeY];
        node_claims = new Dictionary<int, MapNode>();
        activeNodes = new List<MapNode>();
        MakeMapNodeGraph();
        MakeNodeNeighbors();
        CheckNodeActivity();
        GetActiveList();
        if(draw_test_markers){
            DrawTestMarker();
        }
        map = this;
    }

    /**
     * Generate map nodes and shift every other row by 0.5 units
     * */
    private void MakeMapNodeGraph()
    {
        for (int x_idx = 0; x_idx < sizeX; x_idx++)
        {
            for(int y_idx = 0; y_idx < sizeY; y_idx++)
            {

                MapNode node = new MapNode(x_idx + (0.5f * (y_idx % 2)) + origin.x, -y_idx + origin.y, cellSizeX, cellSizeY);

                mapNodes[x_idx, y_idx] = node;

            }
        }
    }
    /**
     * For each map node, assign neighbors that are diagonal to it and horiz/vert to it, allowing npc's the ability to move
     * up, down, or diagonal to reach their goal
     * */
    private void MakeNodeNeighbors()
    {
        for (int x_idx = 0; x_idx < sizeX; x_idx++)
        {
            for (int y_idx = 0; y_idx < sizeY; y_idx++)
            {

                MapNode node = mapNodes[x_idx, y_idx];

                if(x_idx != 0)
                {
                    node.AddNeighbor(mapNodes[x_idx - 1, y_idx]);
                }
                if (x_idx != sizeX - 1)
                {
                    node.AddNeighbor(mapNodes[x_idx + 1, y_idx]);
                }

                if(y_idx != 0)
                {
                    node.AddNeighbor(mapNodes[x_idx, y_idx - 1]);
                }

                if (y_idx != sizeY - 1)
                {
                    node.AddNeighbor(mapNodes[x_idx, y_idx + 1]);
                }

                if (y_idx > 1)
                {
                    node.AddNeighbor(mapNodes[x_idx, y_idx - 2]);
                }

                if (y_idx < sizeY - 2)
                {
                    node.AddNeighbor(mapNodes[x_idx, y_idx + 2]);
                }

                //Because every other row is offset, depending on offset neighbor is going
                //to be reversed
                if (y_idx % 2 == 0)
                {
                    if(x_idx != 0 && y_idx != 0)
                    {
                        node.AddNeighbor(mapNodes[x_idx - 1, y_idx - 1]);
                    }

                    if (x_idx != 0 && y_idx != sizeY - 1)
                    {
                        node.AddNeighbor(mapNodes[x_idx - 1, y_idx + 1]);
                    }
                }
                else
                {
                    //y_idx should never be zero but just in case
                    if (x_idx != sizeX - 1 && y_idx != 0)
                    {
                        node.AddNeighbor(mapNodes[x_idx + 1, y_idx - 1]);
                    }

                    if (x_idx != sizeX - 1 && y_idx != sizeY - 1)
                    {
                        node.AddNeighbor(mapNodes[x_idx + 1, y_idx + 1]);
                    }
                }
                mapNodes[x_idx, y_idx] = node;
            }
        }
    }

    /**
     * For each map node, check against the "blockers" list of tilemaps to see what areas the npc is now allowed to go to
     * */
    private void CheckNodeActivity()
    {
        for (int x_idx = 0; x_idx < sizeX; x_idx++)
        {
            for (int y_idx = 0; y_idx < sizeY; y_idx++)
            {

                MapNode node = mapNodes[x_idx, y_idx];

                foreach(Tilemap map in this.blockers)
                {

                    TileBase tile = map.GetTile((Vector3Int)node.GetIntPosUnder());
                    if(!(tile is null))
                    {
                        node.SetActive(false);
                    }
                    tile = map.GetTile((Vector3Int)node.GetIntPosOver());
                    if (!(tile is null))
                    {
                        node.SetActive(false);
                    }
                }
            }
        }
    }

    private void GetActiveList()
    {
        activeNodes = new List<MapNode>();

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                if (mapNodes[x, y].IsActive())
                {
                    activeNodes.Add(mapNodes[x, y]);
                }
            }
        }
    }

    /**
     * Draws the range of the map nodes
     * */
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(new Vector2(origin.x, origin.y), new Vector2(origin.x + sizeX, origin.y));
        Gizmos.DrawLine(new Vector2(origin.x, origin.y), new Vector2(origin.x, origin.y - sizeY));
        Gizmos.DrawLine(new Vector2(origin.x + sizeX, origin.y), new Vector2(origin.x + sizeX, origin.y - sizeY));
        Gizmos.DrawLine(new Vector2(origin.x, origin.y - sizeY), new Vector2(origin.x + sizeX, origin.y - sizeY));
    }

    public void Unclaim(int id)
    {
        node_claims.Remove(id);
    }

    /**
     * Replaces the claim of a node to replace owner
     * */
    private void ReplaceClaim(int instanceID, MapNode mapNode)
    {
        if (node_claims.ContainsKey(instanceID) && node_claims[instanceID] != null)
        {
            MapNode oldClaim = node_claims[instanceID];
            oldClaim.SetClaim(-1);
        }
        mapNode.SetClaim(instanceID);
        node_claims[instanceID] = mapNode;
    }

    public Vector2 GetAStarMovement(int instanceID, Vector2 currPos, GameObject target)
    {
        //Make sure every node is cleared
        ResetNodesPrevious();

        PriorityQueue<MapNode> nodeQueue = new PriorityQueue<MapNode>();

        MapNode origin = GetNearestNode(currPos);
        MapNode goal = GetNearestNode(target.transform.position);

        ReplaceClaim(instanceID, origin);

        nodeQueue.Enqueue(origin, 0);

        MapNode next = origin;

        int checks = 0;

        while (!nodeQueue.IsEmpty())
        {
            checks++;
            next = nodeQueue.Dequeue();


            if (next.Equals(goal))
            {
                break;
            }

            if(checks > 200)
            {
                Debug.Log("FAILED TO FIND AN A* PATH in 200 checks");
                return new Vector2(0, 0);
            }

            foreach(MapNode node in next.GetNeighbors())
            {
                if (node.GetPrevious() == null && node.IsActive()){
                    float value_to_assign = next.GetValue() + Vector2.Distance(next.GetPos(), node.GetPos());


                    node.SetValue(value_to_assign);
                    node.SetPrevious(next);

                    value_to_assign +=  Vector2.Distance(node.GetPos(), goal.GetPos());
                    nodeQueue.Enqueue(node, value_to_assign);
                }

            }

        }

        if (next.Equals(origin))
        {
            return new Vector2(0, 0);
        }

        while (!next.GetPrevious().Equals(origin))
        {
            next = next.GetPrevious();
        }

        Vector2 new_movement = next.GetPos() - origin.GetPos();
        return new_movement;
    }

    public Vector2 GetAStarMovement(int instanceID, Vector2 currPos, Vector2 goal_pos)
    {
        //Make sure every node is cleared
        ResetNodesPrevious();

        PriorityQueue<MapNode> nodeQueue = new PriorityQueue<MapNode>();

        MapNode origin = GetNearestNode(currPos);
        MapNode goal = GetNearestNode(goal_pos);

        ReplaceClaim(instanceID, origin);

        nodeQueue.Enqueue(origin, 0);

        MapNode next = origin;

        int checks = 0;

        while (!nodeQueue.IsEmpty())
        {
            checks++;
            next = nodeQueue.Dequeue();


            if (next.Equals(goal))
            {
                break;
            }

            if (checks > 200)
            {
                Debug.Log("FAILED TO FIND AN A* PATH in 200 checks");
                return new Vector2(0, 0);
            }

            foreach (MapNode node in next.GetNeighbors())
            {
                if (node.GetPrevious() == null && node.IsActive() && node.GetClaim() == -1)
                {
                    float value_to_assign = next.GetValue() + Vector2.Distance(next.GetPos(), node.GetPos());


                    node.SetValue(value_to_assign);
                    node.SetPrevious(next);

                    value_to_assign += Vector2.Distance(node.GetPos(), goal.GetPos());
                    nodeQueue.Enqueue(node, value_to_assign);
                }

            }

        }

        if (next.Equals(origin))
        {
            return new Vector2(0, 0);
        }

        while (!next.GetPrevious().Equals(origin))
        {
            next = next.GetPrevious();
        }

        Vector2 new_movement = next.GetPos() - origin.GetPos();
        return new_movement;
    }

    /**
     * resets each node's previous node value to null for calculations to be done again
     * */
    private void ResetNodesPrevious()
    {
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                mapNodes[i, j].SetPrevious(null);
                mapNodes[i, j].SetValue(0);
            }
        }
    }

    /**
     * Returns the node nearest to the current position
     * */
    private MapNode GetNearestNode(Vector2 pos)
    {
        MapNode nearest = null;
        for(int i = 0; i < sizeX; i++)
        {
            for(int j = 0; j < sizeY; j++)
            {
                if(mapNodes[i, j].IsActive()){
                    if(nearest == null)
                    {
                        nearest = mapNodes[i, j];
                    }
                    if(Vector2.Distance(pos, mapNodes[i, j].GetPos()) < Vector2.Distance(pos, nearest.GetPos()))
                    {
                        nearest = mapNodes[i, j];
                    }
                }
            }
        }

        return nearest;
    }

    public Vector2 GetRandomActivePoint()
    {

        if(activeNodes.Count == 0)
        {
            Debug.Log("No active nodes in nodemap");
            return new Vector2(0, 0);
        }

        return activeNodes[Random.Range(0, activeNodes.Count)].GetPos();

    }

    public Vector2 GetNearestPoint(Vector2 pos)
    {
        MapNode node = GetNearestNode(pos);
        return node.GetPos();
    }


    public void DrawTestMarker()
    {
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {

                if (mapNodes[i, j].IsActive())
                {

                    Instantiate(testMarker, mapNodes[i, j].GetPos(), Quaternion.identity);
                }
            }
        }
    }
}

class MapNode{

    private float x, y;
    public List<MapNode> neighbors;

    private bool isActive;
    private int sizeX, sizeY;

    private MapNode previous;
    private float value;
    private int claimed;

    public MapNode(float x, float y, int sizeX, int sizeY)
    {
        this.x = x;
        this.y = y;
        this.neighbors = new List<MapNode>();
        this.isActive = true;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.claimed = -1;
    }

    public Vector2 GetPos()
    {
        return new Vector2(x, y);
    }

    public Vector2Int GetIntPosUnder()
    {
        return new Vector2Int((int)(x-0.5f)/sizeX, (int)(y-1)/sizeY);
    }

    public Vector2Int GetIntPosOver()
    {
        return new Vector2Int((int)(x-1.5f) / sizeX, (int)(y-1) / sizeY);
    }

    public void AddNeighbor(MapNode node)
    {
        neighbors.Add(node);
    }

    public List<MapNode> GetNeighbors()
    {
        return this.neighbors;
    }

    public void SetActive(bool active)
    {
        this.isActive = active;
    }

    public bool IsActive()
    {
        return this.isActive;
    }

    public void SetPrevious(MapNode node)
    {
        this.previous = node;
    }

    public MapNode GetPrevious()
    {
        return this.previous;
    }

    public float GetValue()
    {
        return this.value;
    }

    public void SetValue(float value)
    {
        this.value = value;
    }

    public override string ToString()
    {
        return "<" + this.x + "," + this.y + ">";
    }
    
    public void SetClaim(int id)
    {
        this.claimed = id;
    }

    public int GetClaim()
    {
        return this.claimed;
    }

}
