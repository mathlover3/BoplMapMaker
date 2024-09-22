﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using UnityEngine;
using System.IO.Compression;
using System.Threading;

namespace MapMaker
{
    internal class PipeStuff
    {

        public class PipeResponder
        {
            public static int TotalLength = 0;
            public static int LengthCompleted = 0;
            public static List<byte> bytes = new();
            public static CancellationTokenSource _cancellationTokenSource;
            public void StartPipe()
            {
                // Using Task.Run to avoid blocking the main thread
                //thanks to chatgpt for telling me about the CancellationToken so it doesnt go on forever and keep the game from closing
                _cancellationTokenSource = new CancellationTokenSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => StartPipeReal(_cancellationTokenSource.Token));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            //based on https://stackoverflow.com/questions/46793391/how-to-wait-for-response-from-namedpipeserver
            private async void StartPipeReal(CancellationToken cancellationToken)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
    "testpipe",
    PipeDirection.InOut,
    1,
    PipeTransmissionMode.Byte))//Set TransmissionMode to Message
                {
                    pipeServer.ReadMode = PipeTransmissionMode.Byte;
                    //part chatgpt
                    // Wait for a client to connect asynchronously, with cancellation support
                    Debug.Log("Waiting for client connection...");
                    try
                    {
                        await pipeServer.WaitForConnectionAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.Log("Pipe connection was canceled.");
                        return; // Exit if canceled
                    }

                    Console.WriteLine("Client connected.");

                    //receive message from client
                    while (pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        var messageBytes = ReadMessage(pipeServer);
                        if (messageBytes != null)
                        {
                            Console.WriteLine("Map received from client, length: " + messageBytes.Length);
                            var memoryStream = new MemoryStream(messageBytes);
                            var zip = new ZipArchive(memoryStream, ZipArchiveMode.Read);
                            TestMap(zip);
                        }
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        //start the pipe agien
                        Debug.Log("restarting pipe");
                        //this is already on a difrent theread so no need to make a new thread.
                        StartPipeReal(cancellationToken);
                    }
                }
            }
            public static void TestMap(ZipArchive zip)
            {
                Debug.Log("testing map");
                //leave any online games
                GameSessionHandler.LeaveGame(true, false);
                //set all the sizes to 1
                Plugin.zipArchives = new ZipArchive[1];
                Plugin.MapJsons = new string[1];
                Plugin.MetaDataJsons = new string[1];
                Plugin.zipArchives[0] = zip;
                Plugin.MapJsons[0] = Plugin.GetFileFromZipArchive(zip, Plugin.IsBoplMap)[0];
                Plugin.IsInTestMode = true;
                Plugin.MetaDataJsons[0] = Plugin.GetFileFromZipArchive(zip, Plugin.IsMetaDataFile)[0];
            }
            private static byte[] ReadMessage(PipeStream pipe)
            {
                byte[] lengthBuff = new byte[4];
                int lengthBuffLength = 0;
                if (TotalLength == LengthCompleted)
                {
                    //copy it so we can still return it after clearing it
                    lengthBuffLength = pipe.Read(lengthBuff, 0, 4);
                }
                //mesage length TODO fix this so it doesnt error if theres less then 4 bytes and instead saves it for next time.
                if (lengthBuffLength != 0)
                {
                    Console.WriteLine("reading new Length");
                    int Length = BitConverter.ToInt32(lengthBuff, 0);
                    TotalLength = Length;
                    LengthCompleted = 0;
                }
                if (LengthCompleted < TotalLength)
                {
                    Console.WriteLine("reading data");
                    byte[] buff = new byte[TotalLength - LengthCompleted];
                    var newDataLength = pipe.Read(buff, 0, TotalLength - LengthCompleted);
                    bytes.AddRange(buff.ToList().GetRange(0, newDataLength));
                    LengthCompleted += newDataLength;
                }
                if (TotalLength == LengthCompleted && LengthCompleted != 0)
                {
                    //copy it so we can still return it after clearing it
                    byte[] bytes2 = new byte[LengthCompleted];
                    bytes.CopyTo(bytes2, 0);
                    bytes = new();
                    return bytes2;
                }
                return null; 

                
            }
        }
    }
}