using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Scheduling;

namespace Altus.Core.PubSub
{
    public delegate void PublicationErrorHandler(object sender, PublicationErrorArgs e);
    public delegate void PublicationDefinitionHandler(object sender, PublicationDefinitionArgs e);

    public class PublicationDefinitionArgs : EventArgs
    {
        public PublicationDefinitionArgs(Subscription subscription)
        {
            this.Subscription = subscription;
        }
        /// <summary>
        /// Gets the active subscription for the current context.
        /// </summary>
        public Subscription Subscription { get; private set; }
        /// <summary>
        /// Publishers can explicitly set the publication schedule for the current publicaiton context with this property.
        /// </summary>
        public Schedule Schedule { get; set; }
        /// <summary>
        /// Publishers can set this to TRUE to cancel the subscription request for the current context.
        /// </summary>
        public bool Cancel { get; set; }
        /// <summary>
        /// Publishers can attach event handlers to catch errors.
        /// </summary>
        public event PublicationErrorHandler Error;
        /// <summary>
        /// For guaranteed delivery publications, the specified delegate will be called after the subscriber 
        /// acknowledges receipt of the published data.
        /// </summary>
        public Delegate Success;
    }

    public class PublicationErrorArgs : EventArgs
    {
        public PublicationErrorArgs(Subscription subscription, object payload, Exception exception)
        {
            this.Subscription = subscription;
            this.Exception = exception;
            this.PublicationPayload = payload;
        }
        public Subscription Subscription { get; private set; }
        public object PublicationPayload { get; private set; }
        public Exception Exception { get; private set; }
    }

    public class PublicationSuccessArgs : EventArgs
    {
        public PublicationSuccessArgs(Subscription subscription, object payload, object response)
        {
            this.Subscription = subscription;
            this.ResponsePayload = response;
            this.PublicationPayload = payload;
        }
        public Subscription Subscription { get; private set; }
        public object PublicationPayload { get; private set; }
        public object ResponsePayload { get; private set; }
    }

    /// <summary>
    /// Attribute used to declare a method or event as a publication source.  In the case of events, delegate return values cannot be used as 
    /// publication payload arguments.  Only in and out parameters may be sent as publication payload data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = true, Inherited = true)]
    public class PublicationAttribute : Attribute
    {
        public PublicationAttribute(string topicName)
        {
            this.TopicName = topicName;
            this.Interval = 30000;
            this.Public = true;

        }

        public PublicationAttribute(string topicName, int interval) : this(topicName)
        {
            this.Interval = interval;
        }

        /// <summary>
        /// Gets the topic name for the publication
        /// </summary>
        public string TopicName { get; private set; }
        /// <summary>
        /// Get/set the default publication interval in milliseconds
        /// </summary>
        public int Interval { get; set; }
        /// <summary>
        /// Get/set whether this publisher is available to outside processes
        /// </summary>
        public bool Public { get; set; }
    }

    /// <summary>
    /// Attribue used to declare a method to be called during publication construction to determine the publication schedule 
    /// and any other initialization for the given topic.  This attribute can only be used on methods that implement the
    /// PublicationDefinitionHandler delegate interface.  The method decorated with this attribute will be called once during initial construction
    /// of the publication, and then again once for each new subscriber that consumes the publication (if the topic is configured on a subcriber-specific basis).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public class PublicationDefinitionAttribute : Attribute
    {
        public PublicationDefinitionAttribute(string topicName)
        {
            this.TopicName = topicName;

        }
        public string TopicName { get; private set; }
    }

    /// <summary>
    /// Attribute used to declare a method of delegate type PublicationErrorHandler should handle error responses
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PublicationErrorAttribute : Attribute
    {
        public PublicationErrorAttribute(string topicName)
        {
            this.TopicName = topicName;

        }
        public string TopicName { get; private set; }
    }

    /// <summary>
    /// Attribute used to declare a method of delegate type PublicationSuccessHandler should handle success responses
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PublicationSuccessAttribute : Attribute
    {
        public PublicationSuccessAttribute(string topicName)
        {
            this.TopicName = topicName;

        }
        public string TopicName { get; private set; }
    }

    /// <summary>
    /// Attribute used to indicate that the parameter of a method or delegate should be sent to subscribers
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple=false, Inherited=true)]
    public class PublicationDataAttribute : Attribute
    {

    }
}
