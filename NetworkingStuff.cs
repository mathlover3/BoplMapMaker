using Entwined;
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
        public static bool HasResetListsYet = false;
        public static int NumberOfZipsReceved = 0;
        public static int ZipId = 0;
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
                    UUIDs.Add(Convert.ToInt32(Meta["MapUUID"]));
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
                bytes.AddRange(BitConverter.GetBytes(ZipId));
                ZipId++;
                if (ZipId >= Plugin.zipArchives.Length)
                {
                    ZipId = 0;
                }
                return bytes.ToArray();
            }
        }
        public static ZipArchivePacket ByteArrayToZipArchive(byte[] byteArray)
        {
            List<byte> bytes = byteArray.ToList();
            bytes.RemoveRange(bytes.Count - 8, 8);
            byte[] data = bytes.ToArray();
            var memoryStream = new MemoryStream(data);
            var zipPacket = new ZipArchivePacket
            {
                zip = new ZipArchive(memoryStream, ZipArchiveMode.Read),
                length = BitConverter.ToInt32(byteArray, byteArray.Length - 8),
                id = BitConverter.ToInt32(byteArray, byteArray.Length - 4)
            };
            return zipPacket;
        }
        public static void OnZipArchive(ZipArchivePacket payload, PacketSourceInfo sourceInfo)
        {

            //if its the host we add them to the list of zip archives and do the rest of the initalison for them.
            if (SteamManager.instance.currentLobby.IsOwnedBy(sourceInfo.Identity.SteamId))
            {
                UnityEngine.Debug.Log($"number of zips to reseve is {payload.length}");
                UnityEngine.Debug.Log($"id is {payload.id}");
                if (!HasResetListsYet)
                {
                    Plugin.zipArchives = new ZipArchive[payload.length];
                    Plugin.MapJsons = new string[payload.length];
                    Plugin.MetaDataJsons = new string[payload.length];
                    HasResetListsYet = true;
                }
                Plugin.zipArchives[payload.id] = payload.zip;
                Plugin.MapJsons[payload.id] = Plugin.GetFileFromZipArchive(payload.zip, Plugin.IsBoplMap)[0];
                Plugin.MetaDataJsons[payload.id] = Plugin.GetFileFromZipArchive(payload.zip, Plugin.IsMetaDataFile)[0];
                NumberOfZipsReceved++;
                if (NumberOfZipsReceved == payload.length)
                {
                    HasResetListsYet = false;
                }
            }
        }
    }
    public struct ZipArchivePacket
    {
        public ZipArchive zip;
        public int length;
        public int id;
    }
}
