using Entwined;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    public static class NetworkingStuff
    {
        //networking stuff
        public static EntwinedPacketChannel<int[]> MapUUIDsChannel;
        public static List<int> MapUUIDsReceved = new();
        public static void Awake()
        {
            //networking stuff
            var entwiner = new GenericEntwiner<int[]>(IntArrayToBytes, ByteArrayToInts);

            
            MapUUIDsChannel = new EntwinedPacketChannel<int[]>(Plugin.instance, entwiner);
            MapUUIDsChannel.OnMessage += OnMessage;
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
            for (int i = 0; i < bytes.Length; i = i + 4)
            {
                ints.Add(BitConverter.ToInt32(bytes, i));
            }
            return ints.ToArray();
        }
        public static void OnMessage(int[] payload, PacketSourceInfo sourceInfo)
        {
            UnityEngine.Debug.Log("packet reseved!");
        }
    }
}
