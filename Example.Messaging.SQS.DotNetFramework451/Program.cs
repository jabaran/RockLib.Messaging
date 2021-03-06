﻿using System;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using RockLib.Messaging;
using System.Threading;

namespace Example.Messaging.SQS.DotNetFramework451
{
    class Program
    {
        static void Main(string[] args)
        {
            // The AWS credentials should be provide via a profile or other more secure means. This is only for example.
            // See http://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html
            var options = new CredentialProfileOptions
            {
                AccessKey = "AKIAJWXCD7ZCUGRQYCMA",
                SecretKey = "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"
            };
            var profile = new CredentialProfile("default", options) { Region = RegionEndpoint.USWest2 };
            new NetSDKCredentialsFile().RegisterProfile(profile);

            Console.WriteLine("Make a selection:");
            Console.WriteLine("1) Create an ISender and prompt for messages.");
            Console.WriteLine("2) Create an IReceiver and listen for messages.");
            Console.WriteLine("3) Create an ISender and an IReceiver and send one message from one to the other.");
            Console.WriteLine("q) Quit");
            Console.WriteLine("Tip: Start multiple instances of this app and have them send and receive to each other.");
            Console.Write("selection>");

            while (true)
            {
                var c = Console.ReadKey(true).KeyChar;
                switch (c)
                {
                    case '1':
                        Console.WriteLine(c);
                        RunSender();
                        return;
                    case '2':
                        Console.WriteLine(c);
                        RunReceiver();
                        return;
                    case '3':
                        Console.WriteLine(c);
                        SendAndReceiveOneMessage();
                        return;
                    case 'q':
                        Console.WriteLine(c);
                        return;
                }
            }
        }

        private static void RunSender()
        {
            using (var sender = MessagingScenarioFactory.CreateQueueProducer("queue"))
            {
                Console.WriteLine($"Enter a message for sender '{sender.Name}'. Leave blank to quit.");
                string message;
                while (true)
                {
                    Console.Write("message>");
                    if ((message = Console.ReadLine()) == "")
                        return;
                    sender.Send(message);
                }
            }
        }

        private static void RunReceiver()
        {
            using (var receiver = MessagingScenarioFactory.CreateQueueConsumer("queue"))
            {
                receiver.MessageReceived += (s, e) => Console.WriteLine(e.Message.GetStringValue());
                Console.WriteLine($"Receiving messages from receiver '{receiver.Name}'. Press <enter> to quit.");
                receiver.Start();
                while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
            }
        }

        private static void SendAndReceiveOneMessage()
        {
            // Use a wait handle to pause the main thread while waiting for the message to be received.
            var waitHandle = new AutoResetEvent(false);

            var producer = MessagingScenarioFactory.CreateQueueProducer("queue");
            var consumer = MessagingScenarioFactory.CreateQueueConsumer("queue");

            consumer.MessageReceived += (sender, eventArgs) =>
            {
                var eventArgsMessage = eventArgs.Message;
                var message = eventArgsMessage.GetStringValue();

                Console.WriteLine($"Message received: {message}");

                waitHandle.Set();
            };
            consumer.Start();

            producer.Send($"SQS test message from {typeof(Program).FullName}");

            waitHandle.WaitOne();

            consumer.Dispose();
            producer.Dispose();
            waitHandle.Dispose();

            Console.Write("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}