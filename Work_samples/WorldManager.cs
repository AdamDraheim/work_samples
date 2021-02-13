using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

/**
 * Created by Adam Draheim for the RPG Bygone
 * 
 * For instances of NPC's that are saved between levels in Unity, the world manager acts as storage for all npc's and returns 
 * whichever ones match given criteria. NPC's spawned or placed into levels will be saved here, and then reloaded into the world
 * once the player leaves and re-enters the level. This allows NPC persistence between loads.
 * 
 * As well, world manager tracks how long it has been since the world has been updated, and if a threshold is passed,
 * will wipe all valid NPC's from that level and return it to an "unvisited" state.
 * */

public class WorldManager : MonoBehaviour
{
    public static WorldManager world_manager;

    private int instance_id = 1;

    [System.Serializable]
    public struct npc_data
    {
        public int instance_id;
        public string level;
        public int level_id;
        public string npc;
        public bool persistent;
        public bool isActive;
    }

    //Tracks if level has been reset
    private Dictionary<string, int> reset;
    //Tracks data for each instance
    private Dictionary<int, npc_data> id_to_data;
    //List of all levels the player has seen
    private List<string> levelList;
    //How many levels the player must travel through before this one resets
    [Tooltip("How long until reset")]
    public int reset_threshold;

    [Tooltip("Tracks the last levels the player has been through")]
    public string last_level;

    // Start is called before the first frame update
    void Awake()
    {
        
        if (world_manager == null)
        {
            world_manager = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            world_manager.InstantiateNPCsinLevel(SceneManager.GetActiveScene().name);
            Destroy(this.gameObject);
            return;
        }

        reset = new Dictionary<string, int>();
        id_to_data = new Dictionary<int, npc_data>();
        levelList = new List<string>();

    }

    private void Update()
    {

        CheckResets();
        AddLevelToTrack(SceneManager.GetActiveScene().name);
    }

    /**
     * Save a new npc into the dictionary and return the instance id it is saved as.
     * */
    public int RegisterNPCInstance(string npc, int levelID, string level, bool persistent)
    {

        //Increment instance id
        instance_id += 1;

        npc = Json_Methods.ReplaceValue(npc, "instance_id", instance_id.ToString());

        //Establish data for the npc
        npc_data new_data = new npc_data();
        new_data.instance_id = instance_id;
        new_data.level = level;
        new_data.level_id = levelID;
        new_data.npc = npc;
        new_data.persistent = persistent;
        new_data.isActive = true;

        //Reference instance id to new data
        id_to_data.Add(instance_id, new_data);

        return instance_id;
    }

    /**
     * Given an NPC object, JSON npc string, level, and persistent definition, see if 
     * there is already an npc matching the defined characteristics in the dictionary. If 
     * there is, then return false, otherwise true
     * 
     * Checks that the npc does not already exist
     * */
    public bool CheckNPCInstance(NPC npcObj, int levelID, string level)
    {

        foreach (npc_data data in id_to_data.Values)
        {
            Debug.Log(level + " " + data.level);
            //Debug.Log(JsonUtility.ToJson(data));
            if(data.level.Equals(level)){
                //Debug.Log("PASSED LEVEL");
                if(levelID == data.level_id)
                {
                    if (npcObj.GetInstanceID() == data.instance_id)
                    {
                        return false;
                    }
                }
            }
        }
        
        return true;
    }

    /**
     * Removes an NPC of instance id from the dictionary
     * */
    public void DeregisterNPCInstance(int id)
    {
        if (id_to_data.ContainsKey(id))
        {
            npc_data data = id_to_data[id];
            data.isActive = false;
            id_to_data[id] = data;
        }
    }

    /**
     * Adds level to tracked list
     * */
    public void AddLevelToTrack(string level)
    {
        if(!reset.ContainsKey(level)){
            reset.Add(level, 0);
            levelList.Add(level);
        }
    }

    public void ResetWorldManager()
    {
        reset = new Dictionary<string, int>();
        id_to_data = new Dictionary<int, npc_data>();
        levelList = new List<string>();
    }

    /**
     * Takes in an instance ID and a new set of data. If the instance ID is stored, then replace the data
     * with the new set of JSON data
     * */
    public void ReplaceNPC(int passedInInstanceID, string npc_data)
    {

        if (!id_to_data.ContainsKey(passedInInstanceID))
        {
            Debug.Log("Instance ID " + passedInInstanceID + " does not exist in persistent data");
            return;
        }

        npc_data data = id_to_data[passedInInstanceID];
        data.npc = npc_data;
        id_to_data[passedInInstanceID] = data;
    }

    /**
     * Removes all npc's from a level that are not defined as persistent
     * */
    public void ResetLevel(string level)
    {
        List<int> IdsToRemove = new List<int>();
        foreach (npc_data data in id_to_data.Values)
        {
            if (data.level.Equals(level))
            {
                if (!data.persistent)
                {
                    IdsToRemove.Add(data.instance_id);
                }
            }
        }

        foreach(int id in IdsToRemove)
        {
            id_to_data.Remove(id);
        }

    }

    /**
     * Checks every level to see if it should be reset, ie world space can be restored to an unvisited state
     * */
    private void CheckResets()
    {
        //Plyaer has to be in a different level
        if (SceneManager.GetActiveScene().name != last_level)
        { 
            //Ensure the gamestate is in game, so menu does not count
            if(GameState.GetGameState() == GameState.gamestate.GAME)
            {
                last_level = SceneManager.GetActiveScene().name;

                //Reset each level in the keys if the value surpasses the threshold
                foreach(string key in levelList)
                {
                    //Make sure to not reset the current level
                    if (SceneManager.GetActiveScene().name != key){
                        reset[key] += 1;
                        if(reset[key] >= reset_threshold)
                        {
                            ResetLevel(key);
                            reset[key] = 0;
                        }
                       
                    }
                }
            }
        }
    }

    /*
     * Returns a list of JSON data for each npc in a given level
     * */
    public List<string> GetNPCsInLevel(string level)
    {
        List<string> npcs = new List<string>();

        foreach(npc_data data in id_to_data.Values)
        {
            if (data.level.Equals(level))
            {
                npcs.Add(data.npc);
            }
        }

        return npcs;
    }

    /**
     * Given an npc id and a level, return a list of all instance id's that match
     * */
    public List<int> GetIDofNPCTypeInLevel(int npcID, string level)
    {
        List<int> ids = new List<int>();

        foreach (npc_data data in id_to_data.Values)
        {
            if (data.level.Equals(level))
            {
                string savedID = Json_Methods.GetValue(data.npc, "ID");
                if (int.TryParse(savedID, out int newid))
                {
                    if(newid == npcID)
                    {
                        ids.Add(data.instance_id);
                    }

                }


            }
        }

        return ids;

    }

    /*
     * Given the specified level, spawn the npc's into it that are not currently active
     * */
    public void InstantiateNPCsinLevel(string level)
    {
        List<npc_data> to_modify = new List<npc_data>();

        foreach(npc_data data in id_to_data.Values)
        {
            if(data.level == level & data.isActive && data.level_id == -1)
            {
                string dataval = data.npc;
                string id_string = Json_Methods.GetValue(dataval, "NPCID");
                if(int.TryParse(id_string, out int id))
                {
                    NPC npc = NPC_Lib.npcLib.GetNPC(id);
                    npc = LoadNPConJson(npc, dataval);
                    //Set the npc position
                    Vector2 pos = getNPCpos(dataval);
                    //Set the npc to LOADED
                    npc.instantiation_type = NPC.INSTANCE_TYPE.LOADED;

                    Instantiate(npc, pos, Quaternion.identity);

                    to_modify.Add(data);
                }
            }
        }

        //For each npc we are adding, set to active
        foreach(npc_data data in to_modify)
        {
            int instance_id = data.instance_id;
            npc_data copy = id_to_data[instance_id];
            copy.isActive = true;
            id_to_data[instance_id] = copy;
        }

    }
    /* *
    * When an npc is manually placed in the level, check to see if there is data that can be assigned first
    * */
    public void LoadNPCfromLevel(NPC npc, int levelID, string level)
    {
        int id = npc.GetID();
        Destroy(npc.gameObject);

        foreach (npc_data data in id_to_data.Values)
        {
            //If in current level, the player specific level id equals the saved one, and the saved npc is active, load it
            if (data.level == level & data.level_id == levelID & data.isActive)
            {

                string strCheckId = Json_Methods.GetValue(data.npc, "NPCID");

                int.TryParse(strCheckId, out int checkID);

                //If the id of the JSON equals the saved npc id, then load the npc into the level
                if(checkID == id)
                {

                    //Create a copy of the npc from the library prefab (must use copy to not override prefab information)
                    NPC newnpc = Instantiate(NPC_Lib.npcLib.GetNPC(id));
                    NPC npcCopy = LoadNPConJson(newnpc, data.npc);

                    //Get the saved position from JSON
                    Vector2 pos = getNPCpos(data.npc);

                    //Set the instantiation type to loaded so it does not save as new instance once created
                    npcCopy.instantiation_type = NPC.INSTANCE_TYPE.LOADED;
                    
                    //Create the npc at the position specified in the JSON
                    Instantiate(npcCopy, pos, Quaternion.identity);
                    
                    int instance_id = data.instance_id;

                    npc_data copy = id_to_data[data.instance_id];
                    copy.isActive = true;
                    id_to_data[instance_id] = copy;

                    //Destroy the copy so we only have one in the world
                    Destroy(newnpc.gameObject);

                    break;

                }
            }
        }
        
    }

    /**
     * 
     * Unity does not support inheritance JSON, so check to see what type the object is and manually deserialize it
     * 
     * */ 
    private NPC LoadNPConJson(NPC npc, string Json)
    {
        if(npc is Chaser)
        {
            npc = (Chaser)npc;
            npc.GetComponent<Chaser>().Deserialize(Json);
        }
        else if(npc is Patroller)
        {
            npc = (Patroller)npc;
            npc.GetComponent<Patroller>().Deserialize(Json);
        }else if(npc is Villager)
        {
            npc = (Villager)npc;
            npc.GetComponent<Villager>().Deserialize(Json);
        }else if(npc is Tracker)
        {
            npc = (Tracker)npc;
            npc.GetComponent<Tracker>().Deserialize(Json);
        }
        else if (npc is Shopkeep)
        {
            npc = (Shopkeep)npc;
            npc.GetComponent<Shopkeep>().Deserialize(Json);
        }
        else if (npc is Fyeldor)
        {
            npc = (Fyeldor)npc;
            npc.GetComponent<Fyeldor>().Deserialize(Json);
        }else if(npc is TargetAttacker)
        {
            npc = (TargetAttacker)npc;
            npc.GetComponent<TargetAttacker>().Deserialize(Json);
        }else if(npc is NoncombativeCombatant)
        {
            npc = (NoncombativeCombatant)npc;
            npc.GetComponent<NoncombativeCombatant>().Deserialize(Json);
        }

        return npc;
    }

    /**
     * Retrieves the npc position from the json file in respect to both x and y coordinates
     * */
    private Vector2 getNPCpos(string data)
    {

        string str_x = Json_Methods.GetValue(data, "x");
        string str_y = Json_Methods.GetValue(data, "y");

        float.TryParse(str_x, out float x);
        float.TryParse(str_y, out float y);

        Vector2 pos = new Vector2(x, y);

        return pos;
    }

    /**
     * Clears all npcs from the instance dictionary
     * */
    public void ClearNPCs()
    {
        id_to_data.Clear();
    }

    /**
     * Saves the instance dictionary into a text file
     * */
    public void SaveNPCList(string destination)
    {
        string saveData = "\n" + this.instance_id + "\n";

        //Debug.Log("Saving npc data to " + destination);

        foreach(int key in id_to_data.Keys)
        {

            saveData += key + "#" + JsonUtility.ToJson(id_to_data[key]) + "\n";
            
        }
        FileStream stream = new FileStream(destination, FileMode.Create);

        BinaryFormatter bf = new BinaryFormatter();

        bf.Serialize(stream, saveData);
        stream.Close();
    }

    /**
     * Loads the npc data from a textfile into the instance dictionary
     * */
    public void LoadNPCList(string path)
    {
        //Debug.Log("Loading NPC data from " + path);

        string data = "";

        if (File.Exists(path))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            data = bf.Deserialize(stream) as string;
            stream.Close();
        }
        else
        {
            Debug.Log("Cannot find file " + path);
            return;
        }

        StreamReader reader = new StreamReader(path);
        reader.Peek();
        string nextLine = reader.ReadLine();
        nextLine = reader.ReadLine();
        int.TryParse(nextLine, out this.instance_id);

        while ((nextLine = reader.ReadLine()) != null){

            if(nextLine.Length <= 1){
                return;
            }

            string[] datavals = nextLine.Split('#');
            int.TryParse(datavals[0], out int inst_id);
            id_to_data.Add(inst_id, (npc_data)JsonUtility.FromJson(datavals[1], typeof(npc_data)));


        }

    }

    /**
     * Saves level data to a text file
     * */
    public void SaveLevelData(string destination)
    {
        string saveData = "\n";

        //Debug.Log("Saving npc data to " + destination);

        foreach (string key in reset.Keys)
        {

            saveData += key + "#" + JsonUtility.ToJson(reset[key]) + "\n";

        }
        FileStream stream = new FileStream(destination, FileMode.Create);

        BinaryFormatter bf = new BinaryFormatter();

        bf.Serialize(stream, saveData);
        stream.Close();
    }

    /**
     * Loads from a text file the data for each level the player has visited
     * */
    public void LoadLevelData(string path)
    {
        //Debug.Log("Loading NPC data from " + path);

        string data = "";

        if (File.Exists(path))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            data = bf.Deserialize(stream) as string;
            stream.Close();
        }
        else
        {
            Debug.Log("Cannot find file " + path);
            return;
        }

        StreamReader reader = new StreamReader(path);
        reader.Peek();
        string nextLine = reader.ReadLine();

        while ((nextLine = reader.ReadLine()) != null)
        {

            if (nextLine.Length <= 1)
            {
                return;
            }

            string[] datavals = nextLine.Split('#');
            int.TryParse(datavals[1], out int count);
            reset.Add(datavals[0], count);
            levelList.Add(datavals[0]);


        }

    }

    /**
     * Resets all the levels to an empty state for new game
     * */
    public void ResetLevelData()
    {
        reset.Clear();
        levelList.Clear();
    }

    /**
     * Finds and saves every npc instance in the world
     * */
    public void UpdateNPCs()
    {
        NPC[] npcs = GameObject.FindObjectsOfType<NPC>();
        foreach(NPC character in npcs)
        {
            character.SaveNPC();
        }
    }


}
