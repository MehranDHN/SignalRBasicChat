using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace SignalRBasicChat.RealTime
{
    public class LiveChatHub : Hub
    {
        // I prefer to use ConcurrentDictionary over Dictionary because it's designed for multithreaded scenarios. 
        // and I don't have to use locks in my code to add or remove items from the collection.
        static ConcurrentDictionary<string, string> usersDictionary = new ConcurrentDictionary<string, string>();
        public void InitSignalR(String msg)
        {
            Clients.All.doInitSignalR(msg);
        }
        public void Send(string name, string message)
        {
            //Broadcast To All
            Clients.All.broadcastMessage(name, message);
        }

        public void SendTo(string name, string message, string receiver)
        {
            // Broadcast To Caller
            Clients.Caller.broadcastMessage(name, message);
            // Only To Reciever
            Clients.Client(usersDictionary[receiver]).broadcastMessage(name, message);
        }

        public void Notify(string name, string guid)
        {
            // If new incomer already Exists in dictionary force to change the name
            if (usersDictionary.ContainsKey(name))
            {
                Clients.Caller.anotherName(name);
            }
            else
            {
                bool addedSuccess = usersDictionary.TryAdd(name, guid);
                foreach(var entry in usersDictionary)
                {
                    Clients.Caller.online(entry.Key);
                }
                if(addedSuccess)
                {
                    Clients.Others.justJoin(name);
                }                
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            String targetGuid = Context.ConnectionId;
            if(usersDictionary.Values.Contains(targetGuid))
            {
                string sValue;
                var name = usersDictionary.FirstOrDefault(u => u.Value == targetGuid);
                usersDictionary.TryRemove(name.Key, out sValue);
                return Clients.All.disconnected(name.Key);
            }
            return base.OnDisconnected(stopCalled);
        }
    }
}