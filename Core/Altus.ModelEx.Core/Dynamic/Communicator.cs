using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using Altus.Core;
using Altus.Core.Component;
using Altus.Core.Dynamic.Components;
using Altus.Core.Messaging.Udp;
using Altus.Core.Security;

namespace Altus.Core.Dynamic
{
    public class DynamicEventArgs : DynamicObject
    {
        public DynamicEventArgs()
        {

        }
    }

    public delegate void DynamicEventHandler(object sender, dynamic e);

    public class Communicator : DynamicObject
    {
        static Communicator()
        {
            if (!DynamicShell.Instance.IsRunning)
            {
                // loads the components
                DynamicShell.RunWithoutHosting(new Context(InstanceType.WindowsFormsClient, IdentityType.ServiceProcess),
                    "-ListenHttpIP:+",
                    "-ListenHttpPort:808",
                    "-ListenTcpIP:127.0.0.1",
                    "-ListenTcpPort:909",
                    "-ListenerThreadCount:4",
                    "-ListenUdpIP:127.0.0.1",
                    "-ListenUdpPort:919",
                    "-ListenUdpBridgeIP:127.0.0.1",
                    "-ListenUdpBridgePort:929",
                    "-UdpMulticastIP:224.100.0.2",
                    "-NodeAddress:o.v.a.m.c.002.c.c",
                    "-NetworkId:Net1");
            }
        }

        public Communicator()
        {
            Context.CurrentContext = Context.GlobalContext;
            NodeAddress = NodeIdentity.NodeAddress;
        }
        public string NodeAddress { get; set; }
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            return base.TryInvokeMember(binder, args, out result);
        }

        public void Join(string nodeAddress, string authKey)
        {

        }

        public void Publish(string topicName, object data)
        {
            //DynamicShell.Instance.Shell.GetComponent<UdpHost>("UdpHost").Broadcast(topicName, data);
        }

        public void Subscribe(string topic, DynamicEventHandler callback)
        {
            
        }

        private string GetDataTypeForTopic(string topic)
        {
            return "CabinetStatus";
        }

        public void Post(string uri)
        {

        }

        public void CreateTopic(string topic, string dataType)
        {

        }


    }

    public class Test
    {
        public static void Main()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            dynamic p = asm.CreateInstance("Altus.dynamic.Publisher");
            p.Publish("some data");
        }
    }
}
