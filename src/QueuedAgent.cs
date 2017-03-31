﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreAPM.Events.Model;
using Newtonsoft.Json.Linq;

namespace CoreAPM.DotNet.Agent
{
    public class QueuedAgent : Agent
    {
        private readonly List<Event> _eventQueue = new List<Event>();
        private bool _alive = true;
        private readonly TimeSpan _sendInterval;

        public QueuedAgent(IConfig config, HttpClient httpClient, TimeSpan? sendInterval = null) : base(config, httpClient)
        {
            _sendInterval = sendInterval ?? TimeSpan.FromSeconds(1);
            Task.Run(RunSender);
        }

        private async Task RunSender()
        {
            while (_alive)
            {
                var eventsToSend = GetEventsToSend();
                if (eventsToSend.Any())
                    await SendEvents(eventsToSend);
                Thread.Sleep(_sendInterval);
            }
        }

        public IList<Event> GetEventsToSend()
        {
            return _eventQueue.ToList();
        }

        public async Task SendEvents(ICollection<Event> eventsToSend)
        {
            var content = GetPostContent(eventsToSend);
            await _httpClient.PostAsync(_addEventURL, content);
            _eventQueue.RemoveAll(eventsToSend.Contains);
        }

        public static HttpContent GetPostContent(IEnumerable<Event> eventsToSend)
        {
            return new StringContent(JArray.FromObject(eventsToSend).ToString());
        }

        public override void Send(Event e) => _eventQueue.Add(e);

        public override void Dispose()
        {
            _alive = false;
            Thread.Sleep(_sendInterval);
            SendEvents(GetEventsToSend()).Wait(_sendInterval);
            base.Dispose();
        }
    }
}