﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace LudoNewWorld.Classes
{
    class GameEngine
    {
        public static Player.RowBoat LastPressedBoat { get; set; }
        public static GameTile LastPressedGameTile { get; set; }
        private static Player ActivePlayer { get; set; }
        public static int LastDiceRoll { get; set; }
        public static int Tick { get; set; }
        public static int SwitchPlayerTimer { get; set; }
        public static int moveAITick { get; set; }
        public static int PlayerTurn { get; set; }

        private static int currentTileIndex;
        private static int round = 1;
        private static bool newRound = true;
        private static bool gameActive = false;

        public static bool diceRolled = false;
        public static bool playerCanMove = false;
        public static bool playerRoundCompleted = false;
        public static bool moveConfirmed = false;
        public Player.RowBoat boat;
        public GameTile tile;
        public Player p1, p2, p3, p4;

        public static List<Faction> factionList = new List<Faction>();
        private static readonly Random _random = new Random();

        /// <summary>
        /// Creates four Player objects and places them into
        /// <see cref="Player.playerList"/>
        /// </summary>
        /// <param name="faction"></param>
        public void CreatePlayers(Faction faction)
        {
            factionList.Add(Faction.Britain);
            factionList.Add(Faction.Dutch);
            factionList.Add(Faction.Spain);
            factionList.Add(Faction.France);
            factionList.Remove(faction);

            if (faction == Faction.Britain)
            {
                Player.playerList.Add(p1 = new Player(1, faction, true));
                Player.playerList.Add(p2 = new Player(2, factionList[0], false));
                Player.playerList.Add(p3 = new Player(3, factionList[1], false));
                Player.playerList.Add(p4 = new Player(4, factionList[2], false));
            }
            else if (faction == Faction.Dutch)
            {
                Player.playerList.Add(p1 = new Player(1, factionList[0], false));
                Player.playerList.Add(p2 = new Player(2, faction, true));
                Player.playerList.Add(p3 = new Player(3, factionList[1], false));
                Player.playerList.Add(p4 = new Player(4, factionList[2], false));
            }
            else if (faction == Faction.Spain)
            {
                Player.playerList.Add(p1 = new Player(1, factionList[0], false));
                Player.playerList.Add(p2 = new Player(2, factionList[1], false));
                Player.playerList.Add(p3 = new Player(3, faction, true));
                Player.playerList.Add(p4 = new Player(4, factionList[2], false));
            }
            else if (faction == Faction.France)
            {
                Player.playerList.Add(p1 = new Player(1, factionList[0], false));
                Player.playerList.Add(p2 = new Player(2, factionList[1], false));
                Player.playerList.Add(p3 = new Player(3, factionList[2], false));
                Player.playerList.Add(p4 = new Player(4, faction, true));
            }
        }

        /// <summary>
        /// <para>Creates all player and rowboat objects needed to play the game</para>
        /// <para>Takes in what <see cref="Faction"/> the player picked in menu</para>
        /// </summary>
        /// <param name="faction">What faction are the player picking in the start menu</param>
        public void StartGame(Faction faction)
        {
            CreatePlayers(faction);
            MainPage.showDice = true;
            switch (faction)
            {
                case Faction.Britain:
                    PlayerTurn = 1; break;
                case Faction.Dutch:
                    PlayerTurn = 2; break;
                case Faction.Spain:
                    PlayerTurn = 3; break;
                case Faction.France:
                    PlayerTurn = 4; break;
                default: break;
            }
            gameActive = true;
            NextRound();
        }

        /// <summary>
        /// Round loop, Is called for each update (60 times a second)
        /// This method is used to send the active player objects into <see cref="Player.CheckIfMovable(Player.RowBoat, int)"/>
        /// Will also call for either <see cref="Player.MoveRowBoat"/> or <see cref="GameEngine.MoveAI"/> based on if the player is human or AI
        /// <para><see cref="gameActive"/> needs to be set True</para>
        /// </summary>
        public void NextRound()
        {
            // First step, is to check if a new round is happening or not
            if (newRound)
            {
                SwitchPlayerTimer = 0;
                round++;
                newRound = false;
            }
            switch (PlayerTurn)
            {
                case 1: ActivePlayer = p1; break;
                case 2: ActivePlayer = p2; break;
                case 3: ActivePlayer = p3; break;
                case 4: ActivePlayer = p4; break;
                case 0: ActivePlayer = ActivePlayer; break;
            }
            // If the active player is AI
            if(gameActive && !ActivePlayer.IsHuman)
            {
                // Send all rowboat objects from the AI player into the MoveAI method
                if (!diceRolled) LastDiceRoll = GraphicHandler.ScrambleDice(ActivePlayer.ID);
                diceRolled = true;
                MoveAI();
            }
            // If the active player is a player/human
            else if(gameActive && ActivePlayer.IsHuman)
            {
                if (diceRolled)
                {
                    // Send all rowboat objects from the human player into the Player.CheckIfMovable method
                    foreach (var boat in ActivePlayer.rowBoats)
                    {
                        ActivePlayer.CheckIfMovable(boat, LastDiceRoll);
                    }
                    if (Player.targetableRowBoats.Count <= 0) SwitchPlayer();
                }
                diceRolled = false;

                // Reset highlight effect outside the window so it isnt visible
                if (LastPressedBoat == null)
                {
                    GraphicHandler.highlighter.GameTileVector = new Vector2(2000, 2000);
                }

                // This segment is used to calculate the amount of steps a boat thats been targeted by the user and highlight the tile
                if (LastPressedBoat != null)
                {
                    playerCanMove = true;
                    currentTileIndex = LastPressedBoat.CurrentTile;
                    if (LastPressedBoat.CurrentTile + LastDiceRoll > 43)
                    {
                        currentTileIndex = currentTileIndex - 43 + LastDiceRoll - 1;
                    }
                    else
                    {
                        currentTileIndex += LastDiceRoll;
                    }
                    GraphicHandler.highlighter.GameTileVector = GetHighlightVector(currentTileIndex);
                }

                // Logic to move a boat
                // Require that the user has pressed both a boat and the specific tile we have checked is OK to move to
                if (LastPressedBoat != null && LastPressedBoat.targetable)
                {
                    GraphicHandler.highlighter.GameTileVector = new Vector2(2000, 2000);
                    if (GraphicHandler.GetOrderedTiles().IndexOf(LastPressedGameTile) == LastPressedBoat.CurrentTile + LastDiceRoll)
                    {
                        moveConfirmed = true;
                    }
                    else if ((LastPressedBoat.CurrentTile + LastDiceRoll - 1) >= 43)
                    {
                        moveConfirmed = true;
                    }
                    if (moveConfirmed)
                    {
                        ActivePlayer.MoveRowBoat();
                        GraphicHandler.highlighter.GameTileVector = new Vector2(2000, 2000);
                        playerRoundCompleted = true;
                        playerCanMove = false;
                        foreach (var boat in Player.targetableRowBoats)
                        {
                            boat.targetable = false;
                        }
                        if (LastDiceRoll == 6 && GraphicHandler.GetOrderTile(currentTileIndex).TileType != Tile.NegativeTile 
                            && GraphicHandler.GetOrderTile(currentTileIndex).TileType != Tile.RandomTile
                            || GraphicHandler.GetOrderTile(currentTileIndex).TileType == Tile.PositiveTile)
                        {
                            Player.PositiveTileEffect();
                            LastPressedBoat = null;
                            Player.targetableRowBoats.Clear();
                        }
                        else if (GraphicHandler.GetOrderTile(currentTileIndex).TileType == Tile.RandomTile)
                        {
                            int randomNumber = _random.Next(1, 3);
                            if (randomNumber == 1)
                            {
                                Player.PositiveTileEffect();
                                LastPressedBoat = null;
                                Player.targetableRowBoats.Clear();
                            }
                            else
                            {
                                Player.RandomNegativeTile(LastPressedBoat);
                                SwitchPlayer();
                                Player.targetableRowBoats.Clear();
                            }
                        }
                        else
                        {
                            SwitchPlayer();
                            Player.targetableRowBoats.Clear();
                        }
                    }
                    else
                    {
                        // In the case a user press the incorrect tile/anywhere else on the map that isnt the correct tile
                        LastPressedBoat = null;
                    }
                }
            }
        }

        private void MoveAI()
        {
            // Send all of the current players ships into CheckIfMovable to make sure they follow the logic rules
            // CheckIfMovable fills a list of "targetable" boats that we later then use to move AI ships.
            foreach (var ship in ActivePlayer.rowBoats)
            {
                ActivePlayer.CheckIfMovable(ship, LastDiceRoll);
            }
            // If the list targetableRowBoats comes back empty, the game should change player as the current player cant make a move
            if (Player.targetableRowBoats.Count <= 0)
            {
                SwitchPlayer();
                diceRolled = false;
                MainPage.showDice = true;
                MainPage.showDice = true;
            }
            else if(Player.targetableRowBoats.Count > 0 && moveAITick >= 80)
            {
                if(Player.targetableRowBoats.Count <= 0)
                {
                    SwitchPlayer();
                }
                else
                {
                    // Generate a random number between 0 and the amount of ships in the player objects rowboat list
                    int boatNumber = _random.Next(0, Player.targetableRowBoats.Count);
                    Player.RowBoat boat = Player.targetableRowBoats[boatNumber];

                    // Get the GameTile index of the ship we just generated a random index for
                    int currentTileIndex = boat.CurrentTile;

                    // Now we need to get the target game tile index & game tile object based on currentTileIndex + dice roll
                    int targetTileIndex = GraphicHandler.GetOrderedTiles().IndexOf(GraphicHandler.GetOrderTile(currentTileIndex + LastDiceRoll));
                    GameTile targetTile = GraphicHandler.GetOrderTile(targetTileIndex);

                    // Move ship to target tile
                    float shipX = targetTile.GameTileVector.X - 10;
                    float shipY = targetTile.GameTileVector.Y - 25;
                    boat.Vector = new Vector2(shipX, shipY);
                    boat.active = true;
                    boat.CurrentTile = targetTileIndex;
                    targetTile.IsPlayerOnTile = true;
                    Player.DestroyRowBoat(boat, targetTile);

                    foreach (var ship in Player.targetableRowBoats)
                    {
                        ship.targetable = false;
                    }
                    diceRolled = false;
                    Player.targetableRowBoats.Clear();
                    if (LastDiceRoll == 6 && GraphicHandler.GetOrderTile(targetTileIndex).TileType != Tile.NegativeTile 
                        && GraphicHandler.GetOrderTile(targetTileIndex).TileType != Tile.RandomTile
                        || GraphicHandler.GetOrderTile(targetTileIndex).TileType == Tile.PositiveTile)
                    {
                        Player.PositiveTileEffect();
                    }
                    else if (GraphicHandler.GetOrderTile(targetTileIndex).TileType == Tile.RandomTile)
                    {
                        int randomNumber = _random.Next(1, 3);
                        if (randomNumber == 1)
                        {
                            Player.PositiveTileEffect();
                        }
                        else
                        {
                            Player.RandomNegativeTile(boat);
                            SwitchPlayer();
                        }
                    }
                    else if (LastDiceRoll != 6 && moveAITick >= 60)
                    {                       
                        SwitchPlayer();
                    }
                    MainPage.showDice = true;
                }
            }
        }

        /// <summary>
        /// Recieves the tile index of current tile + dice roll and returns the vector of that tile index
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <returns></returns>
        private static Vector2 GetHighlightVector(int tileIndex)
        {
            int tileIndexBritain = LastPressedBoat.CurrentTile + LastDiceRoll - 44;
            int tileIndexDutch = LastPressedBoat.CurrentTile + LastDiceRoll - 10;
            int tileIndexSpain = LastPressedBoat.CurrentTile + LastDiceRoll - 21;
            int tileIndexFrance = LastPressedBoat.CurrentTile + LastDiceRoll - 32;
            Vector2 highlightVector;

            if (LastPressedBoat.active && LastPressedBoat.CurrentTile + LastDiceRoll > 43 && LastPressedBoat.Faction == Faction.Britain)
            {
                highlightVector = new Vector2(GraphicHandler.GetBritainGoalTile(tileIndexBritain).GameTileVector.X - 12,
                    GraphicHandler.GetBritainGoalTile(tileIndexBritain).GameTileVector.Y - 12);
                return highlightVector;
            }
            else if (LastPressedBoat.active && LastPressedBoat.CurrentTile <= 10
                && LastPressedBoat.CurrentTile + LastDiceRoll > 10 && LastPressedBoat.Faction == Faction.Dutch)
            {
                highlightVector = new Vector2(GraphicHandler.GetDutchGoalTile(tileIndexDutch).GameTileVector.X - 12,
                    GraphicHandler.GetDutchGoalTile(tileIndexDutch).GameTileVector.Y - 12);
                return highlightVector;
            }
            else if (LastPressedBoat.active && LastPressedBoat.CurrentTile <= 21
                && LastPressedBoat.CurrentTile + LastDiceRoll > 21 && LastPressedBoat.Faction == Faction.Spain)
            {
                highlightVector = new Vector2(GraphicHandler.GetSpainGoalTile(tileIndexSpain).GameTileVector.X - 12,
                    GraphicHandler.GetSpainGoalTile(tileIndexSpain).GameTileVector.Y - 12);
                return highlightVector;
            }
            else if (LastPressedBoat.active && LastPressedBoat.CurrentTile <= 32
                && LastPressedBoat.CurrentTile + LastDiceRoll > 32 && LastPressedBoat.Faction == Faction.France)
            {
                highlightVector = new Vector2(GraphicHandler.GetFranceGoalTile(tileIndexFrance).GameTileVector.X - 12,
                    GraphicHandler.GetSpainGoalTile(tileIndexFrance).GameTileVector.Y - 12);
                return highlightVector;
            }
            else
            {
                highlightVector = new Vector2(GraphicHandler.GetOrderTile(tileIndex).GameTileVector.X - 12, GraphicHandler.GetOrderTile(tileIndex).GameTileVector.Y - 12);
                return highlightVector;
            }

        }

        /// <summary>
        /// Takes in the latest mouse click coordinates in the form of a Vector2 value
        /// <para>Check if any ship is on that position of the screen</para>
        /// </summary>
        /// <param name="clickCords">X/Y coordinates of the latest mouse click on the game screen</param>
        /// <returns>Player.RowBoat object or null</returns>
        public Player.RowBoat CheckForShipsOnMousePressed(Vector2 clickCords)
        {
            if (gameActive)
            {
                if (LastPressedBoat != null)
                {
                    LastPressedBoat.pressedByMouse = false;
                    LastPressedBoat.targetable = true;
                }

                /// NYI: This should be refactored into a method as we repeat it 4 times
                if (p1.IsHuman)
                {
                    foreach (var ship in p1.rowBoats)
                    {
                        if (ship.targetable)
                        {
                            if (clickCords.X >= ship.scaledVector.X - 30 && clickCords.X <= ship.scaledVector.X + 30
                            && clickCords.Y >= ship.scaledVector.Y - 30 && clickCords.Y <= ship.scaledVector.Y + 30)
                            {
                                if (ship.pressedByMouse)
                                {
                                    ship.targetable = true;
                                    ship.pressedByMouse = false;
                                }
                                else
                                {
                                    ship.targetable = false;
                                    ship.pressedByMouse = true;
                                    LastPressedBoat = ship;
                                }
                                return ship;
                            }
                        }
                    }
                }
                else if (p2.IsHuman)
                {
                    foreach (var ship in p2.rowBoats)
                    {
                        if (ship.targetable)
                        {
                            if (clickCords.X >= ship.scaledVector.X - 30 && clickCords.X <= ship.scaledVector.X + 30
                            && clickCords.Y >= ship.scaledVector.Y - 30 && clickCords.Y <= ship.scaledVector.Y + 30)
                            {
                                if (ship.pressedByMouse)
                                {
                                    ship.targetable = true;
                                    ship.pressedByMouse = false;
                                }
                                else
                                {
                                    ship.targetable = false;
                                    ship.pressedByMouse = true;
                                    LastPressedBoat = ship;
                                }
                                return ship;
                            }
                        }
                    }
                }
                else if (p3.IsHuman)
                {
                    foreach (var ship in p3.rowBoats)
                    {
                        if (ship.targetable)
                        {
                            if (clickCords.X >= ship.scaledVector.X - 30 && clickCords.X <= ship.scaledVector.X + 30
                            && clickCords.Y >= ship.scaledVector.Y - 30 && clickCords.Y <= ship.scaledVector.Y + 30)
                            {
                                if (ship.pressedByMouse)
                                {
                                    ship.targetable = true;
                                    ship.pressedByMouse = false;
                                }
                                else
                                {
                                    ship.targetable = false;
                                    ship.pressedByMouse = true;
                                    LastPressedBoat = ship;
                                }
                                return ship;
                            }
                        }
                    }
                }
                else if (p4.IsHuman)
                {
                    foreach (var ship in p4.rowBoats)
                    {
                        if (ship.targetable)
                        {
                            if (clickCords.X >= ship.scaledVector.X - 30 && clickCords.X <= ship.scaledVector.X + 30
                            && clickCords.Y >= ship.scaledVector.Y - 30 && clickCords.Y <= ship.scaledVector.Y + 30)
                            {
                                if (ship.pressedByMouse)
                                {
                                    ship.targetable = true;
                                    ship.pressedByMouse = false;
                                }
                                else
                                {
                                    ship.targetable = false;
                                    ship.pressedByMouse = true;
                                    LastPressedBoat = ship;
                                }
                                return ship;
                            }
                        }
                    }
                }

            }
            return null;
        }

        /// <summary>
        /// Takes in the latest mouse click coordinates in the form of a Vector2 value
        /// <para>Check if any game tile is on that position of the screen</para>
        /// </summary>
        /// <param name="clickCords">X/Y coordinates of the latest mouse click on the game screen </param>
        /// <returns>GameTile object or null</returns>
        public GameTile CheckForTileOnMousePressed(Vector2 clickCords)
        {
            if (gameActive && playerCanMove)
            {
                foreach (var tile in GraphicHandler.GetOrderedTiles())
                {
                    if (tile.TileType != Tile.BaseTile)
                    {
                        if (clickCords.X >= tile.ScaledVector.X - 50 && clickCords.X <= tile.ScaledVector.X
                        && clickCords.Y >= tile.ScaledVector.Y - 50 && clickCords.Y <= tile.ScaledVector.Y)
                        {
                            LastPressedGameTile = tile;
                            return tile;
                        }
                    }
                }

                foreach (var tile in GraphicHandler.GetBritainGoalTiles())
                {
                    if (clickCords.X >= tile.ScaledVector.X - 50 && clickCords.X <= tile.ScaledVector.X
                    && clickCords.Y >= tile.ScaledVector.Y - 50 && clickCords.Y <= tile.ScaledVector.Y)
                    {
                        LastPressedGameTile = tile;
                        LastPressedBoat.isOnGoalTile = true;
                        return tile;
                    }
                }


            }
            return null;
        }

        public static bool GetGameActive()
        {
            return gameActive;
        }

        /// <summary>
        /// Simple method to check all the active <see cref="Player.playerList"/> count and determine if a player has won
        /// When a list comes back empty, that player has won.
        /// </summary>
        public static void CheckWin()
        {
            foreach (var player in Player.playerList)
            {
                if (player.rowBoats.Count == 0)
                {
                    MainPage mainpage = new MainPage();                    
                    mainpage.winnerPOP.IsOpen = true;
                    mainpage.WinnerTextBlock.Text=$"{player.playerFaction} won the game ";                   
                }
            }
        }

        /// <summary>
        /// Switch the current player playing
        /// <para>Checks what value <see cref="GameEngine.PlayerTurn"/> and change the current player</para>
        /// </summary>
        private static void SwitchPlayer()
        {
            // Clean up before switching player
            foreach (var boat in Player.targetableRowBoats)
            {
                boat.targetable = false;
            }
            Player.targetableRowBoats.Clear();
            LastPressedBoat = null;
            LastPressedGameTile = null;
            // Switch to next player clockwise
            switch (PlayerTurn)
            {
                case 1: PlayerTurn = 2; break;
                case 2: PlayerTurn = 3; break;
                case 3: PlayerTurn = 4; break;
                case 4: PlayerTurn = 1; newRound = true; break;
            }
        }
        public static Player GetActivePlayer()
        {
            return ActivePlayer;
        }
    }
}
