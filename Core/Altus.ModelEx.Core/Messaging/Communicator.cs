using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace Altus.Core.Messaging
{
    public enum DeliveryOption
    {
        MulticastBestEffort,
        BestEffort,
        Guaranteed
    }

    public class Communicator : DynamicObject
    {
        /// <summary>
        /// Request service and wait for a response from the designated service Uri
        /// </summary>
        /// <typeparam name="U">type of data to receive from service response</typeparam>
        /// <param name="serviceUri">uri of the form "Altus://[company].[solution].[unit].[customer].[subnet].[nodeID].mobytech.com/[Service]/[Method]?arg1={1}&arg2={2}..."</param>
        /// <param name="argVals">optional additional scalar parameters</param>
        /// <returns></returns>
        public U Request<U>(string serviceUri, params object[] argVals)
        {
            throw (new NotImplementedException());
        }
        /// <summary>
        /// Request service and wait for a response from the designated service Uri
        /// </summary>
        /// <typeparam name="T">type of data to send to service</typeparam>
        /// <typeparam name="U">type of data to receive from service response</typeparam>
        /// <param name="serviceUri">uri of the form "Altus://[company].[solution].[unit].[customer].[subnet].[nodeID].mobytech.com/[Service]/[Method]/[DataType]?arg1={1}&arg2={2}..."</param>
        /// <param name="data">the main data payload for the request</param>
        /// <param name="argVals">optional additional scalar parameters</param>
        /// <returns></returns>
        public U Request<T, U>(string serviceUri, T data, params object[] argVals)
        {
            throw (new NotImplementedException());
        }


        /// <summary>
        /// Sends data to the service uri provided without blocking for a response, using a best-effort attempt to deliver the payload
        /// </summary>
        /// <typeparam name="T">type of data to send</typeparam>
        /// <param name="serviceUri">uri of the form "Altus://[company].[solution].[unit].[customer].[subnet].[nodeID].mobytech.com/[Service]/[Method]/[DataType]?arg1={1}&arg2={2}..."</param>
        /// <param name="data">the main data payload to send</param>
        public void Send<T>(string serviceUri, T data)
        {
            throw(new NotImplementedException());
        }

        /// <summary>
        /// Sends data to the service uri provided without blocking for a response, using the specified delivery guarantee for the payload
        /// </summary>
        /// <typeparam name="T">type of data to send</typeparam>
        /// <param name="serviceUri">uri of the form "Altus://[company].[solution].[unit].[customer].[subnet].[nodeID].mobytech.com/[Service]/[Method]/[DataType]?arg1={1}&arg2={2}..."</param>
        /// <param name="deliveryOption">the level of delivery guarantee for the operation</param>
        /// <param name="data">the main data payload to send</param>
        public void Send<T>(string serviceUri, DeliveryOption deliveryOption, T data)
        {
            throw (new NotImplementedException());
        }

        /// <summary>
        /// Publishes the data provided for the given topic using a best-effort delivery mode
        /// </summary>
        /// <typeparam name="T">type of data to publish</typeparam>
        /// <param name="topicName">name of topic to publish data for</param>
        /// <param name="data">the main data payload to publish</param>
        public void Publish<T>(string topicName, T data)
        {
            throw (new NotImplementedException());
        }

        /// <summary>
        /// Subscribes to the designated topicName, registering the messageCallback to receive data as it arrives.  
        /// Messages will be delivered in a best-effort fashion.
        /// </summary>
        /// <typeparam name="T">payload type provided by the publisher</typeparam>
        /// <param name="topicName">name of the topic to subscribe to</param>
        /// <param name="messageCallback">the callback delegate to handle received messages</param>
        public void Subscribe<T>(string topicName, SubscriptionMessageAvailableHandler<T> messageCallback)
        {
            Subscribe<T>(topicName, null, messageCallback);
        }

        /// <summary>
        /// Subscribes to the designated topicName provided by the designated publisher, registering the messageCallback to receive data as it arrives.  
        /// Messages will be delivered in a best-effort fashion.
        /// </summary>
        /// <typeparam name="T">payload type provided by the publisher</typeparam>
        /// <param name="publisher">long-form node address according to the following form "[company].[solution].[unit].[customer].[subnet].[nodeID].mobytech.com"</param>
        /// <param name="topicName">name of the topic to subscribe to</param>
        /// <param name="messageCallback">the callback delegate to handle received messages</param>
        public void Subscribe<T>(string publisher, string topicName, SubscriptionMessageAvailableHandler<T> messageCallback)
        {
            Subscribe<T>(publisher, topicName, messageCallback, DeliveryOption.BestEffort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="publisher"></param>
        /// <param name="topicName"></param>
        /// <param name="publisherData"></param>
        /// <param name="messageCallback"></param>
        public void Subscribe<T, U>(string publisher, string topicName, U publisherData, SubscriptionMessageAvailableHandler<T> messageCallback)
        {
            Subscribe<T, U>(publisher, topicName, publisherData, messageCallback, DeliveryOption.BestEffort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="publisher"></param>
        /// <param name="topicName"></param>
        /// <param name="messageCallback"></param>
        /// <param name="deliveryOption"></param>
        public void Subscribe<T>(string publisher, string topicName, SubscriptionMessageAvailableHandler<T> messageCallback, DeliveryOption deliveryOption)
        {
            Subscribe<T, object>(publisher, topicName, null, messageCallback, deliveryOption);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="publisher"></param>
        /// <param name="topicName"></param>
        /// <param name="publisherData"></param>
        /// <param name="messageCallback"></param>
        /// <param name="deliveryOption"></param>
        public void Subscribe<T, U>(string publisher, string topicName, U publisherData, SubscriptionMessageAvailableHandler<T> messageCallback, DeliveryOption deliveryOption)
        {
            throw (new NotImplementedException());
        }

    }

    public delegate void SubscriptionMessageAvailableHandler<T>(SubscriptionMessage<T> message);
    public class SubscriptionMessage<T>
    {
        public SubscriptionMessage(T payload)
        {
            Payload = payload;
        }

        public T Payload { get; private set; }
    }
}
