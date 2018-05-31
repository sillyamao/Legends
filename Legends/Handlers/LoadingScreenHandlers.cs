﻿using Legends.Configurations;
using Legends.Core.Cryptography;
using Legends.Core.Protocol;
using Legends.Core.Protocol.Enum;
using Legends.Core.Protocol.Game;
using Legends.Core.Protocol.LoadingScreen;
using Legends.Core.Protocol.Other;
using Legends.Core.Utils;
using Legends.Network;
using Legends.World;
using Legends.World.Entities;
using Legends.World.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Legends.Handlers
{
    unsafe class LoadingScreenHandlers
    {
        public static byte[] PartialKey = new byte[3]
        {
            0x2A,
            0,
            0xFF
        };
        private static Logger logger = new Logger();

        [MessageHandler(PacketCmd.PKT_KeyCheck, Channel.CHL_C2S)]
        public static void HandleKeyCheckMessage(KeyCheckMessage message, LoLClient client)
        {
            long userId = (long)BlowFishCS.Decrypt2(LoLServer.GetBlowfish(), (ulong)message.checkId);

            logger.Write("User (" + userId + ") connected!");

            if (userId != message.userId)
            {
                return;
            }
            client.UserId = userId;

            PlayerData datas = ConfigurationManager.Instance.GetPlayerData(userId);

            if (datas == null)
            {
                logger.Write("No player data for userId:" + userId, MessageState.WARNING);
                return;
            }

            Game targetGame = GamesManager.TestGame;

            if (targetGame.Contains(userId))
            {
                logger.Write(userId + "try to connect a second time!", MessageState.WARNING);
                return;
            }
            client.DefinePlayer(new Player(client, datas));

            client.Player.DefineGame(targetGame);

            client.Player.Game.AddUnit(client.Player, client.Player.Data.TeamId);

            client.Player.PlayerNo = client.Player.Game.PopNextPlayerNo();

            // on montre aux joueurs de la partie que je suis la !
            client.Player.Game.Send(new KeyCheckMessage(PartialKey, client.Player.PlayerNo, userId, 0, 0, 0), Channel.CHL_HANDSHAKE);

            // on montre a moi les autre joueurs de la partie
            foreach (var player in client.Player.Game.Players)
            {
                client.Send(new KeyCheckMessage(PartialKey, player.PlayerNo, player.Client.UserId.Value, 0, 0, 0), Channel.CHL_HANDSHAKE);
            }

            // useless??
            client.Send(new WorldGameNumberMessage(client.Player.Game.Id, client.Player.Game.Name), Channel.CHL_S2C);

            // blowfish >= 8 
            client.Encrypt = true;
        }

        [MessageHandler(PacketCmd.PKT_C2S_QueryStatusReq)]
        public static void HandleQueryStatusRequest(QueryStatusRequestMessage message, LoLClient client)
        {
            client.Send(new QueryStatusAnswerMessage(0, 1), Channel.CHL_S2C);
        }

        [MessageHandler(PacketCmd.PKT_C2S_SynchVersion)]
        public static void HandleSynchVersionMessage(SynchVersionMessage message, LoLClient client)
        {
            string version = Crypto.GetVersion(message.version);

            if (version != LoLServer.CLIENT_REQUIRED_VERSION)
            {
                client.Disconnect();
            }
            else
            {
                var infos = ConfigurationManager.Instance.GetPlayersInformations();
                client.Send(new SynchVersionAnswerMessage(0, 1, client.Player.Game.Map.Id, infos,
                LoLServer.CLIENT_REQUIRED_VERSION, "CLASSIC", "NA1", 487826), Channel.CHL_S2C);
            }
        }
        [MessageHandler(PacketCmd.PKT_C2S_ClientReady, Channel.CHL_C2S)]
        public static void HandleClientReadyMessage(ClientReadyMessage message, LoLClient client)
        {
            client.Ready = true;

            client.Send(new LoadScreenInfoMessage(6, 6, ConfigurationManager.Instance.GetBlueIds(), ConfigurationManager.Instance.GetPurpleIds()), Channel.CHL_LOADING_SCREEN);

            foreach (var player in ConfigurationManager.Instance.Configuration.Players)
            {
                client.Send(new LoadScreenPlayerNameMessage(player.UserId, player.SkinId, player.Name, 0), Channel.CHL_LOADING_SCREEN);
                client.Send(new LoadScreenPlayerChampionMessage(player.UserId, player.SkinId, player.ChampionName, 0), Channel.CHL_LOADING_SCREEN);
            }


        }


        [MessageHandler(PacketCmd.PKT_C2S_Ping_Load_Info, Channel.CHL_C2S)]
        public static void HandlePingLoadInfoMessage(PingLoadInfoMessage message, LoLClient client)
        {
            client.Player.Game.Send(new PingLoadInfoAnswerMessage()
            {
                netId = message.netId,
                loaded = message.loaded,
                ping = message.ping,
                unk1 = message.unk1, // 0 que Yasuo, 1 que Riven, playerId?
                unk2 = message.unk2,
                unk3 = message.unk3,
                unk4 = message.unk4,
                userId = client.UserId.Value,
            }, Channel.CHL_LOW_PRIORITY, ENet.PacketFlags.None);
        }
        [MessageHandler(PacketCmd.PKT_C2S_Exit)]
        public static void HandleExit(ExitMessage message, LoLClient client)
        {
            client.OnDisconnect();
        }
    }
}
