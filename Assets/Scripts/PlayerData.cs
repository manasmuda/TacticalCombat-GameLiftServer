using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData
{
    public string playerId;

    private static readonly System.Random _random = new System.Random();

    public Dictionary<string, object> king;
    public Dictionary<string, object> lord1;
    public Dictionary<string, object> lord2;
    public Dictionary<string, object> commanderk;
    public Dictionary<string, object> commanderl1;
    public Dictionary<string, object> commanderl2;
    public Dictionary<string,Dictionary<string, object>> knights;
    public Dictionary<string,Dictionary<string, object>> soldiers;
    public Dictionary<int, string> positionIdMatcher;

    public List<string> fort1;
    public List<string> fort2;


    public int posMultiplier;
    public int offset;
    public int fDir;
    public float angleYOffSet;

    public PlayerData(string id)
    {
        this.playerId = id;
        this.knights = new Dictionary<string,Dictionary<string, object>> { };
        this.soldiers = new Dictionary<string,Dictionary<string, object>> { };
        this.positionIdMatcher = new Dictionary<int, string> { };
        this.fort1 = new List<string> { };
        this.fort2 = new List<string> { };
    }

    public void InitializeKing(int x, int y,string id)
    {
        king = new Dictionary<string, object> { };
        king.Add("id", id);
        king.Add("color", "gold");
        king.Add("teamId", playerId);
        king.Add("posI", x);
        king.Add("posJ", y);
        king.Add("state", "alive");
        positionIdMatcher.Add(x*10+y,id);
    }

    public void InitializeLord1(int x,int y,string id)
    {
        lord1 = new Dictionary<string,object> { };
        lord1.Add("id", id);
        lord1.Add("color", "gold");
        lord1.Add("teamId", playerId);
        lord1.Add("posI", x);
        lord1.Add("posJ", y);
        lord1.Add("state", "alive");
        positionIdMatcher.Add(x * 10 + y, id);
    }

    public void InitializeLord2(int x,int y,string id)
    {
        lord2 = new Dictionary<string,object> { };
        lord2.Add("id", id);
        lord2.Add("color", "gold");
        lord2.Add("teamId", playerId);
        lord2.Add("posI", x);
        lord2.Add("posJ", y);
        lord2.Add("state", "alive");
        positionIdMatcher.Add(x * 10 + y, id);
    }

    public void InitializeCommanderK(int x, int y,string id)
    {
        commanderk = new Dictionary<string,object> { };
        commanderk.Add("id", id);
        commanderk.Add("color", "silver");
        commanderk.Add("teamId", playerId);
        commanderk.Add("posI", x);
        commanderk.Add("posJ", y);
        commanderk.Add("state", "alive");
        commanderk.Add("serves", "king");
        positionIdMatcher.Add(x * 10 + y, id);
    }

    public void InitializeCommanderL1(int x, int y, string id)
    {
        commanderl1 = new Dictionary<string, object> { };
        commanderl1.Add("id", id);
        commanderl1.Add("color", "silver");
        commanderl1.Add("teamId", playerId);
        commanderl1.Add("posI", x);
        commanderl1.Add("posJ", y);
        commanderl1.Add("state", "alive");
        commanderl1.Add("serves", "lord1");
        positionIdMatcher.Add(x * 10 + y, id);
    }

    public void InitializeCommanderL2(int x, int y, string id)
    {
        commanderl2 = new Dictionary<string,object> { };
        commanderl2.Add("id", id);
        commanderl2.Add("color", "silver");
        commanderl2.Add("teamId", playerId);
        commanderl2.Add("posI", x);
        commanderl2.Add("posJ", y);
        commanderl2.Add("state", "alive");
        commanderl2.Add("serves", "lord2");
        positionIdMatcher.Add(x * 10 + y, id);
    }

    public void addSoldier(int x,int y,int power,string serves,string id)
    {
        Dictionary<string, object> dict = new Dictionary<string, object> { };
        dict.Add("id", id);
        dict.Add("posI",x);
        dict.Add("posJ", y);
        dict.Add("power", power);
        dict.Add("serves", serves);
        dict.Add("color","bronze");
        dict.Add("state", "alive");
        soldiers.Add(Convert.ToString(dict["id"]),dict);
        positionIdMatcher.Add(x * 10 + y, id);
    }

    public void addKnight(int x, int y, int power, string serves,string id)
    {
        Dictionary<string, object> dict = new Dictionary<string, object> { };
        dict.Add("id", id);
        dict.Add("posI", x);
        dict.Add("posJ", y);
        dict.Add("power", power);
        dict.Add("serves", serves);
        dict.Add("color", "silver");
        dict.Add("state", "alive");
        knights.Add(Convert.ToString(dict["id"]),dict);
        positionIdMatcher.Add(x * 10 + y, id);
    }

    /*public string updatePos(int pos,string type,string x,string y)
    {
        if (type == "king")
        {
            king["posX"] = x;
            king["posY"] = y;
            return king["id"];
        }
        else if (type == "lord1")
        {
            lord1["posX"] = x;
            lord2["posY"] = y;
            return lord1["id"];
        }
        else if (type == "lord2")
        {
            lord2["posX"] = x;
            lord2["posY"] = y;
            return lord2["id"];
        }
        else if (type == "commanderk")
        {
            commanderk["posX"] = x;
            commanderk["posY"] = y;
            return commanderk["id"];
        }
        else if (type == "commanderl1")
        {
            commanderl1["posX"] = x;
            commanderl1["posY"] = y;
            return commanderl1["id"];
        }
        else if (type == "commanderl2")
        {
            commanderl2["posX"] = x;
            commanderl2["posY"] = y;
            return commanderl2["id"];
        }
        else if (type == "knight")
        {
            knights[pos]["posX"] = x;
            knights[pos]["posY"] = y;
            return knights[pos]["id"];
        }
        else if (type == "soldier")
        {
            soldiers[pos]["posX"] = x;
            soldiers[pos]["posY"] = y;
            return soldiers[pos]["id"];
        }
        else return "0";
    }

    public string updateState(string type,int pos)
    {
        if (type == "commanderl1")
        {
            commanderl1["state"] = "injure";
            return "I1" + commanderl1["id"];
        }
        else if (type == "commanderl2")
        {
            commanderl2["state"] = "injure";
            return "I1" + commanderl2["id"];
        }
        else if (type == "knight")
        {
            if (knights[pos]["serves"] != "king")
            {
                knights[pos]["state"] = "injure";
                if (knights[pos]["serves"] == "lord1")
                    return "I1" + knights[pos]["id"];
                else
                    return "I2" + knights[pos]["id"];
            }
            else
            {
                knights[pos]["state"] = "dead";
                knights.RemoveAt(pos);
                return "D" + knights[pos]["id"];
            }

        }
        else if (type == "soldier")
        {
            if (soldiers[pos]["serves"] != "king")
            {
                soldiers[pos]["state"] = "injure";
                if (soldiers[pos]["serves"] == "lord1")
                    return "I1" + soldiers[pos]["id"];
                else
                    return "I2" + soldiers[pos]["id"];
            }
            else
            {
                soldiers[pos]["state"] = "dead";
                soldiers.RemoveAt(pos);
                return "D" + soldiers[pos]["id"];
            }
        }
        else return "0";
    }*/

    public List<Dictionary<string, object>> convertToOppData()
    {
        List<Dictionary<string, object>> tempList = new List<Dictionary<string, object>> { };
        Dictionary<string, object> tempKing = new Dictionary<string, object> { };
        tempKing.Add("id", king["id"]);
        tempKing.Add("color", "gold");
        tempKing.Add("posI", king["posI"]);
        tempKing.Add("posJ", king["posJ"]);
        tempKing.Add("state", "alive");
        tempList.Add(tempKing);
        Dictionary<string, object> templord1 = new Dictionary<string, object> { };
        templord1.Add("id", lord1["id"]);
        templord1.Add("color", "gold");
        templord1.Add("posI", lord1["posI"]);
        templord1.Add("posJ", lord1["posJ"]);
        templord1.Add("state", "alive");
        tempList.Add(templord1);
        Dictionary<string, object> templord2 = new Dictionary<string, object> { };
        templord2.Add("id", lord2["id"]);
        templord2.Add("color", "gold");
        templord2.Add("posI", lord2["posI"]);
        templord2.Add("posJ", lord2["posJ"]);
        templord2.Add("state", "alive");
        tempList.Add(templord2);
        Dictionary<string, object> tempCK = new Dictionary<string, object> { };
        tempCK.Add("id", commanderk["id"]);
        tempCK.Add("color", "silver");
        tempCK.Add("posI", commanderk["posI"]);
        tempCK.Add("posJ", commanderk["posJ"]);
        tempCK.Add("state", "alive");
        tempList.Add(tempCK);
        Dictionary<string, object> tempCL1 = new Dictionary<string, object> { };
        tempCL1.Add("id", commanderl1["id"]);
        tempCL1.Add("color", "silver");
        tempCL1.Add("posI", commanderl1["posI"]);
        tempCL1.Add("posJ", commanderl1["posJ"]);
        tempCL1.Add("state", "alive");
        tempList.Add(tempCL1);
        Dictionary<string, object> tempCL2 = new Dictionary<string, object> { };
        tempCL2.Add("id", commanderl2["id"]);
        tempCL2.Add("color", "silver");
        tempCL2.Add("posI", commanderl2["posI"]);
        tempCL2.Add("posJ", commanderl2["posJ"]);
        tempCL2.Add("state", "alive");
        tempList.Add(tempCL2);
        List<Dictionary<string, object>> tempValues = new List<Dictionary<string, object>>(knights.Values);
        for(int i = 0; i < tempValues.Count; i++)
        {
            Dictionary<string, object> tempDict = new Dictionary<string, object> { };
            tempDict.Add("id", tempValues[i]["id"]);
            tempDict.Add("color", "silver");
            tempDict.Add("posI", tempValues[i]["posI"]);
            tempDict.Add("posJ", tempValues[i]["posJ"]);
            tempDict.Add("state", "alive");
            tempList.Add(tempDict);
        }
        tempValues = new List<Dictionary<string, object>>(soldiers.Values); ;
        for (int i = 0; i < tempValues.Count; i++)
        {
            Dictionary<string, object> tempDict = new Dictionary<string, object> { };
            tempDict.Add("id", tempValues[i]["id"]);
            tempDict.Add("color", "bronze");
            tempDict.Add("posI",tempValues[i]["posI"]);
            tempDict.Add("posJ", tempValues[i]["posJ"]);
            tempDict.Add("state", "alive");
            tempList.Add(tempDict);
        }
        return tempList;
    }
    public static string RandomString(int size)
    {
        var builder = new StringBuilder(size);

        char offset = 'A';
        const int lettersOffset = 26; // A...Z or a..z: length=26  

        for (var i = 0; i < size; i++)
        {
            var @char = (char)_random.Next(offset, offset + lettersOffset);
            builder.Append(@char);
        }

        return builder.ToString();
    }
}
