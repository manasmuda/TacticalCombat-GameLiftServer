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
                                MovePiece(player1.king, player1, id1, source, dest, "gold", 1, msg1, msg2);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1,dest);
                            }
                            else if (type2 == "lord")
                            {
                                Debug.Log("Type 2: Lord");
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
                                    MovePiece(player1.king, player1, id1, source, dest, "gold", 1, msg1, msg2);
                                    LoD(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                }
                                else
                                {
                                    MovePiece(player1.king, player1, id1, source, dest, "gold", 2, msg1, msg2, false);
                                    KingD(true);
                                    return;
                                }
                            }
                            else if (type2 == "king")
                            {
                                Debug.Log("Type 2: King");
                                if (Convert.ToString(player2.commanderk["state"]) == "alive")
                                {
                                    MovePiece(player1.king, player1, id1, source, dest, "gold", 2, msg1, msg2, false);
                                    KingD(true);
                                    return;
                                }
                                else
                                {
                                    MovePiece(player1.king, player1, id1, source, dest, "gold", 1, msg1, msg2);
                                    KingD(false);
                                    return;
                                }
                            }
                            else if (type2 == "commander")
                            {
                                Debug.Log("Type 2: Commander");
                                int f1=source/10;
                                int f2 = dest/10;
                                if ((f2 - f1) == -1) //Head-On Attack
                                {
                                    MovePiece(player1.king, player1, id1, source, dest, "gold", 2, msg1, msg2, false);
                                    KingD(true);
                                    return;
                                }
                                else
                                {
                                    MovePiece(player1.king, player1, id1, source, dest, "gold", 1, msg1, msg2);
                                    ComD(pieceData2, player2, false,id2, msg1, msg2, dest);
                                }
                            }
                            
                        }
                        else if (type1 == "lord")
                        {
                            Debug.Log("Type 1: Lord");
                            if (type2 == "soldier" || type2 == "knight")
                            {
                                Debug.Log("Type 2: Soilder || Knight");
                                MovePiece(pieceData1, player1, id1, source, dest, "gold", 1, msg1, msg2);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                            else if (type2 == "lord")
                            {
                                Debug.Log("Type 2: Lord");
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
                                    MovePiece(pieceData1, player1, id1, source, dest, "gold", 1, msg1, msg2);
                                    LoD(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                }
                                else
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "gold", 2, msg1, msg2, false);
                                    LoD(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                }
                            }
                            else if (type2 == "king")
                            {
                                Debug.Log("Type 2: King");
                                if (Convert.ToString(player2.commanderk["state"]) == "alive")
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "gold", 2, msg1, msg2, false);
                                    LoD(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                }
                                else
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "gold", 1, msg1, msg2);
                                    KingD(false);
                                    return;
                                }
                            }
                            else if (type2 == "commander")
                            {
                                Debug.Log("Type 2: Commander");
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                if ((f2 - f1) == -1) //Head-On Attack
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "gold", 2, msg1, msg2, true);
                                    LoD(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                }
                                else
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "gold", 1, msg1, msg2);
                                    ComD(pieceData2, player2, false, id2, msg1, msg2, dest);
                                }
                            }
                        }
                        else if (type1 == "commander")
                        {
                            Debug.Log("Type 1: Commander");
                            MovePiece(pieceData1, player1, id1, source, dest, "silver", 1, msg1, msg2);
                            if (type2 == "soldier" || type2 == "knight")
                            {
                                Debug.Log("Type 2: Soilder || Knight");
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                            else if (type2 == "lord")
                            {
                                Debug.Log("Type 2: Lord");
                                LoD(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                            else if (type2 == "king")
                            {
                                Debug.Log("Type 2: King");
                                KingD(false);
                                return;
                            }
                            else if (type2 == "commander")
                            {
                                Debug.Log("Type 2: Commander");
                                ComD(pieceData2, player2, false, id2, msg1, msg2, dest);
                            }
                        }
                        else if (type1 == "knight")
                        {
                            Debug.Log("Type 1: Knight");
                            if (type2 == "soldier")
                            {
                                Debug.Log("Type 2: Soilder");
                                MovePiece(pieceData1, player1, id1, source, dest, "silver", 1, msg1, msg2);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                
                            }
                            else if (type2 == "knight")
                            {
                                Debug.Log("Type 2: Knight");
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                int curPower1 = Convert.ToInt32(pieceData1["power"]);
                                int curPower2 = Convert.ToInt32(pieceData2["power"]);
                                if ((f2 - f1) != -1)  //Sneak Attack
                                {
                                    curPower1++;
                                }
                                if (curPower1 >= curPower2)
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "silver", 1, msg1, msg2);
                                    SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                    if (Convert.ToInt32(pieceData1["power"])!=7)
                                    {
                                        SoOrKnPI(pieceData1, id1, msg1);
                                    }
                                    else
                                    {
                                        if (Convert.ToInt32(pieceData2["power"]) == 7)
                                        {
                                            Dictionary<string, object> commander = player1.commanderk;
                                            if (Convert.ToString(pieceData1["serves"]) == "king")
                                            {
                                                commander = player1.commanderk;
                                            }
                                            else if (Convert.ToString(pieceData1["serves"]) == "lord1")
                                            {
                                                commander = player1.commanderl1;
                                            }
                                            else if (Convert.ToString(pieceData1["serves"]) == "lord2")
                                            {
                                                commander = player1.commanderl2;
                                            }
                                            if (commander == null)
                                            {
                                                SoOrKnUp(pieceData1, player1, id1, true, msg1, msg2, type1, dest);
                                            }
                                            else if (Convert.ToString(commander["state"]) != "alive")
                                            {
                                                SoOrKnUp(pieceData1, player1, id1, true, msg1, msg2, type1, dest);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "silver", 2, msg1, msg2, false);
                                    SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                }
                            }
                            else if (type2 == "lord")
                            {
                                Debug.Log("Type 2: Lord");
                                MovePiece(pieceData1, player1, id1, source, dest, "silver", 2, msg1, msg2, false);
                                SoOrKnDI(player1, player2, id1,true,msg1,msg2,type1,type2,dest);
                            }
                            else if (type2 == "king")
                            {
                                Debug.Log("Type 2: King");
                                MovePiece(pieceData1, player1, id1, source, dest, "silver", 2, msg1, msg2, false);
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                            else if (type2 == "commander")
                            {
                                Debug.Log("Type 2: Commander");
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                if ((f2 - f1) != -1) //Sneak Attack
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "silver", 1, msg1, msg2);
                                    ComD(pieceData2, player2, false, id2, msg1, msg2, dest);
                                    if (Convert.ToInt32(pieceData1["power"]) != 7)
                                    {
                                        SoOrKnPI(pieceData1, id1, msg1);
                                    }
                                    else
                                    {
                                        Dictionary<string, object> commander = player1.commanderk;
                                        if (Convert.ToString(pieceData1["serves"]) == "king")
                                        {
                                            commander = player1.commanderk;
                                        }
                                        else if (Convert.ToString(pieceData1["serves"]) == "lord1")
                                        {
                                            commander = player1.commanderl1;
                                        }
                                        else if (Convert.ToString(pieceData1["serves"]) == "lord2")
                                        {
                                            commander = player1.commanderl2;
                                        }
                                        if (commander == null)
                                        {
                                            SoOrKnUp(pieceData1, player1, id1, true, msg1, msg2, type1, dest);
                                        }
                                        else if (Convert.ToString(commander["state"]) != "alive")
                                        {
                                            SoOrKnUp(pieceData1, player1, id1, true, msg1, msg2, type1, dest);
                                        }
                                    }
                                }
                                else
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "silver", 2, msg1, msg2, false);
                                    SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                }
                            }
                        }
                        else if (type1 == "soldier")
                        {
                            Debug.Log("Type 1: Soldier");
                            if (type2 == "soldier")
                            {
                                Debug.Log("Type 2: Soilder");
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                int curPower1 = Convert.ToInt32(pieceData1["power"]);
                                int curPower2 = Convert.ToInt32(pieceData2["power"]);
                                if ((f2 - f1) != -1)  //Sneak Attack
                                {
                                    curPower1++;
                                }
                                if (curPower1 >= curPower2)
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "bronze", 1, msg1, msg2);
                                    SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                    if (Convert.ToInt32(pieceData1["power"]) != 4)
                                    {
                                        SoOrKnPI(pieceData1, id1, msg1);
                                    }
                                    else
                                    {
                                        if (Convert.ToInt32(pieceData2["power"]) == 4)
                                        {
                                            SoOrKnUp(pieceData1, player1, id1, true, msg1, msg2, type1, dest);
                                        }
                                    }
                                }
                                else
                                {
                                    MovePiece(pieceData1, player1, id1, source, dest, "bronze", 2, msg1, msg2,false);
                                    SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                }
                            }
                            else if (type2 == "knight")
                            {
                                Debug.Log("Type 2: Knight");
                                MovePiece(pieceData1, player1, id1, source, dest, "silver", 2, msg1, msg2,false);
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                            else if (type2 == "lord")
                            {
                                Debug.Log("Type 2: Lord");
                                MovePiece(pieceData1, player1, id1, source, dest, "silver", 2, msg1, msg2, false);
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                            else if (type2 == "king")
                            {
                                Debug.Log("Type 2: King");
                                MovePiece(pieceData1, player1, id1, source, dest, "silver", 2, msg1, msg2, false);
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                            else if (type2 == "commander")
                            {
                                Debug.Log("Type 2: Commander");
                                MovePiece(pieceData1, player1, id1, source, dest, "silver", 2, msg1, msg2, false);
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                        }
                    }
                    else if (boardPosStates[dest] == 0)
                    {
                        Debug.Log("Handling Player Move:1: Moving to empty place");
                        if (id1.Substring(0, 2).Equals("ki"))
                        {
                            MovePiece(player1.king,player1, id1, source, dest, "gold", 1, msg1, msg2);
                        }
                        else if (id1.Substring(0, 2).Equals("l1"))
                        {
                            MovePiece(player1.lord1, player1, id1, source, dest, "gold", 1, msg1, msg2);
                        }
                        else if (id1.Substring(0, 2).Equals("l2"))
                        {
                            MovePiece(player1.lord2, player1, id1, source, dest, "gold", 1, msg1, msg2);
                        }
                        else if (id1.Substring(0, 2).Equals("ck"))
                        {
                            MovePiece(player1.commanderk, player1, id1, source, dest, "silver", 1, msg1, msg2);
                        }
                        else if (id1.Substring(0, 2).Equals("c1"))
                        {
                            MovePiece(player1.commanderl1, player1, id1, source, dest, "silver", 1, msg1, msg2);
                        }
                        else if (id1.Substring(0, 2).Equals("c2"))
                        {
                            MovePiece(player1.commanderl2, player1, id1, source, dest, "silver", 1, msg1, msg2);
                        }
                        else if (id1.Substring(0, 2).Equals("kn"))
                        {
                            MovePiece(player1.knights[id1], player1, id1, source, dest, "silver", 1, msg1, msg2);
                        }
                        else if (id1.Substring(0, 2).Equals("so"))
                        {
                            MovePiece(player1.soldiers[id1], player1, id1, source, dest, "bronze", 1, msg1, msg2);
                        }
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
                                Debug.Log("type1: soldier || knight");
                                MovePiece(player2.king, player2, id2, source, dest, "gold", 2, msg1, msg2);
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                
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
                                    MovePiece(player2.king, player2, id2, source, dest, "gold", 2, msg1, msg2);
                                    LoD(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                }
                                else
                                {
                                    MovePiece(player2.king, player2, id2, source, dest, "gold", 1, msg1, msg2, false);
                                    KingD(false);
                                    return;
                                }
                            }
                            else if (type1 == "king")
                            {
                                if (Convert.ToString(player1.commanderk["state"]) == "alive")
                                {
                                    MovePiece(player2.king, player2, id2, source, dest, "gold", 1, msg1, msg2, false);
                                    KingD(false);
                                    return;
                                }
                                else
                                {
                                    MovePiece(player2.king, player2, id2, source, dest, "gold", 2, msg1, msg2);
                                    KingD(true);
                                    return;
                                }
                            }
                            else if (type1 == "commander")
                            {
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                if ((f2 - f1) == 1) //Head-On Attack
                                {
                                    MovePiece(player2.king, player2, id2, source, dest, "gold", 1, msg1, msg2, false);
                                    KingD(false);
                                    return;
                                }
                                else
                                {
                                    MovePiece(player2.king, player2, id2, source, dest, "gold", 2, msg1, msg2);
                                    ComD(pieceData1, player1, true, id1, msg1, msg2, dest);
                                }
                            }
                        }
                        else if (type2 == "lord")
                        {
                            Debug.Log("Type 2: Lord");
                            if (type1 == "soldier" || type1 == "knight")
                            {
                                Debug.Log("Type 1: Soilder || Knight");
                                MovePiece(pieceData2, player2, id2, source, dest, "gold", 2, msg1, msg2);
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                            else if (type1 == "lord")
                            {
                                Debug.Log("Type 1: Lord");
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
                                    MovePiece(pieceData2, player2, id2, source, dest, "gold", 2, msg1, msg2);
                                    LoD(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                }
                                else
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "gold", 1, msg1, msg2, false);
                                    LoD(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                }
                            }
                            else if (type1 == "king")
                            {
                                Debug.Log("Type 1: King");
                                if (Convert.ToString(player1.commanderk["state"]) == "alive")
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "gold", 1, msg1, msg2, false);
                                    LoD(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                }
                                else
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "gold", 2, msg1, msg2);
                                    KingD(false);
                                    return;
                                }
                            }
                            else if (type1 == "commander")
                            {
                                Debug.Log("Type 1: Commander");
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                if ((f2 - f1) == 1) //Head-On Attack
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "gold", 1, msg1, msg2, false);
                                    LoD(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                }
                                else
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "gold", 2, msg1, msg2);
                                    ComD(pieceData1, player1, true, id1, msg1, msg2, dest);
                                }
                                
                            }
                            
                        }
                        else if (type2 == "commander")
                        {
                            Debug.Log("Type 2: Commander");
                            MovePiece(pieceData2, player2, id2, source, dest, "silver", 2, msg1, msg2);
                            if (type1 == "soldier" || type1 == "knight")
                            {
                                Debug.Log("Type 1: Soilder || Knight");
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                            else if (type1 == "lord")
                            {
                                Debug.Log("Type 1: Lord");
                                LoD(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                            else if (type1 == "king")
                            {
                                Debug.Log("Type 1: King");
                                KingD(true);
                                return;
                            }
                            else if (type1 == "commander")
                            {
                                Debug.Log("Type 1: Commander");
                                ComD(pieceData1, player1, true, id1, msg1, msg2, dest);
                            }
                        }
                        else if (type2 == "knight")
                        {
                            Debug.Log("Type 2: Knight");
                            if (type1 == "soldier")
                            {
                                Debug.Log("Type 1: Soilder");
                                MovePiece(pieceData2, player2, id2, source, dest, "silver", 2, msg1, msg2);
                                SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                            }
                            else if (type1 == "knight")
                            {
                                Debug.Log("Type 1: Knight");
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                int curPower1 = Convert.ToInt32(pieceData1["power"]);
                                int curPower2 = Convert.ToInt32(pieceData2["power"]);
                                if ((f2 - f1) != 1)  //Sneak Attack
                                {
                                    curPower2++;
                                }
                                if (curPower2 >= curPower1)
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "silver", 2, msg1, msg2);
                                    SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                    if (Convert.ToInt32(pieceData2["power"]) != 7)
                                    {
                                        SoOrKnPI(pieceData2, id2, msg2);
                                    }
                                    else
                                    {
                                        if (Convert.ToInt32(pieceData1["power"]) == 7)
                                        {
                                            Dictionary<string, object> commander = player2.commanderk;
                                            if (Convert.ToString(pieceData2["serves"]) == "king")
                                            {
                                                commander = player2.commanderk;
                                            }
                                            else if (Convert.ToString(pieceData2["serves"]) == "lord1")
                                            {
                                                commander = player2.commanderl1;
                                            }
                                            else if (Convert.ToString(pieceData2["serves"]) == "lord2")
                                            {
                                                commander = player2.commanderl2;
                                            }
                                            if (commander == null)
                                            {
                                                SoOrKnUp(pieceData2, player2, id2, false, msg1, msg2, type2, dest);
                                            }
                                            else if (Convert.ToString(commander["state"]) != "alive")
                                            {
                                                SoOrKnUp(pieceData2, player2, id2, false, msg1, msg2, type2, dest);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "silver", 1, msg1, msg2, false);
                                    SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                }
                            }
                            else if (type1 == "lord")
                            {
                                Debug.Log("Type 1: Lord");
                                MovePiece(pieceData2, player2, id2, source, dest, "silver", 1, msg1, msg2, false);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                            else if (type1 == "king")
                            {
                                Debug.Log("Type 1: King");
                                MovePiece(pieceData2, player2, id2, source, dest, "silver", 1, msg1, msg2, false);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                            else if (type1 == "commander")
                            {
                                Debug.Log("Type 1: Commander");
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                if ((f2 - f1) != 1) //Sneak Attack
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "silver", 2, msg1, msg2);
                                    ComD(pieceData1, player1, true, id1, msg1, msg2, dest);
                                    if (Convert.ToInt32(pieceData2["power"]) != 7)
                                    {
                                        SoOrKnPI(pieceData2, id2, msg2);
                                    }
                                    else
                                    {
                                        Dictionary<string, object> commander = player2.commanderk;
                                        if (Convert.ToString(pieceData2["serves"]) == "king")
                                        {
                                            commander = player2.commanderk;
                                        }
                                        else if (Convert.ToString(pieceData2["serves"]) == "lord1")
                                        {
                                            commander = player2.commanderl1;
                                        }
                                        else if (Convert.ToString(pieceData2["serves"]) == "lord2")
                                        {
                                            commander = player2.commanderl2;
                                        }
                                        if (commander == null)
                                        {
                                            SoOrKnUp(pieceData2, player2, id2, false, msg1, msg2, type2, dest);
                                        }
                                        else if (Convert.ToString(commander["state"]) != "alive")
                                        {
                                            SoOrKnUp(pieceData2, player2, id2, false, msg1, msg2, type2, dest);
                                        }
                                    }
                                }
                                else
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "silver", 1, msg1, msg2, false);
                                    SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                }
                            }
                        }
                        else if (type2 == "soldier")
                        {
                            Debug.Log("Type 2: Soldier");
                            if (type1 == "soldier")
                            {
                                Debug.Log("Type 1: Soilder");
                                int f1 = source / 10;
                                int f2 = dest / 10;
                                int curPower1 = Convert.ToInt32(pieceData1["power"]);
                                int curPower2 = Convert.ToInt32(pieceData2["power"]);
                                if ((f2 - f1) != 1)  //Sneak Attack
                                {
                                    curPower2++;
                                }
                                if (curPower2 >= curPower1)
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "bronze", 2, msg1, msg2);
                                    SoOrKnDI(player1, player2, id1, true, msg1, msg2, type1, type2, dest);
                                    if (Convert.ToInt32(pieceData2["power"]) != 4)
                                    {
                                        SoOrKnPI(pieceData2, id2, msg2);
                                    }
                                    else
                                    {
                                        if (Convert.ToInt32(pieceData1["power"]) == 4)
                                        {
                                            SoOrKnUp(pieceData2, player2, id2, false, msg1, msg2, type2, dest);
                                        }
                                    }
                                }
                                else
                                {
                                    MovePiece(pieceData2, player2, id2, source, dest, "bronze", 1, msg1, msg2, false);
                                    SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                                }
                            }
                            else if (type2 == "knight")
                            {
                                Debug.Log("Type 2: Knight");
                                MovePiece(pieceData2, player2, id2, source, dest, "silver", 1, msg1, msg2, false);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                            else if (type2 == "lord")
                            {
                                Debug.Log("Type 2: Lord");
                                MovePiece(pieceData2, player2, id2, source, dest, "silver", 1, msg1, msg2, false);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                            else if (type2 == "king")
                            {
                                Debug.Log("Type 2: King");
                                MovePiece(pieceData2, player2, id2, source, dest, "silver", 1, msg1, msg2, false);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                            else if (type2 == "commander")
                            {
                                Debug.Log("Type 2: Commander");
                                MovePiece(pieceData2, player2, id2, source, dest, "silver", 1, msg1, msg2, false);
                                SoOrKnDI(player2, player1, id2, false, msg1, msg2, type2, type1, dest);
                            }
                        }
                    }
                    else if (boardPosStates[dest] == 0)
                    {
                        Debug.Log("Handling Player Move:2:Move to empty space");
                        if (id2.Substring(0, 2).Equals("ki"))
                        {
                            MovePiece(player2.king, player2, id2, source, dest, "gold", 2, msg1, msg2);
                        }
                        else if (id2.Substring(0, 2).Equals("l1"))
                        {
                            MovePiece(player2.lord1, player2, id2, source, dest, "gold", 2, msg1, msg2);
                        }
                        else if (id2.Substring(0, 2).Equals("l2"))
                        {
                            MovePiece(player2.lord2, player2, id2, source, dest, "gold", 2, msg1, msg2);
                        }
                        else if (id2.Substring(0, 2).Equals("ck"))
                        {
                            MovePiece(player2.commanderk, player2, id2, source, dest, "silver", 2, msg1, msg2);
                        }
                        else if (id2.Substring(0, 2).Equals("c1"))
                        {
                            MovePiece(player2.commanderl1, player2, id2, source, dest, "silver", 2, msg1, msg2);
                        }
                        else if (id2.Substring(0, 2).Equals("c2"))
                        {
                            MovePiece(player2.commanderl2, player2, id2, source, dest, "silver", 2, msg1, msg2);
                        }
                        else if (id2.Substring(0, 2).Equals("kn"))
                        {
                            MovePiece(player2.knights[id2], player2, id2, source, dest, "silver", 2, msg1, msg2);
                        }
                        else if (id2.Substring(0, 2).Equals("so"))
                        {
                            MovePiece(player2.soldiers[id2], player2, id2, source, dest, "bronze", 2, msg1, msg2);
                        }
                    }
                }
            }
            nserver.SendM(0, msg1);
            nserver.SendM(1, msg2);
            StartCoroutine(SendTurnChanged());
        }
    }

    public void MovePiece(Dictionary<string,object> pieceData ,PlayerData movePlayer,string moveId,int source,int dest,string color,int teamN,SimpleMessage msg1,SimpleMessage msg2,bool win=true)
    {
        Dictionary<string, object> movep = new Dictionary<string, object> { };
        Dictionary<string, object> omovep = new Dictionary<string, object> { };
        movep.Add("myTeam", true);
        movep.Add("id", moveId);
        movep.Add("action", "move");
        movep.Add("source", source);
        movep.Add("dest", dest);
        movep.Add("color", color);
        movep.Add("win", win);
        omovep.Add("myTeam", false);
        omovep.Add("id", moveId);
        omovep.Add("action", "move");
        omovep.Add("source", source);
        omovep.Add("dest", dest);
        omovep.Add("color", color);
        omovep.Add("win", win);
        boardPosStates[source] = 0;
        boardPosStates[dest] = teamN;
        pieceData["posI"] = Convert.ToInt32(Math.Floor(Convert.ToDouble(dest / 10)));
        pieceData["posJ"] = dest % 10;
        movePlayer.positionIdMatcher.Remove(source);
        if (win)
        {
            movePlayer.positionIdMatcher.Add(dest, moveId);
        }
        msg1.listData = boardPosStates.ConvertAll<object>(k => k);
        msg2.listData = boardPosStates.ConvertAll<object>(k => k);
        if (teamN == 1)
        {
            msg1.listdictdata.Add(movep);
            msg2.listdictdata.Add(omovep);
        }
        else if(teamN==2)
        {
            msg1.listdictdata.Add(omovep);
            msg2.listdictdata.Add(movep);
        }
        
    }

    public void SoOrKnPI(Dictionary<string, object> powPieceData, string powId,SimpleMessage msg)
    {
        Dictionary<string, object> temp = new Dictionary<string, object> { };
        temp.Add("myTeam", true);
        temp.Add("id", powId);
        temp.Add("action", "power");
        temp.Add("color", Convert.ToString(powPieceData["color"]));
        powPieceData["power"] = Convert.ToInt32(powPieceData["power"]) + 1;
        msg.listdictdata.Add(temp);
    }

    public void SoOrKnUp(Dictionary<string,object> upPieceData,PlayerData upgradePlayer,string upId,bool team1U,SimpleMessage msg1,SimpleMessage msg2,string upType,int dest)
    {
        Dictionary<string, object> uptemp = new Dictionary<string, object> { };
        Dictionary<string, object> ouptemp = new Dictionary<string, object> { };
        uptemp.Add("myTeam", true);
        ouptemp.Add("myTeam", false);
        uptemp.Add("id", upId);
        ouptemp.Add("id", upId);
        uptemp.Add("action", "upgrade");
        ouptemp.Add("action", "upgrade");
        upPieceData["state"] = "upgraded";
        upgradePlayer.positionIdMatcher.Remove(dest);
        if (upType == "soldier")
        {
            uptemp.Add("color", "bronze");
            uptemp.Add("newcolor", "silver");
            uptemp.Add("newId", "kn"+upId.Substring(2));
            ouptemp.Add("color", "bronze");
            ouptemp.Add("newcolor", "silver");
            ouptemp.Add("newId", "kn" + upId.Substring(2));
            upgradePlayer.addKnight(Convert.ToInt32(upPieceData["posI"]),Convert.ToInt32(upPieceData["posJ"]),5,Convert.ToString(upPieceData["serves"]), "kn" + upId.Substring(2));
        }
        else if(upType=="knight")
        {
            uptemp.Add("color", "silver");
            uptemp.Add("newcolor", "silver");
            ouptemp.Add("color", "silver");
            ouptemp.Add("newcolor", "silver");
            if (Convert.ToString(upPieceData["serves"])=="king")
            {
                upgradePlayer.commanderk = null;
                upgradePlayer.InitializeCommanderK(Convert.ToInt32(upPieceData["posI"]),Convert.ToInt32(upPieceData["posJ"]),"ck"+upId.Substring(2));
                uptemp.Add("newId", "ck" + upId.Substring(2));
                ouptemp.Add("newId", "ck" + upId.Substring(2));
            }
            else if (Convert.ToString(upPieceData["serves"]) == "lord1")
            {
                upgradePlayer.commanderl1 = null;
                upgradePlayer.InitializeCommanderL1(Convert.ToInt32(upPieceData["posI"]), Convert.ToInt32(upPieceData["posJ"]), "ck" + upId.Substring(2));
                uptemp.Add("newId", "ck" + upId.Substring(2));
                ouptemp.Add("newId", "ck" + upId.Substring(2));
            }
            else if (Convert.ToString(upPieceData["serves"]) == "lord2")
            {
                upgradePlayer.commanderl2 = null;
                upgradePlayer.InitializeCommanderL2(Convert.ToInt32(upPieceData["posI"]), Convert.ToInt32(upPieceData["posJ"]), "ck" + upId.Substring(2));
                uptemp.Add("newId", "ck" + upId.Substring(2));
                ouptemp.Add("newId", "ck" + upId.Substring(2));
            }
        }
        if (team1U)
        {
            msg1.listdictdata.Add(uptemp);
            msg2.listdictdata.Add(ouptemp);
        }
        else
        {
            msg1.listdictdata.Add(ouptemp);
            msg2.listdictdata.Add(uptemp);
        }
    }

    public void SoOrKnDI(PlayerData deadPlayer,PlayerData killPlayer,string deadId,bool team1D,SimpleMessage msg1,SimpleMessage msg2,string deadType,string killType,int dest)
    {
        Dictionary<string, object> deadtemp = new Dictionary<string, object> { };
        Dictionary<string, object> killtemp = new Dictionary<string, object> { };
        deadtemp.Add("myTeam", true);
        deadtemp.Add("id", deadId);
        killtemp.Add("myTeam", false);
        killtemp.Add("id", deadId);
        if (deadType == "soldier")
        {
            if (Convert.ToString(deadPlayer.soldiers[deadId]["serves"]) == "king")
            {
                deadtemp.Add("action", "dead");
                killtemp.Add("action", "dead");
                deadPlayer.soldiers[deadId]["state"] = "dead";
            }
            else
            {
                deadtemp.Add("action", "injured");
                killtemp.Add("action", "injured");
                deadPlayer.soldiers[deadId]["state"] = "injured";
                if(Convert.ToString(deadPlayer.soldiers[deadId]["serves"]) == "lord1")
                {
                    deadtemp.Add("fort", "1");
                    killtemp.Add("fort", "1");
                    deadtemp.Add("fortId", deadId);
                    killtemp.Add("fortId", deadId);
                    deadPlayer.fort1.Add(deadId);
                }
                else if (Convert.ToString(deadPlayer.soldiers[deadId]["serves"]) == "lord2")
                {
                    deadtemp.Add("fort", "2");
                    killtemp.Add("fort", "2");
                    deadtemp.Add("fortId", deadId);
                    killtemp.Add("fortId", deadId);
                    deadPlayer.fort2.Add(deadId);
                }
            }
            deadtemp.Add("color", "bronze");
            killtemp.Add("color", "bronze");
        }
        else if (deadType == "knight")
        {
            if (Convert.ToString(deadPlayer.knights[deadId]["serves"]) == "king")
            {
                deadtemp.Add("action", "dead");
                killtemp.Add("action", "dead");
            }
            else
            {
                deadtemp.Add("action", "injured");
                killtemp.Add("action", "injured");
                if (Convert.ToString(deadPlayer.knights[deadId]["serves"]) == "lord1")
                {
                    deadtemp.Add("fort", "1");
                    killtemp.Add("fort", "1");
                    deadtemp.Add("fortId", deadId);
                    killtemp.Add("fortId", deadId);
                    deadPlayer.fort1.Add(deadId);
                }
                else if (Convert.ToString(deadPlayer.knights[deadId]["serves"]) == "lord2")
                {
                    deadtemp.Add("fort", "2");
                    killtemp.Add("fort", "2");
                    deadtemp.Add("fortId", deadId);
                    killtemp.Add("fortId", deadId);
                    deadPlayer.fort2.Add(deadId);
                }
            }
            deadtemp.Add("color", "silver");
            killtemp.Add("color", "silver");
        }
        deadPlayer.positionIdMatcher.Remove(dest);
        if (team1D)
        {
            msg1.listdictdata.Add(deadtemp);
            msg2.listdictdata.Add(killtemp);
        }
        else
        {
            msg1.listdictdata.Add(killtemp);
            msg2.listdictdata.Add(deadtemp);
        }
    }

    public void LoD(PlayerData deadPlayer, PlayerData killPlayer, string deadId, bool team1D, SimpleMessage msg1, SimpleMessage msg2, string deadType, string killType, int dest)
    {
        Dictionary<string, object> deadtemp = new Dictionary<string, object> { };
        Dictionary<string, object> killtemp = new Dictionary<string, object> { };
        deadtemp.Add("myTeam", true);
        killtemp.Add("myTeam", false);
        deadtemp.Add("id", deadId);
        killtemp.Add("id", deadId);
        deadtemp.Add("action", "dead");
        killtemp.Add("action", "dead");
        deadtemp.Add("color", "gold");
        killtemp.Add("color", "gold");
        if (team1D)
        {
            msg1.listdictdata.Add(deadtemp);
            msg2.listdictdata.Add(killtemp);
        }
        else
        {
            msg1.listdictdata.Add(killtemp);
            msg2.listdictdata.Add(deadtemp);
        }
        List<string> fort = deadPlayer.fort1;
        string lordn = "1";
        Dictionary<string, object> commander = deadPlayer.commanderl1;
        if (deadId.Substring(0, 2).Equals("l1"))
        {
            deadPlayer.lord1["state"] = "dead";
            fort = deadPlayer.fort1;
            lordn = "1";
            commander = deadPlayer.commanderl1;
        }
        else
        {
            deadPlayer.lord2["state"] = "dead";
            fort = deadPlayer.fort2;
            lordn = "2";
            commander = deadPlayer.commanderl2;
        }
        
        for (int i = 0; i < fort.Count; i++)
        {
            string tempId = fort[i];
            Dictionary<string, object> addptemp = new Dictionary<string, object> { };
            Dictionary<string, object> oaddptemp = new Dictionary<string, object> { };
            if ((tempId.Substring(0, 2).Equals("kn") && Convert.ToInt32(deadPlayer.knights[tempId]["power"]) == 7))
            {
                if (tempId.Substring(0, 2).Equals("kn"))
                {
                    deadPlayer.knights[tempId]["state"] = "opp";
                }
                int tempPos = -1;
                if (team1D)
                {
                    tempPos = findAndFillPlace("2");
                }
                else
                {
                    tempPos = findAndFillPlace("1");
                }
                string newId = "kn" + PlayerData.RandomString(7);
                killPlayer.addKnight(tempPos / 10, tempPos % 10, 5, "king", newId);
                addptemp.Add("myTeam", true);
                addptemp.Add("id", newId);
                addptemp.Add("action", "add");
                addptemp.Add("type", "knight");
                addptemp.Add("color", "silver");
                addptemp.Add("power", 5);
                addptemp.Add("pos", tempPos);
                oaddptemp.Add("myTeam", false);
                oaddptemp.Add("id", newId);
                oaddptemp.Add("action", "add");
                oaddptemp.Add("color", "silver");
                oaddptemp.Add("pos", tempPos);
            }
            else if (tempId.Substring(0, 2).Equals("kn"))
            {
                deadPlayer.knights[tempId]["state"] = "opp";
                int tempPos = -1;
                if (team1D)
                {
                    tempPos = findAndFillPlace("2");
                }
                else
                {
                    tempPos = findAndFillPlace("1");
                }
                string newId = "so" + PlayerData.RandomString(7);
                int tempPower = 0;
                if (Convert.ToInt32(deadPlayer.knights[tempId]["power"]) == 6)
                {
                    tempPower = 4;
                }
                else if (Convert.ToInt32(deadPlayer.knights[tempId]["power"]) == 5)
                {
                    tempPower = 3;
                }
                killPlayer.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                addptemp.Add("myTeam", true);
                addptemp.Add("id", newId);
                addptemp.Add("action", "add");
                addptemp.Add("type", "soldier");
                addptemp.Add("color", "bronze");
                addptemp.Add("power", tempPower);
                addptemp.Add("pos", tempPos);
                oaddptemp.Add("myTeam", false);
                oaddptemp.Add("id", newId);
                oaddptemp.Add("action", "add");
                oaddptemp.Add("color", "bronze");
                oaddptemp.Add("pos", tempPos);
            }
            else if (tempId.Substring(0, 2).Equals("so"))
            {
                deadPlayer.soldiers[tempId]["state"] = "opp";
                int tempPos = -1;
                if (team1D)
                {
                    tempPos = findAndFillPlace("2");
                }
                else
                {
                    tempPos = findAndFillPlace("1");
                }
                string newId = "so" + PlayerData.RandomString(7);
                int tempPower = 1;
                if (Convert.ToInt32(deadPlayer.soldiers[tempId]["power"]) == 4)
                {
                    tempPower = 2;
                }
                killPlayer.addSoldier(tempPos / 10, tempPos % 10, tempPower, "king", newId);
                addptemp.Add("myTeam", true);
                addptemp.Add("id", newId);
                addptemp.Add("action", "add");
                addptemp.Add("type", "soldier");
                addptemp.Add("color", "bronze");
                addptemp.Add("power", tempPower);
                addptemp.Add("pos", tempPos);
                oaddptemp.Add("myTeam", false);
                oaddptemp.Add("id", newId);
                oaddptemp.Add("action", "add");
                oaddptemp.Add("color", "bronze");
                oaddptemp.Add("pos", tempPos);
            }
            if (team1D)
            {
                msg1.listdictdata.Add(oaddptemp);
                msg2.listdictdata.Add(addptemp);
            }
            else
            {
                msg1.listdictdata.Add(addptemp);
                msg2.listdictdata.Add(oaddptemp);
            }
        }
        if (Convert.ToString(commander["state"]) == "alive")
        {
            //player1.commanderl1["serves"] = "king";
            commander["state"] = "change";
            string tempid1 = Convert.ToString(commander["id"]);
            tempid1 = "kn" + tempid1.Substring(2);
            deadPlayer.positionIdMatcher.Remove(Convert.ToInt32(commander["posI"]) * 10 + Convert.ToInt32(commander["posJ"]));
            deadPlayer.addKnight(Convert.ToInt32(commander["posI"]), Convert.ToInt32(commander["posJ"]), 5, "king", tempid1);
            Dictionary<string, object> chtemp = new Dictionary<string, object> { };
            Dictionary<string, object> ochtemp = new Dictionary<string, object> { };
            chtemp.Add("myTeam", true);
            chtemp.Add("id", commander["id"]);
            chtemp.Add("action", "changeserves");
            chtemp.Add("newId", tempid1);
            chtemp.Add("color", "silver");
            ochtemp.Add("myTeam", false);
            ochtemp.Add("id", commander["id"]);
            ochtemp.Add("action", "changeserves");
            ochtemp.Add("newId", tempid1);
            ochtemp.Add("color", "silver");
            if (team1D)
            {
                msg1.listdictdata.Add(chtemp);
                msg2.listdictdata.Add(ochtemp);
            }
        }
        Dictionary<string, Dictionary<string, object>> tempDictDict = new Dictionary<string, Dictionary<string, object>> { };
        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in deadPlayer.knights)
        {
            Dictionary<string, object> tempDict = new Dictionary<string, object>(tempItem.Value);
            if (Convert.ToString(tempDict["state"]) == "alive" && Convert.ToString(tempDict["serves"]) == "lord"+lordn)
            {
                tempDict["power"] = 5;
                tempDict["serves"] = "king";
                Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                temp1.Add("myTeam", true);
                temp1.Add("id", tempDict["id"]);
                temp1.Add("action", "changeserves");
                temp1.Add("newId", tempDict["id"]);
                temp1.Add("color", "silver");
                if (team1D)
                {
                    msg1.listdictdata.Add(temp1);
                }
                else
                {
                    msg2.listdictdata.Add(temp1);
                }
            }
            tempDictDict.Add(tempItem.Key, tempDict);
        }
        deadPlayer.knights= new Dictionary<string, Dictionary<string, object>>(tempDictDict);
        tempDictDict = new Dictionary<string, Dictionary<string, object>> { };
        foreach (KeyValuePair<string, Dictionary<string, object>> tempItem in deadPlayer.soldiers)
        {
            Dictionary<string, object> tempDict = new Dictionary<string, object>(tempItem.Value);
            if (Convert.ToString(tempItem.Value["state"]) == "alive" && Convert.ToString(tempItem.Value["serves"]) == "lord"+lordn)
            {
                tempDict["power"] = 1;
                tempDict["serves"] = "king";
                Dictionary<string, object> temp1 = new Dictionary<string, object> { };
                temp1.Add("myTeam", true);
                temp1.Add("id", tempDict["id"]);
                temp1.Add("action", "changeserves");
                temp1.Add("newId", tempDict["id"]);
                temp1.Add("color", "bronze");
                if (team1D)
                {
                    msg1.listdictdata.Add(temp1);
                }
                else
                {
                    msg2.listdictdata.Add(temp1);
                }
            }
            tempDictDict.Add(tempItem.Key, tempDict);
        }
        deadPlayer.soldiers= new Dictionary<string, Dictionary<string, object>>(tempDictDict);
        Dictionary<string, object> clrf = new Dictionary<string, object> { };
        Dictionary<string, object> oclrf = new Dictionary<string, object> { };
        clrf.Add("myTeam", true);
        clrf.Add("id", "");
        clrf.Add("action", "clearfort");
        clrf.Add("fort", lordn);
        oclrf.Add("myTeam", false);
        oclrf.Add("id", "");
        oclrf.Add("action", "clearfort");
        oclrf.Add("fort", lordn);
        fort = null;
        deadPlayer.positionIdMatcher.Remove(dest);
        if (team1D)
        {
            msg1.listdictdata.Add(clrf);
            msg2.listdictdata.Add(oclrf);
        }
        else
        {
            msg1.listdictdata.Add(oclrf);
            msg2.listdictdata.Add(clrf);
        }
    }

    public void ComD(Dictionary<string,object> deadPiece,PlayerData deadPlayer,bool team1D,string deadId,SimpleMessage msg1,SimpleMessage msg2,int dest)
    {
        Dictionary<string, object> deadtemp = new Dictionary<string, object> { };
        Dictionary<string, object> killtemp = new Dictionary<string, object> { };
        deadtemp.Add("myTeam", true);
        killtemp.Add("myTeam", false);
        deadtemp.Add("id", deadId);
        killtemp.Add("id", deadId);
        deadtemp.Add("color", "silver");
        killtemp.Add("color", "silver");
        deadPlayer.positionIdMatcher.Remove(dest);
        if (deadId.Substring(0, 2).Equals("ck"))
        {
            deadPiece["state"] = "dead";
            deadtemp.Add("action", "dead");
            killtemp.Add("action", "dead");
        }
        else if (deadId.Substring(0, 2).Equals("c1"))
        {
            deadPiece["state"] = "injured";
            deadtemp.Add("action", "injured");
            killtemp.Add("action", "injured");
            deadtemp.Add("fort", "1");
            killtemp.Add("fort", "1");
            deadPlayer.addKnight(Convert.ToInt32(deadPiece["posI"]), Convert.ToInt32(deadPiece["posJ"]), 7, "lord1", "kn" + deadId.Substring(2));
            deadPlayer.knights["kn" + deadId.Substring(2)]["state"] = "injured";
            deadPiece = null;
            deadtemp.Add("fortId", "kn"+deadId.Substring(2));
            killtemp.Add("fortId", "kn" + deadId.Substring(2));
            deadPlayer.fort1.Add("kn" + deadId.Substring(2));
        }
        else if (deadId.Substring(0, 2).Equals("c2"))
        {
            deadPiece["state"] = "injured";
            deadtemp.Add("action", "injured");
            killtemp.Add("action", "injured");
            deadtemp.Add("fort", "2");
            killtemp.Add("fort", "2"); 
            deadPlayer.addKnight(Convert.ToInt32(deadPiece["posI"]), Convert.ToInt32(deadPiece["posJ"]), 7, "lord1", "kn" + deadId.Substring(2));
            deadPlayer.knights["kn" + deadId.Substring(2)]["state"] = "injured";
            deadPiece = null;
            deadtemp.Add("fortId", "kn" + deadId.Substring(2));
            killtemp.Add("fortId", "kn" + deadId.Substring(2));
            deadPlayer.fort2.Add("kn" + deadId.Substring(2));
        }
        deadPlayer.positionIdMatcher.Remove(dest);
        if (team1D)
        {
            msg1.listdictdata.Add(deadtemp);
            msg2.listdictdata.Add(killtemp);
        }
        else
        {
            msg2.listdictdata.Add(deadtemp);
            msg1.listdictdata.Add(killtemp);
        }
    }

    public void KingD(bool team1D)
    {
        if (team1D)
        {
            SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
            SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
            msgg1.win = false;
            msgg2.win = true;
            nserver.SendM(0, msgg1);
            nserver.SendM(1, msgg2);
        }
        else
        {
            SimpleMessage msgg2 = new SimpleMessage(MessageType.GameResult, "Your King Died.You lost the war");
            SimpleMessage msgg1 = new SimpleMessage(MessageType.GameResult, "Your opponent's King Died.You won the war");
            msgg1.win = true;
            msgg2.win = false;
            nserver.SendM(0, msgg1);
            nserver.SendM(1, msgg2);
        }
        nserver.gamelift.TerminateGameSession();
        this.resetServer();
        nserver.ResetNetworkServer();
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
