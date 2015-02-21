using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.GEM.ViewModels
{
    public enum BehaviorType
    {
        Execute,
        Delete,
        Create,
        Update,
        Read,
        Select,
        Rename,
        Preview
    }

    public enum ResultType
    {
        Information,
        Error,
        Success,
        Warning
    }

    public class EntityBehavior
    {
        public EntityBehavior(Entity handler, BehaviorType behavior, params EntityBehaviorResult[] results)
        {
            this.Handler = handler;
            this.BehaviorType = behavior;
            this.Results = results;
        }

        public Entity Handler { get; private set; }

        public BehaviorType BehaviorType { get; private set; }

        public EntityBehaviorResult[] Results { get; private set; }
    }

    public class EntityBehaviorResult
    {
        public EntityBehaviorResult(ResultType type, params object[] results)
        {
            this.ResultType = type;
            this.Results = results;
        }

        public ResultType ResultType { get; private set; }

        public object[] Results { get; private set; }
    }
}
