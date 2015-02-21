using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoodMac.ModelEx.Core.Component;
using WoodMac.ModelEx.Core.Data;
using WoodMac.ModelEx.Core;
using WoodMac.ModelEx.Core.Messaging;
using WoodMac.ModelEx.Core.Messaging.Udp;
using WoodMac.ModelEx.Core.Processing;
using WoodMac.ModelEx.Core.Scheduling;
using WoodMac.ModelEx.Core.Serialization;
using WoodMac.ModelEx.Core.Realtime;
using WoodMac.ModelEx.Core.Data.SQlite;
using WoodMac.ModelEx.Core.Licensing;
using System.Diagnostics;

namespace WoodMac.ModelEx.Core.PubSub
{
    public abstract class TopicPublisher : LicensedComponent, IPublisher
    {
        public TopicPublisher()
        {
            this.App = Context.CurrentContext.CurrentApp;
        }

        protected override bool OnInitialize(params string[] args)
        {
            PublicationDefinition pcd = OnGetPublicationDefinition();
            this.Name = pcd.ToString();
            this.Definition = pcd;
            this.Schedule = new PeriodicSchedule(DateRange.Forever, pcd.DefaultInterval);
            return true;
        }

        protected virtual PublicationDefinition OnGetPublicationDefinition()
        {
            PublicationComponentDefinition pcd = DataContext.Default.GetLocalPublications()
                    .Where( d => TypeHelper.GetType(d.CLRType).Equals(this.GetType())).FirstOrDefault();
            return pcd;

        }
        
        DateTime _lastUpdate = DateTime.MinValue;
        //int count = 0;
        //DateTime start = CurrentTime.Now;
        public object Execute(params object[] args)
        {
            if (this.IsEnabled)
            {
                this.Definition.Topic.Publish();
                //count++;
                //if (count % 10 == 0)
                //{
                //    DateTime end = CurrentTime.Now;
                //    Debug.WriteLine((double)count / end.Subtract(start).TotalSeconds);
                //    count = 0;
                //    start = end;
                //}
                _lastUpdate = this.Definition.Topic.LastUpdated;
            }
            return null;
        }

        public PublicationDefinition Definition { get; private set; }

        public Schedule Schedule
        {
            get;
            set;
        }

        public void Kill()
        {
            this.Schedule = new PeriodicSchedule(DateRange.Never, 0);
        }

        public System.Threading.ThreadPriority Priority
        {
            get { return System.Threading.ThreadPriority.Normal; }
        }
        public byte ProcessorAffinityMask { get; private set; }

        public DeclaredApp App { get; set; }

        protected override void OnApplyLicensing(ILicense[] licenses, params string[] args)
        {
            
        }

        protected override bool OnIsLicensed(object component)
        {
            return true;
        }
    }
}
