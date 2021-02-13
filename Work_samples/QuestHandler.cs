using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using UnityEngine;

/**
 * Quest Handler created by Adam Draheim for the RPG Bygone
 * 
 * Stores, updates, and reports quests that are created by a textfile. The QuestParser creates each quest, and the 
 * quest handler stores them. Each quest has an attached quest ID which is stored in a dictionary.
 * 
 * The quest handler updates quest with notifications from outside events, and acts independently of any
 * non-quest objects. Outside objects only need to call the notify method of the quest parser to report
 * potential quest-related actions.
 * 
 * */

public class QuestHandler : MonoBehaviour
{
    //All quest text files
    public TextAsset[] questTexts;
    //Reference to self as singleton object
    public static QuestHandler questHandler;
    //A parser for the quest text files
    private QuestParser parser;
    //Quest that the player is currently focused on
    private int currentQuest;
    //List of all quests that the player can currently do
    private List<Quest> activeQuestList;
    //Stores every quest to its corresponding ID
    public Dictionary<int, Quest> questTable;

    // Start is called before the first frame update
    void Start()
    {
        if(questHandler == null)
        {
            questHandler = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

        MakeQuests();

    }

    // Update is called once per frame
    void Update()
    {

    }

    /**
     * For every quest text document, attempt to make it into a quest and save it to the quest table
     * */
    public void MakeQuests()
    {
        questTable = new Dictionary<int, Quest>();
        activeQuestList = new List<Quest>();
        parser = new QuestParser();
        foreach (TextAsset questText in questTexts)
        {

            try
            {
                Dictionary<int, Quest> temptable = parser.CreateQuests(questText.text);

                foreach (int val in temptable.Keys)
                {
                    if (questTable.ContainsKey(val))
                    {
                        Debug.Log("Already contains quest ID " + val);
                    }
                    else
                    {
                        questTable.Add(val, temptable[val]);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                Debug.Log("Unable to read file " + questText.name);
            }


        }
        //Find any quests that start as active
        UpdateActiveQuests();
    }

    /**
     * Given a quest ID, reports if the quest is complete
     * */
    public bool QuestIDComplete(int id)
    {
        if (questTable.ContainsKey(id))
        {
            return questTable[id].isComplete();
        }

        Debug.Log("Quest ID " + id + " is not found in the quest table");
        return false;
    }

    public int GetQuestStep(int questID)
    {

        if (!questTable.ContainsKey(questID))
        {
            return -1;
        }

        return questTable[questID].GetCurrentStep();
    }

    /**
     * Sends a message to all active quests
     * */
    public void Notify(string notification)
    {

        List<Quest> completed = new List<Quest>();

        //Notifies every active quest of the message
        foreach(Quest quest in activeQuestList)
        {
            quest.Notify(notification);
            //Checks if quest is now complete
            if (quest.isComplete())
            {
                completed.Add(quest);
            }
        }

        //The length of the active quest list before finished quests are removed
        int ogLength = activeQuestList.Count;

        //removes all quests that are completed
        foreach(Quest quest in completed)
        {
            activeQuestList.Remove(quest);
        }

        //If the length has not changed, then just return after adding new quests
        if(ogLength == activeQuestList.Count)
        {
            UpdateActiveQuests();
            return;
        }

        //If the current quest is 1 less than the length, then just move to the new end
        if(currentQuest == ogLength - 1)
        {
            currentQuest = activeQuestList.Count - 1;
            if (currentQuest < 0)
            {
                currentQuest = 0;
            }
            UpdateActiveQuests();
            return;
        }

        //If the length is odd, subtract a 1 for an offset
        if(ogLength % 2 == 1)
        {
            currentQuest -= 1;

        }

        if (currentQuest < 0)
        {
            currentQuest = 0;
        }

        UpdateActiveQuests();

    }

    /**
     * Updates the active quest list with any quests that have their pre-reqs now met
     * */
    private void UpdateActiveQuests()
    {
        activeQuestList.Clear();
        foreach(Quest quest in questTable.Values)
        {
            Quest accessQuest = quest;

            //Check every pre-req and if all pre-reqs are met, then report that the reqs are met
            bool reqsMet = true;
            foreach(int req in accessQuest.getReqs())
            {
                if (questTable.ContainsKey(req))
                {
                    if (!questTable[req].isComplete())
                    {
                        reqsMet = false;
                    }
                }
            }

            //If the reqs are met and it is neither already added nor complete, then add to the active list
            if (reqsMet && !activeQuestList.Contains(accessQuest) && !accessQuest.isComplete())
            {
                activeQuestList.Add(accessQuest);
            }
        }
    }

    public string QuestToString(Quest quest)
    {
        string toReturn = "";
        toReturn += quest.getID();
        toReturn += " ";
        toReturn += quest.isComplete();

        return toReturn;
    }

    /**
     * Returns every current action that is in use
     * */
    public List<string> GetActions()
    {
        List<string> actions = new List<string>();
        foreach(Quest quest in activeQuestList)
        {
            foreach(string readAction in quest.GetActions())
            {
                actions.Add(readAction);
            }
        }
        return actions;
    }

    public void LoadQuestCompletion(string destination)
    {
        //Debug.Log("Loading from " + destination);

        string data = "";

        if (File.Exists(destination))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(destination, FileMode.Open);

            data = bf.Deserialize(stream) as string;
            stream.Close();
        }
        else
        {
            Debug.Log("Cannot find file " + destination);
            return;
        }

        StreamReader reader = new StreamReader(destination);
        reader.Peek();
        string nextLine = reader.ReadLine();

        while ( (nextLine = reader.ReadLine()) != null)
        {
            string[] vals = nextLine.Split();
            int.TryParse(vals[0], out int questID);

            if (!this.questTable.ContainsKey(questID))
            {
                //Debug.Log("QUEST TABLE DOES NOT CONTAIN KEY " + questID);
            }else{

                Quest loadQuest = questTable[questID];

                int.TryParse(vals[1], out int isCompleted);

                if(isCompleted == 1)
                {
                    loadQuest.SetComplete(true);
                }
                else
                {
                    loadQuest.SetComplete(false);

                    int.TryParse(vals[2], out int step);

                    int.TryParse(vals[3], out int numActions);

                    for (int i = 0; i < numActions; i++)
                    {
                        string readLine = reader.ReadLine();
                        loadQuest.LoadData(step, readLine);
                    }
                    questTable[questID] = loadQuest;
                }
            }
        }

        reader.Close();
        UpdateActiveQuests();
    }

    public void SaveQuestCompletion(string destination)
    {

        string saveData = "\n";

        //Debug.Log("Saving to " + destination);

        foreach(Quest quest in questTable.Values)
        {
            saveData += quest.GetQuestSaveInfo();
            
        }

        FileStream stream = new FileStream(destination, FileMode.Create);

        BinaryFormatter bf = new BinaryFormatter();

        bf.Serialize(stream, saveData);
        stream.Close();

        
    }

    public Quest ChangeQuest(int change)
    {
        if(activeQuestList.Count == 0)
        {
            currentQuest = 0;
            return null;
        }

        currentQuest += change;

        if(currentQuest < 0)
        {
            currentQuest += activeQuestList.Count;
        }

        currentQuest %= activeQuestList.Count;

        return (CheckAllHidden(activeQuestList[currentQuest]) ? null : activeQuestList[currentQuest]);
    }

    public Quest GetCurrentQuest()
    {
        if (activeQuestList.Count == 0)
        {
            return null;
        }

        if (CheckAllHidden(activeQuestList[currentQuest]))
        {
            currentQuest = FindNextNonHidden();
            return null;
        }

        return activeQuestList[currentQuest];
    }

    /**
     * Iterates through every action in the current quest's current quest step and sees if all are hidden
     * */
    private bool CheckAllHidden(Quest check)
    {
        foreach(QuestAction action in check.getQuestStepActions())
        {
            if (!action.GetHidden())
            {
                return false;
            }
        }
        return true;
    }

    /**
     * Finds next quest where not every action is hidden
     * */
    private int FindNextNonHidden()
    {
        //Start at current quest and go up
        for(int i = currentQuest; i < activeQuestList.Count; i++)
        {
            if (!CheckAllHidden(activeQuestList[i]))
            {
                return i;
            }
        }
        //Start at 0 and go back up to current quest
        for (int i = 0; i < currentQuest; i++)
        {
            if (!CheckAllHidden(activeQuestList[i]))
            {
                return i;
            }
        }

        return 0;
    }

}
