using Entwined;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Tango;

namespace MapMaker
{
    public static class NetworkingStuff
    {
        //networking stuff
        public static EntwinedPacketChannel<int[]> MapUUIDsChannel;
        public static List<int> MapUUIDsReceved = new();
        public static EntwinedPacketChannel<bool> DoWeHaveDifrentMapIdsChannel;
        public static EntwinedPacketChannel<ZipArchivePacket> ZipChannel;
        public static EntwinedPacketChannel<BetterStartRequestPacket> StartChannel;
        //used to know if the ZipArchivePacket was receved so that if it wasnt we can send it agien. this is the id of the zip that was receved.
        //make sure everyone receves it.
        public static EntwinedPacketChannel<int> ZipRecevedChannel;
        public static int MilisecondsToDelayBeforeResendingZip;
        public static Dictionary<SteamId, bool> HasRecevedLatestZip = new();
        public static float MilisecondsSenceLastZipSend = 0;
        public static void Awake()
        {
            //networking stuff
            var entwinerIntArray = new GenericEntwiner<int[]>(IntArrayToBytes, ByteArrayToInts);
            MapUUIDsChannel = new EntwinedPacketChannel<int[]>(Plugin.instance, entwinerIntArray);
            MapUUIDsChannel.OnMessage += OnUUIDs;
            DoWeHaveDifrentMapIdsChannel = new EntwinedPacketChannel<bool>(Plugin.instance, new BoolEntwiner());
            DoWeHaveDifrentMapIdsChannel.OnMessage += OnDifrentMapIds;
            var entwinerZip = new GenericEntwiner<ZipArchivePacket>(ZipArchiveToByteArray, ByteArrayToZipArchive);
            ZipChannel = new EntwinedPacketChannel<ZipArchivePacket>(Plugin.instance, entwinerZip);
            ZipChannel.OnMessage += OnZipArchive;
            var entwinerStart = new GenericEntwiner<BetterStartRequestPacket>(BetterStartRequestPacketToByteArray, ByteArrayToBetterStartRequestPacket);
            StartChannel = new EntwinedPacketChannel<BetterStartRequestPacket>(Plugin.instance, entwinerStart);
            StartChannel.OnMessage += OnStartPacket;
            ZipRecevedChannel = new EntwinedPacketChannel<int>(Plugin.instance, new IntEntwiner());
            ZipRecevedChannel.OnMessage += OnZipRecevedConfermed;
        }
        public static void Update()
        {
            var delta = (Time.deltaTime * 1000f);
            MilisecondsSenceLastZipSend = MilisecondsSenceLastZipSend + delta;
            if (MilisecondsSenceLastZipSend > MilisecondsToDelayBeforeResendingZip && HasRecevedLatestZip.Count < SteamManager.instance.connectedPlayers.Count && SteamManager.LocalPlayerIsLobbyOwner)
            {
                //resend the zip
                MilisecondsSenceLastZipSend = 0;
                ZipArchivePacket zipArchivePacket = new ZipArchivePacket
                {
                    zip = Plugin.MyZipArchives[Plugin.NextMapIndex],
                    length = Plugin.MyZipArchives.Length,
                    id = Plugin.NextMapIndex
                };
                ZipChannel.SendMessage(zipArchivePacket);
                //set all this stuff
                MilisecondsToDelayBeforeResendingZip = NetworkingStuff.GetDelayForResendingZip();
                UnityEngine.Debug.LogWarning("map file appears to have been droped, resending");
            }
        }
        public static byte[] IntArrayToBytes(int[] array)
        {
            List<byte> bytes = new List<byte>();
            foreach (int i in array)
            {
                bytes.AddRange(BitConverter.GetBytes(i));
            }
            return bytes.ToArray();
        }
        public static int[] ByteArrayToInts(byte[] bytes)
        {
            List<int> ints = new();
            for (int i = 0; i < bytes.Length - 3; i = i + 4)
            {
                //https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/how-to-convert-a-byte-array-to-an-int
                // If the system architecture is little-endian (that is, little end first),
                // reverse the byte array.
                byte[] bytes2 = { bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3] };
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes2);
                }
                ints.Add(BitConverter.ToInt32(bytes2, 0));
            }
            return ints.ToArray();
        }
        public static void OnUUIDs(int[] payload, PacketSourceInfo sourceInfo)
        {
            if (SteamManager.instance.currentLobby.IsOwnedBy(sourceInfo.Identity.SteamId))
            {
                UnityEngine.Debug.Log("reseved packet");
                List<int> UUIDs = new List<int>();
                foreach (string json in Plugin.MetaDataJsons)
                {
                    Dictionary<string, object> Meta = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
                    if (Meta.ContainsKey("MapUUID"))
                    {
                        UUIDs.Add(Convert.ToInt32(Meta["MapUUID"]));
                    }
                }
                if (UUIDs.ToArray() != payload)
                {
                    UnityEngine.Debug.LogError("ERROR WE HAVE DIFRENT MAP IDS/MAPS IN DIFRENT ORDERS!");
                    Plugin.zipArchives = new ZipArchive[0];
                    Plugin.MapJsons = new string[0];
                    Plugin.MetaDataJsons = new string[0];
                    DoWeHaveDifrentMapIdsChannel.SendMessage(true);
                }
                MapUUIDsReceved = UUIDs;
            }
        }
        public static void OnDifrentMapIds(bool payload,  PacketSourceInfo sourceInfo)
        {
            if (SteamManager.LocalPlayerIsLobbyOwner)
            {
                var i = 0;
                foreach (ZipArchive archive in Plugin.zipArchives)
                {
                    var zipPacket = new ZipArchivePacket
                    {
                        zip = archive,
                        length = Plugin.zipArchives.Length,
                        id = i
                    };
                    ZipChannel.SendMessage(zipPacket);
                    i++;
                }
            }
        }

        
        public static byte[] ZipArchiveToByteArray(ZipArchivePacket archive)
        {
            //chatgpt code
            using (var memoryStream = new MemoryStream())
            {
                // Create a new ZipArchive in the MemoryStream
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var entry in archive.zip.Entries)
                    {
                        var newEntry = zipArchive.CreateEntry(entry.FullName);
                        using (var entryStream = entry.Open())
                        using (var newEntryStream = newEntry.Open())
                        {
                            entryStream.CopyTo(newEntryStream);
                        }
                    }
                }
                //my code
                var bytes = memoryStream.ToArray().ToList();
                bytes.AddRange(BitConverter.GetBytes(Plugin.zipArchives.Length));
                bytes.AddRange(BitConverter.GetBytes(archive.id));
                return bytes.ToArray();
            }
        }
        public static ZipArchivePacket ByteArrayToZipArchive(byte[] byteArray)
        {
            List<byte> bytes = byteArray.ToList();
            bytes.RemoveRange(bytes.Count - 8, 8);
            byte[] data = bytes.ToArray();
            var memoryStream = new MemoryStream(data);
            //https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/how-to-convert-a-byte-array-to-an-int
            // If the system architecture is little-endian (that is, little end first),
            // reverse the byte array.
            byte[] bytes2 = { byteArray[byteArray.Length - 8], byteArray[byteArray.Length - 7], byteArray[byteArray.Length - 6], byteArray[byteArray.Length - 5] };
            byte[] bytes3 = { byteArray[byteArray.Length - 4], byteArray[byteArray.Length - 3], byteArray[byteArray.Length - 2], byteArray[byteArray.Length - 1] };
            //idk why but the microsoft exsample is backwords?
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes2);
                Array.Reverse(bytes3);
            }
            var zipPacket = new ZipArchivePacket
            {
                zip = new ZipArchive(memoryStream, ZipArchiveMode.Read),

                length = BitConverter.ToInt32(bytes2, 0),
                id = BitConverter.ToInt32(bytes3, 0)
            };
            return zipPacket;
        }
        public static void OnZipArchive(ZipArchivePacket payload, PacketSourceInfo sourceInfo)
        {

            //if its the host we add them to the list of zip archives and do the rest of the initalison for them.
            if (SteamManager.instance.currentLobby.IsOwnedBy(sourceInfo.Identity.SteamId))
            {
                UnityEngine.Debug.Log($"length of zip array is {payload.length}");
                UnityEngine.Debug.Log($"id is {payload.id}");
                if (Plugin.zipArchives.Length < payload.length)
                {
                    Plugin.zipArchives = new ZipArchive[payload.length];
                    Plugin.MapJsons = new string[payload.length];
                    Plugin.MetaDataJsons = new string[payload.length];
                }
                Plugin.zipArchives[payload.id] = payload.zip;
                Plugin.MapJsons[payload.id] = Plugin.GetFileFromZipArchive(payload.zip, Plugin.IsBoplMap)[0];
                Plugin.MetaDataJsons[payload.id] = Plugin.GetFileFromZipArchive(payload.zip, Plugin.IsMetaDataFile)[0];
                //conferm we got the packet
                ZipRecevedChannel.SendMessage(payload.id);
            }
        }

        public static byte[] BetterStartRequestPacketToByteArray(BetterStartRequestPacket BetterStartRequest)
        {
            StartRequestPacket startRequest = BetterStartRequest.startRequest;
            byte[] buff = new byte[105];
            NetworkTools.EncodeStartRequest(ref buff, startRequest);
            List<byte> packet = buff.ToList();
            packet.AddRange(BitConverter.GetBytes(BetterStartRequest.MapIndex));
            return packet.ToArray();
        }
        public static BetterStartRequestPacket ByteArrayToBetterStartRequestPacket(byte[] bytes)
        {
            List<byte> bytes2 = bytes.ToList();
            bytes2.RemoveRange(bytes2.Count - 4, 4);
            var uintConversionArray = new byte[4];
            var ulongConversionArray = new byte[8];
            var ushortConversionArray = new byte[8];
            StartRequestPacket startRequest = NetworkTools.ReadStartRequest(bytes2.ToArray(), ref uintConversionArray, ref ulongConversionArray, ref ushortConversionArray);
            byte[] bytes3 = { bytes[bytes.Length - 4], bytes[bytes.Length - 3], bytes[bytes.Length - 2], bytes[bytes.Length - 1] };
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes3);
            }
            var MapIndex = BitConverter.ToInt32(bytes3, 0);
            UnityEngine.Debug.Log($"MapIndex is {MapIndex}");
            var betterStartRequestPacket = new BetterStartRequestPacket
            {
                startRequest = startRequest,
                MapIndex = MapIndex
            };
            return betterStartRequestPacket;
        }
        public static void OnStartPacket(BetterStartRequestPacket payload, PacketSourceInfo sourceInfo)
        {
            if (SteamManager.instance.currentLobby.IsOwnedBy(sourceInfo.Identity.SteamId))
            {
                SteamManager.startParameters = payload.startRequest;
                Plugin.CurrentMapIndex = payload.MapIndex;
                //its max exsclusive min inclusinve
                if (Plugin.MapJsons.Length != 0)
                {
                    UnityEngine.Debug.Log($"we have {Plugin.MapJsons.Length} maps");
                    UnityEngine.Debug.Log($"map index is {Plugin.CurrentMapIndex}");
                    Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                    var type = Convert.ToString(MetaData["MapType"]);
                    UnityEngine.Debug.Log("getting map type");
                    switch (type)
                    {
                        case "space":
                            GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                            break;
                        case "snow":
                            GameSession.currentLevel = (byte)Plugin.SnowMapId;
                            break;
                        default:
                            GameSession.currentLevel = (byte)Plugin.GrassMapId;
                            break;
                    }
                    var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                    Plugin.CurrentMapUUID = UUID;
                }
                if (SteamManager.instance.currentLobby.IsOwnedBy(sourceInfo.Identity.SteamId))
                {
                    SteamManager.instance.EncodeCurrentStartParameters_forReplay(ref SteamManager.instance.networkClient.EncodedStartRequest, SteamManager.startParameters, false);
                    UnityEngine.Debug.Log(string.Concat(new object[]
                    {
                "FORCESTARTGAME: connectedPlayerCount = ",
                SteamManager.instance.connectedPlayers.Count,
                " packet from ",
                sourceInfo.Identity.SteamId
                    }));
                    if (GameSession.inMenus)
                    {
                        CharacterSelectHandler_online.ForceStartGame(null);
                        return;
                    }
                    SteamManager.ForceLoadNextLevel();
                    return;
                }
            }
        }
        public static void OnZipRecevedConfermed(int payload, PacketSourceInfo sourceInfo)
        {
            if (payload == Plugin.NextMapIndex)
            {
                HasRecevedLatestZip[sourceInfo.SenderSteamId] = true;
            }
        }
        public static int GetDelayForResendingZip()
        {
            float maxping = 0;
            foreach (var connectson in SteamManager.instance.connectedPlayers)
            {
                if (connectson.ping > maxping)
                {
                    maxping = connectson.ping;
                }
            }
            UnityEngine.Debug.Log(maxping);
            //the ping in milliseconds + 250 miliseconds of leway
            return (int)(maxping * 1000f) + 250;
        }
    }
    public struct ZipArchivePacket
    {
        public ZipArchive zip;
        public int length;
        public int id;
    }
    public struct BetterStartRequestPacket
    {
        public StartRequestPacket startRequest;
        public int MapIndex;
    }
}
