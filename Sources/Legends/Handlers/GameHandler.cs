﻿using Legends.Configurations;
using Legends.Core.Cryptography;
using Legends.Core.Geometry;
using Legends.Core.Protocol;
using Legends.Core.Protocol.Enum;
using Legends.Core.Protocol.Game;
using Legends.Core.Protocol.LoadingScreen;
using Legends.Core.Protocol.Messages.Game;
using Legends.Core.Protocol.Other;
using Legends.Network;
using Legends.World.Commands;
using Legends.World.Entities;
using Legends.World.Entities.AI;
using Legends.World.Entities.Movements;
using Legends.World.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Legends.Handlers
{
    class GameHandler
    {
        [MessageHandler(PacketCmd.PKT_C2S_Click, Channel.CHL_C2S)]
        public static void HandleClickMessage(ClickMessage message, LoLClient client)
        {
            var targetUnit = client.Hero.Game.Map.GetUnit(message.targetNetId);

            if (targetUnit != null)
            {
              //  string msg = "You clicked on {0} Position : {1} Distance to me : {2}";
               // client.Hero.DebugMessage(string.Format(msg, targetUnit.Name, targetUnit.Position, targetUnit.GetDistanceTo(client.Hero)));
            }
        }

        [MessageHandler(PacketCmd.PKT_C2S_StartGame)]
        public static void HandleStartGameRequestMessage(StartGameRequestMessage message, LoLClient client)
        {
            client.Hero.ReadyToStart = true;

            if (client.Hero.Game.CanStart) //  && ConfigurationProvider.Instance.Configuration.Players.Count() == client.Hero.Game.Players.Count() (wait all players to be connected)
            {
                client.Hero.Game.Start();
            }

        }
        [MessageHandler(PacketCmd.PKT_C2S_CharLoaded, Channel.CHL_C2S)]
        public static void HandleCharLoadedMessage(CharLoadedMessage message, LoLClient client)
        {
            client.Hero.ReadyToSpawn = true;

            client.Hero.NetId = client.Hero.Game.NetIdProvider.PopNextNetId();
            client.Hero.SpawnPosition = client.Hero.Game.Map.GetSpawnPosition(client.Hero);
            client.Hero.Position = client.Hero.SpawnPosition;

            if (client.Hero.Game.CanSpawn)
            {
                client.Hero.Game.Spawn();
            }

        }
        [MessageHandler(PacketCmd.PKT_C2S_ViewReq)]
        public static void HandleViewRequestMessage(ViewRequestMessage message, LoLClient client)
        {
            byte requestNo = 0x00;

            if (message.requestNo == 0xFE)
            {
                requestNo = 0xFF;
            }
            else
            {
                requestNo = message.requestNo;
            }

            client.Send(new ViewMessage(requestNo));
        }

        [MessageHandler(PacketCmd.PKT_C2S_LockCamera)]
        public static void HandleLockCameraRequestMessage(LockCameraRequestMessage message, LoLClient client)
        {

        }
        [MessageHandler(PacketCmd.PKT_C2S_MoveConfirm, Channel.CHL_LOW_PRIORITY)]
        public static void HandleMovementConfirmationMessage(MovementConfirmationMessage message, LoLClient client)
        {

        }
        [MessageHandler(PacketCmd.PKT_C2S_SkillUp)]
        public static void HandleSkillUpRequestMessage(SkillUpRequestMessage message, LoLClient client)
        {

        }
        [MessageHandler(PacketCmd.PKT_C2S_MoveReq)]
        public static void HandleMovementRequestMessage(MovementRequestMessage message, LoLClient client)
        {
            switch (message.type)
            {
                case MovementType.EMOTE:
                    break;
                case MovementType.MOVE:

                    WaypointsReader wayPointsReader = new WaypointsReader(message.moveData, message.coordCount, client.Hero.Game.Map.Size);
                    // the client delay lead to display problems so we secure the first waypoint.
                    wayPointsReader.Waypoints[0] = client.Hero.Position;
                    client.Hero.Move(wayPointsReader.Waypoints);


                    break;
                case MovementType.ATTACK:

                    // ThreadSafe important !! 
                    var target = (AIUnit)client.Hero.Game.Map.GetUnit(message.targetNetId);

                    if (target == null)
                    {
                        client.Hero.DebugMessage("Unable to autoattack, target is null");
                        return;
                    }

                    client.Hero.TryAutoattack(target);


                    break;
                case MovementType.ATTACKMOVE:
                    break;
                case MovementType.STOP:

                    break;
                default:
                    break;




            }

        }
    }
}
