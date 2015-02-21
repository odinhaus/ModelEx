using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Altus.component;
using System.ComponentModel;

namespace Altus.messaging.udp
{
    public interface IUdpTopicAllocationStrategy : IComponent
    {
        bool TopicExists(string topicName);
        IPAddress GetAddress(string topicName);
        IPAddress AllocateAddress(string topicName);
        void DeAllocateAddress(string topicName);
    }

    public class UdpTopicAllocationStrategy : InitializableComponent, IUdpTopicAllocationStrategy
    {
        Dictionary<string, IPAddress> _topics = new Dictionary<string, IPAddress>();
        IPAddress _baseAddress;
        IPAddress _maxAddress = IPAddress.Parse("239.255.255.255");

        protected override void OnInitialize(params string[] args)
        {
            _baseAddress = IPAddress.Parse(Context.GetEnvironmentVariable("UdpMulticastIP").ToString());
        }

        public bool TopicExists(string topicName)
        {
            lock (_topics)
            {
                return _topics.ContainsKey(topicName);
            }
        }

        public IPAddress GetAddress(string topicName)
        {
            lock (_topics)
            {
                return _topics[topicName];
            }
        }

        public IPAddress AllocateAddress(string topicName)
        {
            // TODO: need to handle holes in the allocation table cause by topic deallocation

            if (TopicExists(topicName))
                throw (new InvalidOperationException("Topic " + topicName + " already exists."));
            byte[] baseBytes = _baseAddress.GetAddressBytes();
            if (baseBytes[3] < 255)
                baseBytes[3]++;
            else if (baseBytes[2] < 255)
            {
                baseBytes[3] = 0;
                baseBytes[2]++;
            }
            else
            {
                throw (new InvalidOperationException("Multicast IP Allocation table is full.  Deallocate some IPs, and try again."));
            }
            IPAddress add = new IPAddress(baseBytes);
            lock (_topics)
            {
                _topics.Add(topicName, add);
            }
            // announce the new topic address here
            //UdpHost host = Application.Instance.Shell.GetComponent<UdpHost>();
            //host.Send(string.Format("udp://{0}/moby/CreateTopic/Topic/all", 
            //        GetNodeAddress()),
            //        topicName,
            //        new UdpHeader("Altus-MessageType", "PubSub"),
            //        new UdpHeader("Altus-Sender", GetNodeAddress()),
            //        new UdpHeader("Altus-GuaranteedDelivery", "false"));
            return add;
        }

        public void DeAllocateAddress(string topicName)
        {
            lock (_topics)
            {
                _topics.Remove(topicName);
            }
            //UdpHost host = Application.Instance.Shell.GetComponent<UdpHost>();
            //host.Send(string.Format("udp://{0}/moby/DeleteTopic/Topic/all", 
            //        GetNodeAddress()),
            //        topicName,
            //        new UdpHeader("Altus-MessageType", "PubSub"),
            //        new UdpHeader("Altus-Sender", GetNodeAddress()),
            //        new UdpHeader("Altus-GuaranteedDelivery", "false"));
            // announce the topic removale here???
        }
    }
}
