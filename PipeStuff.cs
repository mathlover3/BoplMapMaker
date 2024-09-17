using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using UnityEngine;

namespace MapMaker
{
    internal class PipeStuff
    {

        public class PipeResponder
        {
            public async void StartPipe()
            {
                // Using Task.Run to avoid blocking the main thread
                await Task.Run(() => StartPipeReal());
            }
            //based on https://stackoverflow.com/questions/46793391/how-to-wait-for-response-from-namedpipeserver
            private void StartPipeReal()
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
    "testpipe",
    PipeDirection.InOut,
    1,
    PipeTransmissionMode.Byte))//Set TransmissionMode to Message
                {
                    pipeServer.ReadMode = PipeTransmissionMode.Byte;
                    // Wait for a client to connect
                    Console.Write("Waiting for client connection...");
                    pipeServer.WaitForConnection();

                    Console.WriteLine("Client connected.");

                    //receive message from client
                    while (pipeServer.IsConnected)
                    {
                        var messageBytes = ReadMessage(pipeServer);
                        if (messageBytes != null)
                        {
                            Console.WriteLine("Message received from client: " + Encoding.UTF8.GetString(messageBytes));
                        }
                    }
                    //start the pipe agien
                    StartPipe();
                }
            }
            private static byte[] ReadMessage(PipeStream pipe)
            {
                byte[] lengthBuff = new byte[4];
                
                var lengthBuffLength = pipe.Read(lengthBuff, 0, 4);
                //mesage length
                if (lengthBuffLength >= 4)
                {
                    Console.WriteLine("reading message");
                    int Length = BitConverter.ToInt32(lengthBuff, 0);
                    byte[] DataBuff = new byte[Length];
                    var readBytes = pipe.Read(DataBuff, 0, Length);
                    return DataBuff;
                }
                return null; 

                
            }
        }
    }
}
