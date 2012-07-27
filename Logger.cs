//---------------------------------------------------------------------------------------------------------------------------- 
// <copyright company="Microsoft Corporation"> 
//  Copyright 2012 Microsoft Corporation 
// </copyright> 
// Licensed under the MICROSOFT LIMITED PUBLIC LICENSE version 1.1 (the "License");  
// You may not use this file except in compliance with the License.  
//--------------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using System.Diagnostics;
using System.IO;

namespace Contoso.Logging
{
    public class Logger
    {
        const string QUEUE_NAME = "Responses";

        CloudStorageAccount _account;
        
        public Logger(CloudStorageAccount account)
        {
            _account = account;
        }


        public void Log(string message)
        {
            try
            {
                writeLog(message);
                Console.WriteLine("Log:" + message);
                var queue = GetQueue(QUEUE_NAME);
                CloudQueueMessage cloudMessage = new CloudQueueMessage(message);
                queue.AddMessage(cloudMessage);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("from logger: " + ex.Message, "Error");
            }
        }

        public void Log(string message, Exception exception)
        {
            Log(message + " with exception: " + exception.Message);
        }

        public List<Message> GetMessages()
        {
            var rv = new List<Message>();
            var queue = GetQueue(QUEUE_NAME);
            foreach (var item in queue.GetMessages(20))
            {
                rv.Add(new Message()
                {
                    Contents = item.AsString,
                    Id = item.Id,
                    TimeStamp = item.InsertionTime
                });
                queue.DeleteMessage(item);
            }
            return rv;
        }
        
        
        CloudQueueClient GetQueueClient()
        {
            CloudQueueClient client = CloudStorageAccountStorageClientExtensions.CreateCloudQueueClient(_account);
            return client;
        }

        CloudQueue GetQueue(string queueName)
        {
            var client = GetQueueClient();
            var queue = client.GetQueueReference(queueName.ToLower());
            queue.CreateIfNotExist();
            return queue;

        }

        private void writeLog(string result)
        {
            StreamWriter log;
            if (!Directory.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "//Log//"))
                Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + "//Log//");

            if (!File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "//Log//" + DateTime.Today.ToString("MMddyyyy") + ".txt"))
                log = new StreamWriter(System.AppDomain.CurrentDomain.BaseDirectory + "//Log//" + DateTime.Today.ToString("MMddyyyy") + ".txt");
            else
                log = File.AppendText(System.AppDomain.CurrentDomain.BaseDirectory + "//Log//" + DateTime.Today.ToString("MMddyyyy") + ".txt");
            using (log)
            {
                log.WriteLine(result);
            }
        }
    }

    public class Message
    {
        public DateTime? TimeStamp { get; set; }
        public string Id { get; set; }
        public string Contents { get; set; }

    }

}


