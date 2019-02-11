using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;

namespace WaterMod
{
    static class NetworkHandler
    {
        static UnityEngine.Networking.NetworkInstanceId Host;
        static bool HostExists = false;

        const TTMsgType WaterChange = (TTMsgType)228;

        private static float serverWaterHeight = -1000f;

        public static float ServerWaterHeight
        {
            get { return serverWaterHeight; }
            set
            {
                serverWaterHeight = value;
                TryBroadcastNewHeight(serverWaterHeight);
            }
        }

        public class WaterChangeMessage : UnityEngine.Networking.MessageBase
        {
            public WaterChangeMessage() { }
            public WaterChangeMessage(float Height)
            {
                this.Height = Height;
            }
            public override void Deserialize(UnityEngine.Networking.NetworkReader reader)
            {
                this.Height = reader.ReadSingle();
            }

            public override void Serialize(UnityEngine.Networking.NetworkWriter writer)
            {
                writer.Write(this.Height);
            }

            public float Height;
        }

        public static void TryBroadcastNewHeight(float Water)
        {
            if (HostExists) try
                {
                    Singleton.Manager<ManNetwork>.inst.SendToAllClients(WaterChange, new WaterChangeMessage(Water), Host);
                    Console.WriteLine("Sent new water level");
                }
                catch { Console.WriteLine("Failed to send new water level"); }
        }

        public static void OnClientChangeWaterHeight(UnityEngine.Networking.NetworkMessage netMsg)
        {
            var reader = new WaterChangeMessage();
            netMsg.ReadMessage(reader);
            serverWaterHeight = reader.Height;
            Console.WriteLine("Received new water level, changing to " + serverWaterHeight.ToString());
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(NetPlayer), "OnRecycle")]
            static class OnRecycle
            {
                static void Postfix(NetPlayer __instance)
                {
                    if (__instance.isServer || __instance.isLocalPlayer)
                    {
                        serverWaterHeight = -1000f;
                        Console.WriteLine("Discarded " + __instance.netId.ToString() + " and reset server water level");
                        HostExists = false;
                    }
                }
            }

            [HarmonyPatch(typeof(ManNetwork),"AddPlayer")]
            static class OnStartClient
            {
                static void Posfix(NetPlayer __instance)
                {
                    Singleton.Manager<ManNetwork>.inst.SubscribeToClientMessage(__instance.netId, WaterChange, new ManNetwork.MessageHandler(OnClientChangeWaterHeight));
                    Console.WriteLine("Subscribed " + __instance.netId.ToString() + " to water level updates from host");
                }
            }

            [HarmonyPatch(typeof(NetPlayer), "OnStartServer")]
            static class OnStartServer
            {
                static void Postfix(NetPlayer __instance)
                {
                    if (__instance.isServer || __instance.isLocalPlayer)
                        serverWaterHeight = -1000f;
                    //Singleton.Manager<ManNetwork>.inst.SubscribeToServerMessage(__instance.netId, WaterChange, new ManNetwork.MessageHandler(OnServerChangeWaterHeight));
                    Console.WriteLine("Host started, hooked water level broadcasting to " + __instance.netId.ToString());
                    Host = __instance.netId;
                    HostExists = true;
                }
            }
        }
    }
}
