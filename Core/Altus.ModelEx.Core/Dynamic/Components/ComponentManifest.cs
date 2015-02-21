using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.component;

[assembly: Component(
    Component = "Altus.serialization.TextSerializer, Altus",
    Name = "CabinetStatusSerializer")]

//[assembly: Component(
//    ComponentType = "Altus.http.HttpHost, Altus",
//    Name = "HttpHost")]
[assembly: Component(
    Component = "Altus.udp.UdpTopicAllocationStrategy, Altus",
    Name = "UdpAllocationStrategy")]

[assembly: Component(
    Component = "Altus.udp.UdpHost, Altus",
    Name = "UdpHost",
    Dependencies = new string[]{ "UdpAllocationStrategy" })]

//[assembly: Component(
//    ComponentType = "Altus.tcp.TcpHost, Altus",
//    Name = "TcpHost")]

[assembly: Component(
    Component = "Altus.udp.UdpTcpBridgeHost, Altus",
    Name = "UdpTcpBridgeHost")]

[assembly: Component(
    Component = "Altus.publication.SubscriptionProcessor, Altus",
    Name = "SubscriptionProcessor")]

[assembly: Component(
    Component = "Altus.messaging.MessagingPipeline, Altus",
    Name = "MessagingPipeline",
    Dependencies = new string[]{"SubscriptionProcessor", "HttpHost", "TcpHost", "UdpHost", "UdpTcpBridgeHost", "CabinetStatusSerializer"})]

[assembly: Component(
    Component = "Altus.dns.DnsHost, Altus",
    Name = "DnsHost")]

//[assembly: Component(
//    ComponentType = "Altus.node.components.TestComponent, moby.node",
//    Name = "TestComponent",
//    Dependencies = new string[]{"DnsHost", "MessagingPipeline"})]