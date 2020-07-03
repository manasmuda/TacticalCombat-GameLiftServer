using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aws.GameLift.Server;


// *** MONOBEHAVIOUR TO MANAGE SERVER LOGIC *** //

public class Server : MonoBehaviour
{
    //We get events back from the NetworkServer through this static list
    public List<SimpleMessage> messagesToProcess = new List<SimpleMessage>();

    public PlayerData player1 = null;
    public PlayerData player2 = null;

    public List<int> boardPosStates = new List<int> { };

    public List<string> ArmyAlias = new List<string> { "king", "commander_king", "knight_king_7", "knight_king_6", "knight_king_5", "soldier_king_4", "soldier_king_3_1", "soldier_king_3_2", "soldier_king_2_1", "soldier_king_2_2", "soldier_king_1_1", "soldier_king_1_2", "lord1", "commander_lord1", "knight_lord1_7", "knight_lord1_6", "knight_lord1_5", "soldier_lord1_4", "soldier_lord1_3", "soldier_lord1_2", "soldier_lord1_1", "lord2", "commander_lord2", "knight_lord2_7", "knight_lord2_6", "knight_lord2_5", "soldier_lord2_4", "soldier_lord2_3", "soldier_lord2_2", "soldier_lord2_1" };
    public List<string> Armyidkeys = new List<string> { "ki", "ck", "kn", "kn", "kn", "so", "so", "so", "so", "so", "so", "so", "l1", "c1", "kn", "kn", "kn", "so", "so", "so", "so", "l2", "c2", "kn", "kn", "kn", "so", "so", "so", "so" };

    public List<string> ids1;
    public List<string> ids2;

    public bool gameStarted = false;
    public bool gameReadyStateP1 = false;
    public bool gameReadyStateP2 = false;

    public float timer = 0.0f;
    public bool timerMode = false;

    public bool gameReady = false;
    public string turn = null;
    public string pTurn = null;

    public Coroutine turnTimer;

    public bool test = true;

    NetworkServer nserver;

    public void InitializePlayer(string id)
    {
        PlayerData playerData = new PlayerData(id);

        if (id == "1")
        {
            playerData.posMultiplier = 1;
            playerData.offset = 70;
            playerData.fDir = -1;
            player1 = playerData;
        }
        else if (id == "2")
        {
            playerData.posMultiplier = -1;
            playerData.offset = 29;
            playerData.fDir = 1;
            player2 = playerData;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        var gameliftServer = GameObject.FindObjectOfType<GameLift>();
        nserver = new NetworkServer(gameliftServer, this);
    }

    public void resetServer()
    {
        pTurn = null;
        turn = null;
        timer = 0.0f;
        timerMode = false;
        ids1 = null;
        ids2 = null;
        gameStarted = false;
        gameReady = false;
        player1 = null;
        player2 = null;
        messagesToProcess.Clear();
    }
    // Update is called once per frame
    void Update()
    {
        nserver.Update();

        /*if (timerMode)
        {
            timer = timer - Time.deltaTime;
        }

        if (timer < -0.5f)
        {
            timerMode = false;
        }*/

        // Go through any messages to process (on the game world)
        foreach (SimpleMessage msg in messagesToProcess)
        {
            // NOTE: We should spawn players and set positions also on server side here and validate actions. For now we just pass this data to clients
            if (msg.messageType == MessageType.PlayerStrategy)
            {
                HandlePlayerStrategyRecieved(msg);
            }
            else if (msg.messageType == MessageType.PlayerMove)
            {
                HandlePlayerMove(msg);
            }
        }
        messagesToProcess.Clear();
    }

    public void CallCheckGameReady()
    {
        StartCoroutine(CheckGameReady());
    }

    IEnumerator CheckGameReady()
    {
        boardPosStates = new List<int> { };

        for (int i = 0; i < 100; i++)
        {
            boardPosStates.Add(0);
        }
        boardPosStates[40] = -1;
        boardPosStates[43] = -1;
        boardPosStates[46] = -1;
        boardPosStates[49] = -1;
        boardPosStates[50] = -1;
        boardPosStates[53] = -1;
        boardPosStates[56] = -1;
        boardPosStates[59] = -1;

        yield return new WaitForSeconds(60);

        if (gameStarted && !gameReady)
        {
            if (gameReadyStateP1 && !gameReadyStateP2)
            {
                //P1 wins other player left
            }
            else if (gameReadyStateP2 && !gameReadyStateP1)
            {
                //P2 wins other player left
            }
            else if (!gameReadyStateP1 && !gameReadyStateP2)
            {
                //Both players failed to join
            }
            timerMode = false;
            gameStarted = false;
        }
    }

    IEnumerator SendTurnChanged()
    {
        Debug.Log("SetTurnChange Started");
        yield return new WaitForSeconds(5);

        if (pTurn == "2")
        {
            turn = "1";
        }
        else if (pTurn == "1")
        {
            turn = "2";
        }
        Debug.Log("Current turn:" + turn);
        SimpleMessage msg = new SimpleMessage(MessageType.TurnChange);
        msg.turnId = turn;
        nserver.SendM(0, msg);
        nserver.SendM(1, msg);
        timer = 35;
        timerMode = true;
        turnTimer = StartCoroutine(CheckTurnStatus());
        Debug.Log("SetTurnChange Ended");
    }

    IEnumerator CheckTurnStatus()
    {
        Debug.Log("Set CheckTurnStatus Started");
        yield return new WaitForSeconds(35);

        if (turn != null)
        {
            pTurn = turn;
            turn = null;
            timer = 0f;
            timerMode = false;
            turnTimer = null;
            StartCoroutine(SendTurnChanged());
        }
        Debug.Log("Set CheckTurnStatus Ended");
    }

    public void HandlePlayerMove(SimpleMessage msg)
    {
        Debug.Log("Handling Player Move Started");
        if (msg.playerId == turn && timerMode)
        {
            StopCoroutine(turnTimer);
            turnTimer = null;
            pTurn = turn;
            turn = null;
            timer = 0f;
            timerMode = false;
            int source = Convert.ToInt32(msg.dictData["source"]);
            int dest = Convert.ToInt32(msg.dictData["dest"]);
            SimpleMessage msg1 = new SimpleMessage(MessageType.BattleResult);
            SimpleMessage msg2 = new SimpleMessage(MessageType.BattleResult);
            if (msg.playerId == "1")
            {
                Debug.Log("Handling Player Move:1");
                string id1 = Convert.ToString(msg.dictData["id"]);
                string type1 = "";
                Dictionary<string, object> pieceData1 = new Dictionary<string, object> { };
                Debug.Log("id1:" + id1);
                if (boardPosStates[source] == 1)
                {
                    if (id1.Substring(0, 2).Equals("ki") && Convert.ToString(player1.king["id"]) == id1)
                    {
                        type1 = "king";
                        pieceData1 = player1.king;
                    }
                    else if (id1.Substring(0, 2).Equals("l1") && Convert.ToString(player1.lord1["id"]) == id1)
                    {
                        type1 = "lord";
                        pieceData1 = player1.lord1;
                    }
                    else if (id1.Substring(0, 2).Equals("l2") && Convert.ToString(player1.lord2["id"]) == id1)
                    {
                        type1 = "lord";
                        pieceData1 = player1.lord2;
                    }
                    else if (id1.Substring(0, 2).Equals("ck") && Convert.ToString(player1.commanderk["id"]) == id1)
                    {
                        type1 = "commander";
                        pieceData1 = player1.commanderk;
                    }
                    else if (id1.Substring(0, 2).Equals("c1") && Convert.ToString(player1.commanderl1["id"]) == id1)
                    {
                        type1 = "commander";
                        pieceData1 = player1.commanderl1;
                    }
                    else if (id1.Substring(0, 2).Equals("c2") && Convert.ToString(player1.commanderl2["id"]) == id1)
                    {
                        type1 = "commander";
                        pieceData1 = player1.commanderl2;
                    }
                    else if (id1.Substring(0, 2).Equals("kn") && player1.knights.ContainsKey(id1))
                    {
                        type1 = "knight";
                        pieceData1 = player1.knights[id1];
                    }
                    else if (id1.Substring(0, 2).Equals("so") && player1.soldiers.ContainsKey(id1))
                    {
                        type1 = "soldier";
                        pieceData1 = player1.soldiers[id1];
                    }

                    if (boardPosStates[dest] == 2)
                    {
                        Debug.Log("Handling Player Move:1:Moving onto opponent");
                        string id2 = player2.positionIdMatcher[dest];
                        string type2 = "";
                        Dictionary<string, object> pieceData2 = new Dictionary<string, object> { };
                        if (id2.Substring(0, 2).Equals("ki") && Convert.ToString(player2.king["id"]) == id2)
                        {
                            type2 = "king";
                            pieceData2 = player2.king;
                        }
                        else if (id2.Substring(0, 2).Equals("l1") && Convert.ToString(player2.lord1["id"]) == id2)
                        {
                            type2 = "lord";
                            pieceData2 = player2.lord1;
                        }
                        else if (id2.Substring(0, 2).Equals("l2") && Convert.ToString(player2.lord2["id"]) == id2)
                        {
                            type2 = "lord";
                            pieceData2 = player2.lord2;
                        }
                        else if (id2.Substring(0, 2).Equals("ck") && Convert.ToString(player2.commanderk["id"]) == id2)
                        {
                            type2 = "commander";
                            pieceData2 = player2.commanderk;
                        }
                        else if (id2.Substring(0, 2).Equals("c1") && Convert.ToString(player2.commanderl1["id"]) == id2)
                        {
                            type2 = "commander";
                            pieceData2 = player2.commanderl1;
                        }
                        else if (id2.Substring(0, 2).Equals("c2") && Convert.ToString(player2.commanderl2["id"]) == id2)
                        {
                            type2 = "commander";
                            pieceData2 = player2.commanderl2;
                        }
                        else if (id2.Substring(0, 2).Equals("kn") && player2.knights.ContainsKey(id2))
                        {
                            type2 = "knight";
                            pieceData2 = player2.knights[id2];
                        }
                        else if (id2.Substring(0, 2).Equals("so") && player2.soldiers.ContainsKey(id2))
                        {
                            type2 = "soldier";
                            pieceData2 = player2.soldiers[id2];
                        }
                        if (type1 == "king")
                        {
                            Debug.Log("Type 1: King");
                            if (type2 == "soldier" || type2 == "knight")
                            {
                                Debug.Log("Type 2: Soilder || Knight");
                                Dictionary<string, object> temp11 = new Dictionary<string, object> { };
                                Dictionary<string, object> temp21 = new Dictionary<string, object> { };
                                temp11.Add("myTeam", false);
                                temp11.Add("id", id2);
                                temp21.Add("myTeam", true);
                                temp21.Add("id", id2);
                                if (pieceData2["serves"].ToString() == "king")
                                {
                                    temp11.Add("action", "dead");
                                    temp21.Add("action", "dead");
                                    if (type2 == "soldier")
                                    {
                                        temp11.Add("color", "bronze");
                                        temp21.Add("color", "bronze");
                                        player2.soldiers[id2]["state"] = "dead";
                                    }
                                    else if (type2 == "knight")
                                    {
                                        temp11.Add("color", "silver");
                                        temp21.Add("color", "silver");
                                        player2.knights[id2]["state"] = "dead";
                                    }
                                }
                                else
                                {
                                    temp11.Add("action", "injured");
                                    temp21.Add("action", "injured");
                                    if (pieceData2["serves"].ToString() == "lord1")
                                    {
                                        temp11.Add("fort", "1");
                                        temp21.Add("fort", "1");
                                        player2.fort1.Add(id2);
                                    }
                                    else if (pieceData2["serves"].ToString() == "lord2")
                                    {
                                        temp11.Add("fort", "2");
                                        temp21.Add("fort", "2");
                                        player2.fort2.Add(id2);
                                    }
                                    if (type2 == "soldier")
                                    {
                                        temp11.Add("color", "bronze");
                                        temp21.Add("color", "bronze");
                                        player2.soldiers[id2]["state"] = "injured";
                                    }
                                    else
                                    {
                                        temp11.Add("color", "silver");
                                        temp21.Add("color", "silver");
                                        player2.knights[id2]["state"] = "injured";
                                    }
                                }
                                player2.positionIdMatcher.Remove(dest);
                                msg1.listdictdata.Add(temp11);
                                msg2.listdictdata.Add(temp21);

                            }
                            else if (type2 == "lord")
                            {
                                bool win = false;
                                if (id2.Substring(0, 2).Equals("l1"))
                                {
                                    if (player2.commanderl1["state"] != "alive")
                                    {
                                        win = true;
                                    }
                                }
                                else if (id2.Substring(0, 2).Equals("l2"))
                                {
                                    if (player2.commanderl2["state"] != "alive")
                                    {
                                        win = true;
                                    }
                                }
                                if (win)
                                {
                                    Dictionary<string, object> temp11 = new Dictionary<string, object> { };
                                    Dictionary<string, object> temp21 = new Dictionary<string, object> { };
                                    temp11.Add("myTeam", false);
                                    temp21.Add("myTeam", true);
                                    temp11.Add("id", id2);
                                    temp21.Add("id", id2);
                                    temp11.Add("action", "dead");
                                    temp21.Add("action", "dead");
                                    temp11.Add("color", "gold");
                                    temp21.Add("color", "gold");
                                    msg1.listdictdata.Add(temp11);
                                    msg2.listdictdata.Add(temp21);
                                    if (id2.Substring(0, 2).Equals("l1"))
                                    {
                                        player2.lord1["state"] = "dead";
                                        for (int i = 0; i < player2.fort1.Count; i++)
                                        {
                                            string tempId = player2.fort1[i];
                                            Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                            Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                            if (tempId.Substring(0, 1).Equals("c") || (tempId.Substring(0, 2).Equals("kn") && Convert.ToInt32(player2.knights[tempId]["power"]) == 7))
                                            {
                                                if (tempId.Substring(0, 1).Equals("c"))
                                                {
                                                    player2.commanderl1["state"] = "opp";
                                                }
                                                else if (tempId.Substring(0, 2).Equals("kn"))
                                                {
                                                    player2.knights[tempId]["state"] = "opp";
                                                }
                                                int tempPos = findAndFillPlace("1");
                                                string newId = "kn" + PlayerData.RandomString(7);
                                                player1.addKnight(tempPos / 10, tempPos % 10, 5, "king", newId);
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("type", "knight");
                                                temp1.Add("color", "silver");
                                                temp1.Add("power", 5);
                                                temp1.Add("pos", tempPos);
                                                temp2.Add("myTeam", false);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("color", "silver");
                                                temp2.Add("pos", tempPos);
                                            }
                                            else if (tempId.Substring(0, 2).Equals("kn"))
                                            {
                                                player2.knights[tempId]["state"] = "opp";
                                                int tempPos = findAndFillPlace("1");
                                                string newId = "so" + PlayerData.RandomString(7);
                                                int tempPower = 0;
                                                if (Convert.ToInt32(player2.knights[tempId]["power"]) == 6)
                                                {
                                                    tempPower = 4;
                                                }
                                                else if (Convert.ToInt32(player2.knights[tempId]["power"]) == 5)
                                                {
                                                    tempPower = 3;
                                                }
                                                player1.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("type", "soldier");
                                                temp1.Add("color", "bronze");
                                                temp1.Add("power", tempPower);
                                                temp1.Add("pos", tempPos);
                                                temp2.Add("myTeam", false);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("color", "bronze");
                                                temp2.Add("pos", tempPos);
                                            }
                                            else if (tempId.Substring(0, 2).Equals("so"))
                                            {
                                                player2.soldiers[tempId]["state"] = "opp";
                                                int tempPos = findAndFillPlace("1");
                                                string newId = "so" + PlayerData.RandomString(7);
                                                int tempPower = 1;
                                                if (Convert.ToInt32(player2.soldiers[tempId]["power"]) == 4)
                                                {
                                                    tempPower = 2;
                                                }
                                                player1.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("type", "soldier");
                                                temp1.Add("color", "bronze");
                                                temp1.Add("power", tempPower);
                                                temp1.Add("pos", tempPos);
                                                temp2.Add("myTeam", false);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("color", "bronze");
                                                temp2.Add("pos", tempPos);
                                            }
                                            msg1.listdictdata.Add(temp1);
                                            msg2.listdictdata.Add(temp2);
                                        }
                                        if (Convert.ToString(player1.commanderl1["state"]) == "alive")
                                        {
                                            //player1.commanderl1["serves"] = "king";
                                            player1.commanderl1["state"] = "change";
                                            string tempid1 = Convert.ToString(player1.commanderl1["id"]);
                                            tempid1 = "kn" + tempid1.Substring(2);
                                            player1.positionIdMatcher.Remove(Convert.ToInt32(player1.commanderl1["posI"]) * 10 + Convert.ToInt32(player1.commanderl1["posJ"]));
                                            player1.addKnight(Convert.ToInt32(player1.commanderl1["posI"]), Convert.ToInt32(player1.commanderl1["posJ"]), 5, "king", tempid1);
                                            Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                            Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                            temp2.Add("myTeam", false);
                                            temp2.Add("id", player1.commanderl1["id"]);
                                            temp2.Add("action", "changeserves");
                                            temp2.Add("newId", tempid1);
                                            temp2.Add("color", "silver");
                                            temp1.Add("myTeam", true);
                                            temp1.Add("id", player1.commanderl1["id"]);
                                            temp1.Add("action", "changeserves");
                                            temp1.Add("newId", tempid1);
                                            temp1.Add("color", "silver");
                                            msg1.listdictdata.Add(temp1);
                                            msg2.listdictdata.Add(temp2);
                                        }
                                        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in player1.knights)
                                        {
                                            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord1")
                                            {
                                                tempItem.Value["power"] = 5;
                                                tempItem.Value["serves"] = "king";
                                                player1.knights[tempItem.Key] = tempItem.Value;
                                                Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", tempItem.Value["id"]);
                                                temp1.Add("action", "changeserves");
                                                temp1.Add("newId", tempItem.Value["id"]);
                                                temp1.Add("color", "silver");
                                                msg1.listdictdata.Add(temp1);
                                            }
                                        }
                                        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in player1.soldiers)
                                        {
                                            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord1")
                                            {
                                                tempItem.Value["power"] = 1;
                                                tempItem.Value["serves"] = "king";
                                                player1.knights[tempItem.Key] = tempItem.Value;
                                                Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", tempItem.Value["id"]);
                                                temp1.Add("action", "changeserves");
                                                temp1.Add("newId", tempItem.Value["id"]);
                                                temp1.Add("color", "bronze");
                                                msg1.listdictdata.Add(temp1);
                                            }
                                        }
                                        Dictionary<string, object> temp12 = new Dictionary<string, object> { };
                                        Dictionary<string, object> temp22 = new Dictionary<string, object> { };
                                        temp12.Add("myTeam", false);
                                        temp12.Add("id", "");
                                        temp12.Add("action", "clearfort");
                                        temp12.Add("fort", "1");
                                        temp22.Add("myTeam", true);
                                        temp22.Add("id", "");
                                        temp22.Add("action", "clearfort");
                                        temp22.Add("fort", "1");
                                        player2.fort1 = null;
                                        player2.positionIdMatcher.Remove(dest);
                                        msg1.listdictdata.Add(temp12);
                                        msg2.listdictdata.Add(temp22);
                                    }
                                    else if (id2.Substring(0, 2).Equals("l2"))
                                    {
                                        player2.lord2["state"] = "dead";
                                        for (int i = 0; i < player2.fort2.Count; i++)
                                        {
                                            string tempId = player2.fort2[i];
                                            Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                            Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                            if (tempId.Substring(0, 1).Equals("c") || (tempId.Substring(0, 2).Equals("kn") && Convert.ToInt32(player2.knights[tempId]["power"]) == 7))
                                            {
                                                if (tempId.Substring(0, 1).Equals("c"))
                                                {
                                                    player2.commanderl2["state"] = "opp";
                                                }
                                                else if (tempId.Substring(0, 2).Equals("kn"))
                                                {
                                                    player2.knights[tempId]["state"] = "opp";
                                                }
                                                int tempPos = findAndFillPlace("1");
                                                string newId = "kn" + PlayerData.RandomString(7);
                                                player1.addKnight(tempPos / 10, tempPos % 10, 5, "king", newId);
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("type", "knight");
                                                temp1.Add("color", "silver");
                                                temp1.Add("power", 5);
                                                temp1.Add("pos", tempPos);
                                                temp2.Add("myTeam", false);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("color", "silver");
                                                temp2.Add("pos", tempPos);
                                            }
                                            else if (tempId.Substring(0, 2).Equals("kn"))
                                            {
                                                player2.knights[tempId]["state"] = "opp";
                                                int tempPos = findAndFillPlace("1");
                                                string newId = "so" + PlayerData.RandomString(7);
                                                int tempPower = 0;
                                                if (Convert.ToInt32(player2.knights[tempId]["power"]) == 6)
                                                {
                                                    tempPower = 4;
                                                }
                                                else if (Convert.ToInt32(player2.knights[tempId]["power"]) == 5)
                                                {
                                                    tempPower = 3;
                                                }
                                                player1.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("type", "soldier");
                                                temp1.Add("color", "bronze");
                                                temp1.Add("power", tempPower);
                                                temp1.Add("pos", tempPos);
                                                temp2.Add("myTeam", false);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("color", "bronze");
                                                temp2.Add("pos", tempPos);
                                            }
                                            else if (tempId.Substring(0, 2).Equals("so"))
                                            {
                                                player2.soldiers[tempId]["state"] = "opp";
                                                int tempPos = findAndFillPlace("1");
                                                string newId = "so" + PlayerData.RandomString(7);
                                                int tempPower = 1;
                                                if (Convert.ToInt32(player2.soldiers[tempId]["power"]) == 4)
                                                {
                                                    tempPower = 2;
                                                }
                                                player1.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("type", "soldier");
                                                temp1.Add("color", "bronze");
                                                temp1.Add("power", tempPower);
                                                temp1.Add("pos", tempPos);
                                                temp2.Add("myTeam", false);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("color", "bronze");
                                                temp2.Add("pos", tempPos);
                                            }
                                            msg1.listdictdata.Add(temp1);
                                            msg2.listdictdata.Add(temp2);
                                        }
                                        if (Convert.ToString(player1.commanderl2["state"]) == "alive")
                                        {
                                            //player1.commanderl2["serves"] = "king";
                                            player1.commanderl2["state"] = "change";
                                            string tempid1 = Convert.ToString(player1.commanderl2["id"]);
                                            tempid1 = "kn" + tempid1.Substring(2);
                                            player1.positionIdMatcher.Remove(Convert.ToInt32(player1.commanderl2["posI"]) * 10 + Convert.ToInt32(player1.commanderl2["posJ"]));
                                            player1.addKnight(Convert.ToInt32(player1.commanderl2["posI"]), Convert.ToInt32(player1.commanderl2["posJ"]), 5, "king", tempid1);
                                            Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                            Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                            temp2.Add("myTeam", false);
                                            temp2.Add("id", player1.commanderl2["id"]);
                                            temp2.Add("action", "changeserves");
                                            temp2.Add("newId", tempid1);
                                            temp2.Add("color", "silver");
                                            temp1.Add("myTeam", true);
                                            temp1.Add("id", player1.commanderl2["id"]);
                                            temp1.Add("action", "changeserves");
                                            temp1.Add("newId", tempid1);
                                            temp1.Add("color", "silver");
                                            msg1.listdictdata.Add(temp1);
                                            msg2.listdictdata.Add(temp2);
                                        }
                                        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in player1.knights)
                                        {
                                            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord2")
                                            {
                                                tempItem.Value["power"] = 5;
                                                tempItem.Value["serves"] = "king";
                                                player1.knights[tempItem.Key] = tempItem.Value;
                                                Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", tempItem.Value["id"]);
                                                temp1.Add("action", "changeserves");
                                                temp1.Add("newId", tempItem.Value["id"]);
                                                temp1.Add("color", "silver");
                                                msg1.listdictdata.Add(temp1);
                                            }
                                        }
                                        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in player1.soldiers)
                                        {
                                            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord2")
                                            {
                                                tempItem.Value["power"] = 1;
                                                tempItem.Value["serves"] = "king";
                                                player1.knights[tempItem.Key] = tempItem.Value;
                                                Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                                temp1.Add("myTeam", true);
                                                temp1.Add("id", tempItem.Value["id"]);
                                                temp1.Add("action", "changeserves");
                                                temp1.Add("newId", tempItem.Value["id"]);
                                                temp1.Add("color", "bronze");
                                                msg1.listdictdata.Add(temp1);
                                            }
                                        }
                                        Dictionary<string, object> temp12 = new Dictionary<string, object> { };
                                        Dictionary<string, object> temp22 = new Dictionary<string, object> { };
                                        temp12.Add("myTeam", false);
                                        temp12.Add("id", "");
                                        temp12.Add("action", "clearfort");
                                        temp12.Add("fort", "2");
                                        temp22.Add("myTeam", true);
                                        temp22.Add("id", "");
                                        temp22.Add("action", "clearfort");
                                        temp22.Add("fort", "2");
                                        player2.fort2 = null;
                                        player2.positionIdMatcher.Remove(dest);
                                        msg1.listdictdata.Add(temp12);
                                        msg2.listdictdata.Add(temp22);
                                    }
                                }
                                else
                                {
                                    SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
                                    SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
                                    nserver.SendM(0, msgg1);
                                    nserver.SendM(1, msgg2);
                                    nserver.gamelift.TerminateGameSession();
                                    this.resetServer();
                                    nserver.ResetNetworkServer();
                                    return;
                                }
                            }
                            else if (type2 == "king")
                            {
                                if (Convert.ToString(player2.commanderk["state"]) == "alive")
                                {
                                    SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
                                    SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
                                    nserver.SendM(0, msgg1);
                                    nserver.SendM(1, msgg2);
                                    nserver.gamelift.TerminateGameSession();
                                    this.resetServer();
                                    nserver.ResetNetworkServer();
                                    return;
                                }
                                else
                                {
                                    SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
                                    SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
                                    nserver.SendM(0, msgg1);
                                    nserver.SendM(1, msgg2);
                                    nserver.gamelift.TerminateGameSession();
                                    this.resetServer();
                                    nserver.ResetNetworkServer();
                                    return;
                                }
                            }
                            else if (type2 == "commander")
                            {
                                SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
                                SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
                                nserver.SendM(0, msgg1);
                                nserver.SendM(1, msgg2);
                                nserver.gamelift.TerminateGameSession();
                                this.resetServer();
                                nserver.ResetNetworkServer();
                                return;
                            }
                            Dictionary<string, object> temp10 = new Dictionary<string, object> { };
                            Dictionary<string, object> temp20 = new Dictionary<string, object> { };
                            temp10.Add("myTeam", true);
                            temp10.Add("id", id1);
                            temp10.Add("action", "move");
                            temp10.Add("source", source);
                            temp10.Add("dest", dest);
                            temp10.Add("color", "gold");
                            temp20.Add("myTeam", false);
                            temp20.Add("id", id1);
                            temp20.Add("action", "move");
                            temp20.Add("source", source);
                            temp20.Add("dest", dest);
                            temp20.Add("color", "gold");
                            boardPosStates[source] = 0;
                            boardPosStates[dest] = 1;
                            player1.king["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.king["posJ"] = dest % 10;
                            player1.positionIdMatcher.Remove(source);
                            player1.positionIdMatcher.Add(dest, id1);
                            msg1.listdictdata.Add(temp10);
                            msg2.listdictdata.Add(temp20);
                            msg1.listData = boardPosStates.ConvertAll<object>(k => k);
                            msg2.listData = boardPosStates.ConvertAll<object>(k => k);
                        }
                        else if (type1 == "lord")
                        {

                        }
                        else if (type1 == "commander")
                        {

                        }
                        else if (type1 == "knight")
                        {

                        }
                        else if (type1 == "soldier")
                        {

                        }
                    }
                    else if (boardPosStates[dest] == 0)
                    {
                        Debug.Log("Handling Player Move:1: Moving to empty place");
                        Dictionary<string, object> temp11 = new Dictionary<string, object> { };
                        Dictionary<string, object> temp21 = new Dictionary<string, object> { };
                        temp11.Add("myTeam", true);
                        temp11.Add("id", id1);
                        temp11.Add("action", "move");
                        temp11.Add("source", source);
                        temp11.Add("dest", dest);
                        temp21.Add("myTeam", false);
                        temp21.Add("id", id1);
                        temp21.Add("action", "move");
                        temp21.Add("source", source);
                        temp21.Add("dest", dest);
                        if (id1.Substring(0, 2).Equals("ki"))
                        {
                            player1.king["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.king["posJ"] = dest % 10;
                            temp11.Add("color", "gold");
                            temp21.Add("color", "gold");
                        }
                        else if (id1.Substring(0, 2).Equals("l1"))
                        {
                            player1.lord1["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.lord1["posJ"] = dest % 10;
                            temp11.Add("color", "gold");
                            temp21.Add("color", "gold");
                        }
                        else if (id1.Substring(0, 2).Equals("l2"))
                        {
                            player1.lord2["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.lord2["posJ"] = dest % 10;
                            temp11.Add("color", "gold");
                            temp21.Add("color", "gold");
                        }
                        else if (id1.Substring(0, 2).Equals("ck"))
                        {
                            player1.commanderk["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.commanderk["posJ"] = dest % 10;
                            temp11.Add("color", "silver");
                            temp21.Add("color", "silver");
                        }
                        else if (id1.Substring(0, 2).Equals("c1"))
                        {
                            player1.commanderl1["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.commanderl1["posJ"] = dest % 10;
                            temp11.Add("color", "silver");
                            temp21.Add("color", "silver");
                        }
                        else if (id1.Substring(0, 2).Equals("c2"))
                        {
                            player1.commanderl2["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.commanderl2["posJ"] = dest % 10;
                            temp11.Add("color", "silver");
                            temp21.Add("color", "silver");
                        }
                        else if (id1.Substring(0, 2).Equals("kn"))
                        {
                            player1.knights[id1]["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.knights[id1]["posJ"] = dest % 10;
                            temp11.Add("color", "silver");
                            temp21.Add("color", "silver");
                        }
                        else if (id1.Substring(0, 2).Equals("so"))
                        {
                            player1.soldiers[id1]["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player1.soldiers[id1]["posJ"] = dest % 10;
                            temp11.Add("color", "bronze");
                            temp21.Add("color", "bronze");
                        }
                        boardPosStates[source] = 0;
                        boardPosStates[dest] = 1;
                        player1.positionIdMatcher.Remove(source);
                        player1.positionIdMatcher.Add(dest, id1);
                        msg1.listData = boardPosStates.ConvertAll<object>(k => k);
                        msg2.listData = boardPosStates.ConvertAll<object>(k => k);
                        msg1.listdictdata.Add(temp11);
                        msg2.listdictdata.Add(temp21);
                    }
                }
            }
            else if (msg.playerId == "2")
            {
                Debug.Log("Handling Player Move:2");
                string id2 = Convert.ToString(msg.dictData["id"]);
                string type2 = "";
                Dictionary<string, object> pieceData2 = new Dictionary<string, object> { };
                Debug.Log("id2:" + id2);
                if (boardPosStates[source] == 2)
                {
                    if (id2.Substring(0, 2).Equals("ki") && Convert.ToString(player2.king["id"]) == id2)
                    {
                        type2 = "king";
                        pieceData2 = player2.king;
                    }
                    else if (id2.Substring(0, 2).Equals("l1") && Convert.ToString(player2.lord1["id"]) == id2)
                    {
                        type2 = "lord";
                        pieceData2 = player2.lord1;
                    }
                    else if (id2.Substring(0, 2).Equals("l2") && Convert.ToString(player2.lord2["id"]) == id2)
                    {
                        type2 = "lord";
                        pieceData2 = player2.lord2;
                    }
                    else if (id2.Substring(0, 2).Equals("ck") && Convert.ToString(player2.commanderk["id"]) == id2)
                    {
                        type2 = "commander";
                        pieceData2 = player2.commanderk;
                    }
                    else if (id2.Substring(0, 2).Equals("c1") && Convert.ToString(player2.commanderl1["id"]) == id2)
                    {
                        type2 = "commander";
                        pieceData2 = player2.commanderl1;
                    }
                    else if (id2.Substring(0, 2).Equals("c2") && Convert.ToString(player2.commanderl2["id"]) == id2)
                    {
                        type2 = "commander";
                        pieceData2 = player2.commanderl2;
                    }
                    else if (id2.Substring(0, 2).Equals("kn") && player2.knights.ContainsKey(id2))
                    {
                        type2 = "knight";
                        pieceData2 = player2.knights[id2];
                    }
                    else if (id2.Substring(0, 2).Equals("so") && player2.soldiers.ContainsKey(id2))
                    {
                        type2 = "soldier";
                        pieceData2 = player2.soldiers[id2];
                    }

                    if (boardPosStates[dest] == 1)
                    {
                        Debug.Log("Handling Player Move:2:Moving onto opponent");
                        string id1 = player1.positionIdMatcher[dest];
                        string type1 = "";
                        Dictionary<string, object> pieceData1 = new Dictionary<string, object> { };
                        if (id1.Substring(0, 2).Equals("ki") && Convert.ToString(player1.king["id"]) == id1)
                        {
                            type1 = "king";
                            pieceData1 = player1.king;
                        }
                        else if (id1.Substring(0, 2).Equals("l1") && Convert.ToString(player1.lord1["id"]) == id1)
                        {
                            type1 = "lord";
                            pieceData1 = player1.lord1;
                        }
                        else if (id1.Substring(0, 2).Equals("l2") && Convert.ToString(player1.lord2["id"]) == id1)
                        {
                            type1 = "lord";
                            pieceData1 = player1.lord2;
                        }
                        else if (id1.Substring(0, 2).Equals("ck") && Convert.ToString(player1.commanderk["id"]) == id1)
                        {
                            type1 = "commander";
                            pieceData1 = player1.commanderk;
                        }
                        else if (id1.Substring(0, 2).Equals("c1") && Convert.ToString(player1.commanderl1["id"]) == id1)
                        {
                            type1 = "commander";
                            pieceData1 = player1.commanderl1;
                        }
                        else if (id1.Substring(0, 2).Equals("c2") && Convert.ToString(player1.commanderl2["id"]) == id1)
                        {
                            type1 = "commander";
                            pieceData1 = player1.commanderl2;
                        }
                        else if (id1.Substring(0, 2).Equals("kn") && player1.knights.ContainsKey(id1))
                        {
                            type1 = "knight";
                            pieceData1 = player1.knights[id1];
                        }
                        else if (id1.Substring(0, 2).Equals("so") && player1.soldiers.ContainsKey(id1))
                        {
                            type1 = "soldier";
                            pieceData1 = player1.soldiers[id1];
                        }
                        if (type2 == "king")
                        {
                            Debug.Log("Type 2: king");
                            if (type1 == "soldier" || type1 == "knight")
                            {
                                Debug.Log("type2: soldier || knight");
                                Dictionary<string, object> temp21 = new Dictionary<string, object> { };
                                Dictionary<string, object> temp11 = new Dictionary<string, object> { };
                                temp21.Add("myTeam", false);
                                temp21.Add("id", id1);
                                temp11.Add("myTeam", true);
                                temp11.Add("id", id1);
                                if (pieceData1["serves"].ToString() == "king")
                                {
                                    temp11.Add("action", "dead");
                                    temp21.Add("action", "dead");
                                    if (type1 == "soldier")
                                    {
                                        temp11.Add("color", "bronze");
                                        temp21.Add("color", "bronze");
                                        player1.soldiers[id1]["state"] = "dead";
                                    }
                                    else if (type2 == "knight")
                                    {
                                        temp11.Add("color", "silver");
                                        temp21.Add("color", "silver");
                                        player1.knights[id1]["state"] = "dead";
                                    }
                                }
                                else
                                {
                                    temp21.Add("action", "injured");
                                    temp11.Add("action", "injured");
                                    if (pieceData1["serves"].ToString() == "lord1")
                                    {
                                        temp11.Add("fort", "1");
                                        temp21.Add("fort", "1");
                                        player1.fort1.Add(id1);
                                    }
                                    else if (pieceData1["serves"].ToString() == "lord2")
                                    {
                                        temp11.Add("fort", "2");
                                        temp21.Add("fort", "2");
                                        player1.fort2.Add(id1);
                                    }
                                    if (type1 == "soldier")
                                    {
                                        temp11.Add("color", "bronze");
                                        temp21.Add("color", "bronze");
                                        player1.soldiers[id1]["state"] = "dead";
                                    }
                                    else
                                    {
                                        temp11.Add("color", "silver");
                                        temp21.Add("color", "silver");
                                        player1.knights[id1]["state"] = "dead";
                                    }
                                }
                                player1.positionIdMatcher.Remove(dest);
                                msg1.listdictdata.Add(temp11);
                                msg2.listdictdata.Add(temp21);
                            }
                            else if (type1 == "lord")
                            {
                                bool win = false;
                                if (id1.Substring(0, 2).Equals("l1"))
                                {
                                    if (player1.commanderl1["state"] != "alive")
                                    {
                                        win = true;
                                    }
                                }
                                else if (id1.Substring(0, 2).Equals("l2"))
                                {
                                    if (player1.commanderl2["state"] != "alive")
                                    {
                                        win = true;
                                    }
                                }
                                if (win)
                                {
                                    Dictionary<string, object> temp11 = new Dictionary<string, object> { };
                                    Dictionary<string, object> temp21 = new Dictionary<string, object> { };
                                    temp21.Add("myTeam", false);
                                    temp11.Add("myTeam", true);
                                    temp21.Add("id", id2);
                                    temp11.Add("id", id2);
                                    temp21.Add("action", "dead");
                                    temp11.Add("action", "dead");
                                    temp11.Add("color", "gold");
                                    temp21.Add("color", "gold");
                                    msg1.listdictdata.Add(temp11);
                                    msg2.listdictdata.Add(temp21);
                                    if (id1.Substring(0, 2).Equals("l1"))
                                    {
                                        player1.lord1["state"] = "dead";
                                        for (int i = 0; i < player1.fort1.Count; i++)
                                        {
                                            string tempId = player1.fort1[i];
                                            Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                            Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                            if (tempId.Substring(0, 1).Equals("c") || (tempId.Substring(0, 2).Equals("kn") && Convert.ToInt32(player2.knights[tempId]["power"]) == 7))
                                            {
                                                if (tempId.Substring(0, 1).Equals("c"))
                                                {
                                                    player1.commanderl1["state"] = "opp";
                                                }
                                                else if (tempId.Substring(0, 2).Equals("kn"))
                                                {
                                                    player1.knights[tempId]["state"] = "opp";
                                                }
                                                int tempPos = findAndFillPlace("2");
                                                string newId = "kn" + PlayerData.RandomString(7);
                                                player2.addKnight(tempPos / 10, tempPos % 10, 5, "king", newId);
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("type", "knight");
                                                temp2.Add("color", "silver");
                                                temp2.Add("power", 5);
                                                temp2.Add("pos", tempPos);
                                                temp1.Add("myTeam", false);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("color", "silver");
                                                temp1.Add("pos", tempPos);
                                            }
                                            else if (tempId.Substring(0, 2).Equals("kn"))
                                            {
                                                player1.knights[tempId]["state"] = "opp";
                                                int tempPos = findAndFillPlace("2");
                                                string newId = "so" + PlayerData.RandomString(7);
                                                int tempPower = 0;
                                                if (Convert.ToInt32(player1.knights[tempId]["power"]) == 6)
                                                {
                                                    tempPower = 4;
                                                }
                                                else if (Convert.ToInt32(player1.knights[tempId]["power"]) == 5)
                                                {
                                                    tempPower = 3;
                                                }
                                                player2.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("type", "soldier");
                                                temp2.Add("color", "bronze");
                                                temp2.Add("power", tempPower);
                                                temp2.Add("pos", tempPos);
                                                temp1.Add("myTeam", false);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("color", "bronze");
                                                temp1.Add("pos", tempPos);
                                            }
                                            else if (tempId.Substring(0, 2).Equals("so"))
                                            {
                                                player1.soldiers[tempId]["state"] = "opp";
                                                int tempPos = findAndFillPlace("2");
                                                string newId = "so" + PlayerData.RandomString(7);
                                                int tempPower = 1;
                                                if (Convert.ToInt32(player1.soldiers[tempId]["power"]) == 4)
                                                {
                                                    tempPower = 2;
                                                }
                                                player2.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("type", "soldier");
                                                temp2.Add("color", "bronze");
                                                temp2.Add("power", tempPower);
                                                temp2.Add("pos", tempPos);
                                                temp1.Add("myTeam", false);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("color", "bronze");
                                                temp1.Add("pos", tempPos);
                                            }
                                            msg1.listdictdata.Add(temp1);
                                            msg2.listdictdata.Add(temp2);
                                        }
                                        if (Convert.ToString(player2.commanderl1["state"]) == "alive")
                                        {
                                            //player2.commanderl1["serves"] = "king";
                                            player2.commanderl1["state"] = "change";
                                            string tempid1 = Convert.ToString(player2.commanderl1["id"]);
                                            tempid1 = "kn" + tempid1.Substring(2);
                                            player2.positionIdMatcher.Remove(Convert.ToInt32(player2.commanderl1["posI"]) * 10 + Convert.ToInt32(player2.commanderl1["posJ"]));
                                            player2.addKnight(Convert.ToInt32(player2.commanderl1["posI"]), Convert.ToInt32(player2.commanderl1["posJ"]), 5, "king", tempid1);
                                            Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                            Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                            temp2.Add("myTeam", true);
                                            temp2.Add("id", player2.commanderl1["id"]);
                                            temp2.Add("action", "changeserves");
                                            temp2.Add("newId", tempid1);
                                            temp2.Add("color", "silver");
                                            temp1.Add("myTeam", false);
                                            temp1.Add("id", player2.commanderl1["id"]);
                                            temp1.Add("action", "changeserves");
                                            temp1.Add("newId", tempid1);
                                            temp1.Add("color", "silver");
                                            msg1.listdictdata.Add(temp1);
                                            msg2.listdictdata.Add(temp2);
                                        }
                                        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in player2.knights)
                                        {
                                            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord1")
                                            {
                                                tempItem.Value["power"] = 5;
                                                tempItem.Value["serves"] = "king";
                                                player2.knights[tempItem.Key] = tempItem.Value;
                                                Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", tempItem.Value["id"]);
                                                temp2.Add("action", "changeserves");
                                                temp2.Add("newId", tempItem.Value["id"]);
                                                temp2.Add("color", "silver");
                                                msg2.listdictdata.Add(temp2);
                                            }
                                        }
                                        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in player2.soldiers)
                                        {
                                            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord1")
                                            {
                                                tempItem.Value["power"] = 1;
                                                tempItem.Value["serves"] = "king";
                                                player2.knights[tempItem.Key] = tempItem.Value;
                                                Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", tempItem.Value["id"]);
                                                temp2.Add("action", "changeserves");
                                                temp2.Add("newId", tempItem.Value["id"]);
                                                temp2.Add("color", "bronze");
                                                msg2.listdictdata.Add(temp2);
                                            }
                                        }
                                        Dictionary<string, object> temp12 = new Dictionary<string, object> { };
                                        Dictionary<string, object> temp22 = new Dictionary<string, object> { };
                                        temp22.Add("myTeam", false);
                                        temp22.Add("id", "");
                                        temp22.Add("action", "clearfort");
                                        temp22.Add("fort", "1");
                                        temp12.Add("myTeam", true);
                                        temp12.Add("id", "");
                                        temp12.Add("action", "clearfort");
                                        temp12.Add("fort", "1");
                                        player1.fort1 = null;
                                        player1.positionIdMatcher.Remove(dest);
                                        msg1.listdictdata.Add(temp12);
                                        msg2.listdictdata.Add(temp22);
                                    }
                                    else if (id1.Substring(0, 2).Equals("l2"))
                                    {
                                        player1.lord2["state"] = "dead";
                                        for (int i = 0; i < player1.fort2.Count; i++)
                                        {
                                            string tempId = player1.fort2[i];
                                            Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                            Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                            if (tempId.Substring(0, 1).Equals("c") || (tempId.Substring(0, 2).Equals("kn") && Convert.ToInt32(player2.knights[tempId]["power"]) == 7))
                                            {
                                                if (tempId.Substring(0, 1).Equals("c"))
                                                {
                                                    player1.commanderl2["state"] = "opp";
                                                }
                                                else if (tempId.Substring(0, 2).Equals("kn"))
                                                {
                                                    player1.knights[tempId]["state"] = "opp";
                                                }
                                                int tempPos = findAndFillPlace("2");
                                                string newId = "kn" + PlayerData.RandomString(7);
                                                player2.addKnight(tempPos / 10, tempPos % 10, 5, "king", newId);
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("type", "knight");
                                                temp2.Add("color", "silver");
                                                temp2.Add("power", 5);
                                                temp2.Add("pos", tempPos);
                                                temp1.Add("myTeam", false);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("color", "silver");
                                                temp1.Add("pos", tempPos);
                                            }
                                            else if (tempId.Substring(0, 2).Equals("kn"))
                                            {
                                                player1.knights[tempId]["state"] = "opp";
                                                int tempPos = findAndFillPlace("1");
                                                string newId = "so" + PlayerData.RandomString(7);
                                                int tempPower = 0;
                                                if (Convert.ToInt32(player1.knights[tempId]["power"]) == 6)
                                                {
                                                    tempPower = 4;
                                                }
                                                else if (Convert.ToInt32(player1.knights[tempId]["power"]) == 5)
                                                {
                                                    tempPower = 3;
                                                }
                                                player2.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("type", "soldier");
                                                temp2.Add("color", "bronze");
                                                temp2.Add("power", tempPower);
                                                temp2.Add("pos", tempPos);
                                                temp1.Add("myTeam", false);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("color", "bronze");
                                                temp1.Add("pos", tempPos);
                                            }
                                            else if (tempId.Substring(0, 2).Equals("so"))
                                            {
                                                player1.soldiers[tempId]["state"] = "opp";
                                                int tempPos = findAndFillPlace("1");
                                                string newId = "so" + PlayerData.RandomString(7);
                                                int tempPower = 1;
                                                if (Convert.ToInt32(player1.soldiers[tempId]["power"]) == 4)
                                                {
                                                    tempPower = 2;
                                                }
                                                player2.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", newId);
                                                temp2.Add("action", "add");
                                                temp2.Add("type", "soldier");
                                                temp2.Add("color", "bronze");
                                                temp2.Add("power", tempPower);
                                                temp2.Add("pos", tempPos);
                                                temp1.Add("myTeam", false);
                                                temp1.Add("id", newId);
                                                temp1.Add("action", "add");
                                                temp1.Add("color", "bronze");
                                                temp1.Add("pos", tempPos);
                                            }
                                            msg1.listdictdata.Add(temp1);
                                            msg2.listdictdata.Add(temp2);
                                        }
                                        if (Convert.ToString(player2.commanderl2["state"]) == "alive")
                                        {
                                            //player2.commanderl1["serves"] = "king";
                                            player2.commanderl2["state"] = "change";
                                            string tempid1 = Convert.ToString(player2.commanderl2["id"]);
                                            tempid1 = "kn" + tempid1.Substring(2);
                                            player2.positionIdMatcher.Remove(Convert.ToInt32(player2.commanderl2["posI"]) * 10 + Convert.ToInt32(player2.commanderl2["posJ"]));
                                            player2.addKnight(Convert.ToInt32(player2.commanderl2["posI"]), Convert.ToInt32(player2.commanderl2["posJ"]), 5, "king", tempid1);
                                            Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                                            Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                            temp2.Add("myTeam", true);
                                            temp2.Add("id", player2.commanderl2["id"]);
                                            temp2.Add("action", "changeserves");
                                            temp2.Add("newId", tempid1);
                                            temp2.Add("color", "silver");
                                            temp1.Add("myTeam", false);
                                            temp1.Add("id", player2.commanderl2["id"]);
                                            temp1.Add("action", "changeserves");
                                            temp1.Add("newId", tempid1);
                                            temp1.Add("color", "silver");
                                            msg1.listdictdata.Add(temp1);
                                            msg2.listdictdata.Add(temp2);
                                        }
                                        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in player2.knights)
                                        {
                                            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord2")
                                            {
                                                tempItem.Value["power"] = 5;
                                                tempItem.Value["serves"] = "king";
                                                player2.knights[tempItem.Key] = tempItem.Value;
                                                Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", tempItem.Value["id"]);
                                                temp2.Add("action", "changeserves");
                                                temp2.Add("newId", tempItem.Value["id"]);
                                                temp2.Add("color", "silver");
                                                msg2.listdictdata.Add(temp2);
                                            }
                                        }
                                        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in player2.soldiers)
                                        {
                                            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord2")
                                            {
                                                tempItem.Value["power"] = 1;
                                                tempItem.Value["serves"] = "king";
                                                player2.knights[tempItem.Key] = tempItem.Value;
                                                Dictionary<string, object> temp2 = new Dictionary<string, object> { };
                                                temp2.Add("myTeam", true);
                                                temp2.Add("id", tempItem.Value["id"]);
                                                temp2.Add("action", "changeserves");
                                                temp2.Add("newId", tempItem.Value["id"]);
                                                temp2.Add("color", "bronze");
                                                msg2.listdictdata.Add(temp2);
                                            }
                                        }
                                        Dictionary<string, object> temp12 = new Dictionary<string, object> { };
                                        Dictionary<string, object> temp22 = new Dictionary<string, object> { };
                                        temp22.Add("myTeam", false);
                                        temp22.Add("id", "");
                                        temp22.Add("action", "clearfort");
                                        temp22.Add("fort", "2");
                                        temp12.Add("myTeam", true);
                                        temp12.Add("id", "");
                                        temp12.Add("action", "clearfort");
                                        temp12.Add("fort", "2");
                                        player1.fort2 = null;
                                        player1.positionIdMatcher.Remove(dest);
                                        msg1.listdictdata.Add(temp12);
                                        msg2.listdictdata.Add(temp22);
                                    }
                                }
                                else
                                {
                                    SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
                                    SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
                                    nserver.SendM(0, msgg1);
                                    nserver.SendM(1, msgg2);
                                    nserver.gamelift.TerminateGameSession();
                                    this.resetServer();
                                    nserver.ResetNetworkServer();
                                    return;
                                }
                            }
                            else if (type1 == "king")
                            {
                                if (Convert.ToString(player1.commanderk["state"]) == "alive")
                                {
                                    SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
                                    SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
                                    nserver.SendM(0, msgg1);
                                    nserver.SendM(1, msgg2);
                                    nserver.gamelift.TerminateGameSession();
                                    this.resetServer();
                                    nserver.ResetNetworkServer();
                                    return;
                                }
                                else
                                {
                                    SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
                                    SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
                                    nserver.SendM(0, msgg1);
                                    nserver.SendM(1, msgg2);
                                    nserver.gamelift.TerminateGameSession();
                                    this.resetServer();
                                    nserver.ResetNetworkServer();
                                    return;
                                }
                            }
                            else if (type1 == "commander")
                            {
                                SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
                                SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
                                nserver.SendM(0, msgg1);
                                nserver.SendM(1, msgg2);
                                nserver.gamelift.TerminateGameSession();
                                this.resetServer();
                                nserver.ResetNetworkServer();
                                return;
                            }
                            Dictionary<string, object> temp10 = new Dictionary<string, object> { };
                            Dictionary<string, object> temp20 = new Dictionary<string, object> { };
                            temp10.Add("myTeam", false);
                            temp10.Add("id", id2);
                            temp10.Add("action", "move");
                            temp10.Add("source", source);
                            temp10.Add("dest", dest);
                            temp10.Add("color", "gold");
                            temp20.Add("myTeam", true);
                            temp20.Add("id", id2);
                            temp20.Add("action", "move");
                            temp20.Add("source", source);
                            temp20.Add("dest", dest);
                            temp20.Add("color", "gold");
                            boardPosStates[source] = 0;
                            boardPosStates[dest] = 2;
                            player2.king["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.king["posJ"] = dest % 10;
                            player2.positionIdMatcher.Remove(source);
                            player2.positionIdMatcher.Add(dest, id2);
                            msg1.listdictdata.Add(temp10);
                            msg2.listdictdata.Add(temp20);
                            msg1.listData = boardPosStates.ConvertAll<object>(k => k);
                            msg2.listData = boardPosStates.ConvertAll<object>(k => k);
                        }
                        else if (type2 == "lord")
                        {

                        }
                        else if (type2 == "commander")
                        {

                        }
                        else if (type2 == "knight")
                        {

                        }
                        else if (type2 == "soldier")
                        {

                        }
                    }
                    else if (boardPosStates[dest] == 0)
                    {
                        Debug.Log("Handling Player Move:2:Move to empty space");
                        Dictionary<string, object> temp11 = new Dictionary<string, object> { };
                        Dictionary<string, object> temp21 = new Dictionary<string, object> { };
                        temp11.Add("myTeam", false);
                        temp11.Add("id", id2);
                        temp11.Add("action", "move");
                        temp11.Add("source", source);
                        temp11.Add("dest", dest);
                        temp21.Add("myTeam", true);
                        temp21.Add("id", id2);
                        temp21.Add("action", "move");
                        temp21.Add("source", source);
                        temp21.Add("dest", dest);
                        if (id2.Substring(0, 2).Equals("ki"))
                        {
                            player2.king["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.king["posJ"] = dest % 10;
                            temp11.Add("color", "gold");
                            temp21.Add("color", "gold");
                        }
                        else if (id2.Substring(0, 2).Equals("l1"))
                        {
                            player2.lord1["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.lord1["posJ"] = dest % 10;
                            temp11.Add("color", "gold");
                            temp21.Add("color", "gold");
                        }
                        else if (id2.Substring(0, 2).Equals("l2"))
                        {
                            player2.lord2["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.lord2["posJ"] = dest % 10;
                            temp11.Add("color", "gold");
                            temp21.Add("color", "gold");
                        }
                        else if (id2.Substring(0, 2).Equals("ck"))
                        {
                            player2.commanderk["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.commanderk["posJ"] = dest % 10;
                            temp11.Add("color", "silver");
                            temp21.Add("color", "silver");
                        }
                        else if (id2.Substring(0, 2).Equals("c1"))
                        {
                            player2.commanderl1["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.commanderl1["posJ"] = dest % 10;
                            temp11.Add("color", "silver");
                            temp21.Add("color", "silver");
                        }
                        else if (id2.Substring(0, 2).Equals("c2"))
                        {
                            player2.commanderl2["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.commanderl2["posJ"] = dest % 10;
                            temp11.Add("color", "silver");
                            temp21.Add("color", "silver");
                        }
                        else if (id2.Substring(0, 2).Equals("kn"))
                        {
                            player2.knights[id2]["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.knights[id2]["posJ"] = dest % 10;
                            temp11.Add("color", "silver");
                            temp21.Add("color", "silver");
                        }
                        else if (id2.Substring(0, 2).Equals("so"))
                        {
                            player2.soldiers[id2]["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
                            player2.soldiers[id2]["posJ"] = dest % 10;
                            temp11.Add("color", "bronze");
                            temp21.Add("color", "bronze");
                        }
                        boardPosStates[source] = 0;
                        boardPosStates[dest] = 2;
                        player2.positionIdMatcher.Remove(source);
                        player2.positionIdMatcher.Add(dest, id2);
                        msg1.listData = boardPosStates.ConvertAll<object>(k => k);
                        msg2.listData = boardPosStates.ConvertAll<object>(k => k);
                        msg1.listdictdata.Add(temp11);
                        msg2.listdictdata.Add(temp21);
                    }
                }
            }
            nserver.SendM(0, msg1);
            nserver.SendM(1, msg2);
            StartCoroutine(SendTurnChanged());
        }
    }

    public void HandlePlayerStrategyRecieved(SimpleMessage msg)
    {
        Debug.Log("Player Strategy Recieved");
        if (timer > 0.0f && gameStarted)
        {
            if (msg.playerId == "1")
            {
                gameReadyStateP1 = true;
                Dictionary<string, int> tempDict = new Dictionary<string, int> { };
                foreach (KeyValuePair<string, object> keyValuePair in msg.dictData)
                {
                    tempDict.Add(keyValuePair.Key, keyValuePair.Value == null ? 0 : Convert.ToInt32(keyValuePair.Value));
                }
                LoadStrategy(tempDict, ids1, player1);
            }
            else if (msg.playerId == "2")
            {
                gameReadyStateP2 = true;
                Dictionary<string, int> tempDict = new Dictionary<string, int> { };
                foreach (KeyValuePair<string, object> keyValuePair in msg.dictData)
                {
                    tempDict.Add(keyValuePair.Key, keyValuePair.Value == null ? 0 : Convert.ToInt32(keyValuePair.Value));
                }
                LoadStrategy(tempDict, ids2, player2);
            }
            if (gameReadyStateP1 && gameReadyStateP2)
            {
                timerMode = false;
                timer = 0f;
                pTurn = "2";
                gameReady = true;
                gameStarted = false;
                SimpleMessage msg1 = new SimpleMessage(MessageType.GameReady);
                SimpleMessage msg2 = new SimpleMessage(MessageType.GameReady);
                msg1.listdictdata = player2.convertToOppData();
                msg2.listdictdata = player1.convertToOppData();
                msg1.listData = boardPosStates.ConvertAll<object>(k => k);
                msg2.listData = boardPosStates.ConvertAll<object>(k => k); ;
                nserver.SendM(0, msg1);
                nserver.SendM(1, msg2);
                StartCoroutine(SendTurnChanged());
            }
        }
    }

    public int findAndFillPlace(string playerId)
    {
        if (playerId == "1")
        {
            for (int i = 99; i >= 70; i--)
            {
                if (boardPosStates[i] == 0)
                {
                    boardPosStates[i] = 1;
                    return i;
                }
            }
        }
        else if (playerId == "2")
        {
            for (int i = 0; i < 30; i++)
            {
                if (boardPosStates[i] == 0)
                {
                    boardPosStates[i] = 2;
                    return i;
                }
            }
        }
        return -1;
    }

    public void DisconnectAll()
    {
        this.nserver.DisconnectAll();
    }

    void LoadStrategy(Dictionary<string, int> loadingStrategy, List<string> idx, PlayerData playerData)
    {
        List<string> keys = new List<string>(loadingStrategy.Keys);
        List<int> values = new List<int>(loadingStrategy.Values);
        for (int i = 0; i < keys.Count; i++)
        {
            int tempBoardPos = values[i];
            tempBoardPos = playerData.offset + playerData.posMultiplier * tempBoardPos;
            int ip = (tempBoardPos) / 10;
            int jp = tempBoardPos % 10;
            float tempx = (jp - 5) * (2.5f) + (1.25f);
            float tempy = (5 - ip) * (2.5f) - (1.25f);
            boardPosStates[tempBoardPos] = Convert.ToInt32(playerData.playerId);
            if (keys[i].Split('_')[0] == "king")
            {
                playerData.InitializeKing(ip, jp, idx[ArmyAlias.IndexOf(keys[i])]);
            }
            else if (keys[i].Split('_')[0] == "lord1")
            {

                playerData.InitializeLord1(ip, jp, idx[ArmyAlias.IndexOf(keys[i])]);
            }
            else if (keys[i].Split('_')[0] == "lord2")
            {
                playerData.InitializeLord2(ip, jp, idx[ArmyAlias.IndexOf(keys[i])]);
            }
            else if (keys[i].Split('_')[0] == "commander")
            {
                if (keys[i].Split('_')[1] == "king")
                {
                    playerData.InitializeCommanderK(ip, jp, idx[ArmyAlias.IndexOf(keys[i])]);
                }
                else if (keys[i].Split('_')[1] == "lord1")
                {
                    playerData.InitializeCommanderL1(ip, jp, idx[ArmyAlias.IndexOf(keys[i])]);
                }
                else if (keys[i].Split('_')[1] == "lord2")
                {
                    playerData.InitializeCommanderL2(ip, jp, idx[ArmyAlias.IndexOf(keys[i])]);
                }
            }
            else if (keys[i].Split('_')[0] == "knight")
            {
                playerData.addKnight(ip, jp, Convert.ToInt32(keys[i].Split('_')[2]), keys[i].Split('_')[1], idx[ArmyAlias.IndexOf(keys[i])]);
            }
            else if (keys[i].Split('_')[0] == "soldier")
            {
                playerData.addSoldier(ip, jp, Convert.ToInt32(keys[i].Split('_')[2]), keys[i].Split('_')[1], idx[ArmyAlias.IndexOf(keys[i])]);
            }
        }

    }

}

// *** SERVER NETWORK LOGIC *** //

public class NetworkServer
{
    private TcpListener listener;
    private List<TcpClient> clients = new List<TcpClient>();
    private List<TcpClient> readyClients = new List<TcpClient>();
    private List<TcpClient> clientsToRemove = new List<TcpClient>();

    public GameLift gamelift = null;
    private Server server = null;

    private bool gameExiting = false;

    private int timer = 0;

    public NetworkServer(GameLift gamelift, Server serverx)
    {
        this.gamelift = gamelift;
        this.server = serverx;
        //Start the TCP server
        int port = this.gamelift.listeningPort;
        Debug.Log("Starting server on port " + port);
        listener = new TcpListener(IPAddress.Any, this.gamelift.listeningPort);
        Debug.Log("Listening at: " + listener.LocalEndpoint.ToString());
        listener.Start();
    }

    // Checks if socket is still connected
    private bool IsSocketConnected(TcpClient client)
    {
        var bClosed = false;

        // Detect if client disconnected
        if (client.Client.Poll(0, SelectMode.SelectRead))
        {
            byte[] buff = new byte[1];
            if (client.Client.Receive(buff, SocketFlags.Peek) == 0)
            {
                // Client disconnected
                bClosed = true;
            }
        }

        return !bClosed;
    }

    public void Update()
    {
        // Are there any new connections pending?
        if (listener.Pending())
        {
            Debug.Log("Client pending..");
            TcpClient client = listener.AcceptTcpClient();
            client.NoDelay = true; // Use No Delay to send small messages immediately. UDP should be used for even faster messaging
            Debug.Log("Client accepted.");

            // We have a maximum of 10 clients per game
            if (this.clients.Count < 2)
            {
                this.clients.Add(client);
                return;
            }
            else
            {
                // game already full, reject the connection
                try
                {
                    SimpleMessage message = new SimpleMessage(MessageType.Reject, "game already full");
                    NetworkProtocol.Send(client, message);
                }
                catch (SocketException e)
                {

                }
            }

        }

        // Iterate through clients and check if they have new messages or are disconnected
        int playerIdx = 0;
        foreach (var client in this.clients)
        {
            try
            {
                if (client == null) continue;
                if (this.IsSocketConnected(client) == false)
                {
                    Debug.Log("Client not connected anymore");
                    this.clientsToRemove.Add(client);
                }
                var messages = NetworkProtocol.Receive(client);
                foreach (SimpleMessage message in messages)
                {
                    Debug.Log("Received message: " + message.message + " type: " + message.messageType);
                    bool disconnect = HandleMessage(playerIdx, client, message);
                    if (disconnect)
                        this.clientsToRemove.Add(client);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error receiving from a client: " + e.Message);
                this.clientsToRemove.Add(client);
            }
            playerIdx++;
        }

        //Remove dead clients
        foreach (var clientToRemove in this.clientsToRemove)
        {
            try
            {
                this.RemoveClient(clientToRemove);
            }
            catch (Exception e)
            {
                Debug.Log("Couldn't remove client: " + e.Message);
            }
        }
        this.clientsToRemove.Clear();

        //End game if no clients
        if (this.gamelift.GameStarted())
        {
            if (this.clients.Count <= 0)
            {
                Debug.Log("Clients gone, stop session");
                this.gamelift.TerminateGameSession();
                server.resetServer();
                this.ResetNetworkServer();
            }
            else if (this.clients.Count == 1)
            {
                Debug.Log("One player left, other wins the game");
                this.gamelift.TerminateGameSession();
                server.resetServer();
                this.ResetNetworkServer();
            }
        }




        // Simple test for the the StatsD client: Send current amount of player online
        if (this.gamelift.GameStarted())
        {
            this.gamelift.GetStatsdClient().SendGauge("game.ClientSocketsConnected", this.clients.Count);
        }
    }



    public void DisconnectAll()
    {
        // warn clients
        SimpleMessage message = new SimpleMessage(MessageType.Disconnect);
        TransmitMessage(message);
        // disconnect connections
        foreach (var client in this.clients)
        {
            this.clientsToRemove.Add(client);
        }

        //Reset the client lists
        this.clients = new List<TcpClient>();
        this.readyClients = new List<TcpClient>();
    }

    //Transmit message to multiple clients
    public void TransmitMessage(SimpleMessage msg, TcpClient excludeClient = null)
    {
        // send the same message to all players
        foreach (var client in this.clients)
        {
            //Skip if this is the excluded client
            if (excludeClient != null && excludeClient == client)
            {
                continue;
            }

            try
            {
                NetworkProtocol.Send(client, msg);
            }
            catch (Exception e)
            {
                this.clientsToRemove.Add(client);
            }
        }
    }

    //Send message to single client
    private void SendMessage(TcpClient client, SimpleMessage msg)
    {
        try
        {
            NetworkProtocol.Send(client, msg);
        }
        catch (Exception e)
        {
            this.clientsToRemove.Add(client);
        }
    }

    public void SendM(int x, SimpleMessage msg)
    {
        SendMessage(clients[x], msg);
    }

    private bool HandleMessage(int playerIdx, TcpClient client, SimpleMessage msg)
    {
        if (msg.messageType == MessageType.Connect)
        {
            HandleConnect(playerIdx, msg.message, client);
        }
        else if (msg.messageType == MessageType.Disconnect)
        {
            this.clientsToRemove.Add(client);
            return true;
        }
        else if (msg.messageType == MessageType.Ready)
            HandleReady(client);
        else
        {
            if (playerIdx == 0 && msg.playerId == "1")
            {
                server.messagesToProcess.Add(msg);
            }
            else if (playerIdx == 1 && msg.playerId == "2")
            {
                server.messagesToProcess.Add(msg);
            }

        }
        return false;
    }

    private void HandleConnect(int playerIdx, string json, TcpClient client)
    {
        // respond with the player id and the current state.
        //Connect player
        SimpleMessage msg = new SimpleMessage(MessageType.GameStarted, "");
        this.SendMessage(client, msg);
        var outcome = GameLiftServerAPI.AcceptPlayerSession(json);
        if (outcome.Success)
        {
            SimpleMessage msg1 = new SimpleMessage(MessageType.GameStarted, "");
            this.SendMessage(client, msg1);
            this.SendMessage(client, msg1);
            Debug.Log(":) PLAYER SESSION VALIDATED");
            SimpleMessage message = new SimpleMessage(MessageType.PlayerAccepted, "Player Accepted by server");
            message.time = Convert.ToInt32(60 - this.gamelift.waitingForPlayerTime);
            server.InitializePlayer((playerIdx + 1).ToString());
            message.playerId = (playerIdx + 1).ToString();
            if (playerIdx == 0)
            {
                server.ids1 = new List<string> { };
                for (int i = 0; i < server.ArmyAlias.Count; i++)
                {
                    string tempId = server.Armyidkeys[i] + PlayerData.RandomString(7);
                    message.dictData.Add(server.ArmyAlias[i], tempId);
                    server.ids1.Add(tempId);
                }
            }
            else if (playerIdx == 1)
            {
                server.ids2 = new List<string> { };
                for (int i = 0; i < server.ArmyAlias.Count; i++)
                {
                    string tempId = server.Armyidkeys[i] + PlayerData.RandomString(7);
                    message.dictData.Add(server.ArmyAlias[i], tempId);
                    server.ids2.Add(tempId);
                }
            }
            this.SendMessage(client, message);
        }
        else
        {
            SimpleMessage msg1 = new SimpleMessage(MessageType.GameStarted, "");
            this.SendMessage(client, msg1);
            Debug.Log(":( PLAYER SESSION REJECTED. AcceptPlayerSession() returned " + outcome.Error.ToString());
            this.clientsToRemove.Add(client);
        }
    }

    private void HandleReady(TcpClient client)
    {
        // start the game once all connected clients have requested to start (RETURN key)
        this.readyClients.Add(client);

        if (readyClients.Count == 2)
        {
            Debug.Log("Enough clients, let's start the game!");
            server.gameStarted = true;
            server.gameReadyStateP1 = false;
            server.gameReadyStateP2 = false;
            server.gameReady = false;
            server.timerMode = true;
            server.timer = 60.0f;
            server.CallCheckGameReady();
            SimpleMessage msg = new SimpleMessage(MessageType.GameStarted, "Game Started");
            TransmitMessage(msg);
            this.gamelift.StartGame();

        }
    }

    private void RemoveClient(TcpClient client)
    {
        //Let the other clients know the player was removed
        int clientId = this.clients.IndexOf(client);
        if (readyClients.Contains(client))
        {
            SimpleMessage message = new SimpleMessage(MessageType.PlayerLeft);
            message.clientId = clientId;
            TransmitMessage(message, client);
        }

        // Disconnect and remove
        this.DisconnectPlayer(client);
        this.clients.Remove(client);
        this.readyClients.Remove(client);
    }

    private void DisconnectPlayer(TcpClient client)
    {
        try
        {
            // remove the client and close the connection
            if (client != null)
            {
                NetworkStream stream = client.GetStream();
                stream.Close();
                client.Close();
            }
        }
        catch (Exception e)
        {
            Debug.Log("Failed to disconnect player: " + e.Message);
        }
    }

    public void ResetNetworkServer()
    {
        for (int i = 0; i < clients.Count; i++)
        {
            try
            {
                if (clients[i] != null)
                {
                    NetworkStream stream = clients[i].GetStream();
                    stream.Close();
                    clients[i].Close();
                }
            }
            catch (Exception e)
            {
                Debug.Log("Failed to disconnect player: " + e.Message);
            }
            clients = new List<TcpClient>();
            readyClients = new List<TcpClient>();
            clientsToRemove = new List<TcpClient>();
        }
    }
}
