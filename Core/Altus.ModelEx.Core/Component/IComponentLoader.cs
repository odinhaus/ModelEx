using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace Altus.Core.Component
{
    public delegate void ComponentLoadStatusHandler(ComponentLoadStatusEventArgs e);
    public delegate void ComponentLoadCompleteHandler();
    public delegate void ComponentLoadBeginHandler();
    public delegate void ComponentLoadCoreBegin();
    public delegate void ComponentLoadCoreComplete();
    public delegate void ComponentLoadExtensionsBegin();
    public delegate void ComponentLoadExtensionsComplete();

    public class ComponentLoadStatusEventArgs : EventArgs
    {
        public ComponentLoadStatusEventArgs() { }
        public ComponentLoadStatusEventArgs(string message, IComponent component, int index, int count)
        {
            this.Message = message;
            this.Component = component;
            this.Name = component.GetType().FullName;
            this.Index = index;
            this.Count = count;
        }
        public ComponentLoadStatusEventArgs(string message, IComponent component, string name, int index, int count)
        {
            this.Message = message;
            this.Component = component;
            this.Name = name;
            this.Count = count;
            this.Index = index;
        }
        public string Message { get; private set; }
        public IComponent Component { get; private set; }
        public string Name { get; private set; }
        public int Index { get; private set; }
        public int Count { get; private set; }
    }

    public interface IComponentLoader
    {
        /// <summary>
        /// Attach to this method to receive status updates during the module load process
        /// </summary>
        event ComponentLoadStatusHandler LoadStatus;
        /// <summary>
        /// Attach to this event to receive notification that the modules have all been loaded
        /// </summary>
        event ComponentLoadCompleteHandler LoadComplete;
        /// <summary>
        /// Attach to this event to receive notification that a component loading session is about to begin
        /// </summary>
        event ComponentLoadBeginHandler LoadBegin;
        /// <summary>
        /// Attach to this event to receive notification that the loading of core framework assemblies has started
        /// </summary>
        event ComponentLoadCoreBegin LoadCoreBegin;
        /// <summary>
        /// Attach to this event to receive notification that the loading of core framework assemblies has completed
        /// </summary>
        event ComponentLoadCoreComplete LoadCoreComplete;
        /// <summary>
        /// Attach to this event to receive notification that the loading of core framework assemblies has started
        /// </summary>
        event ComponentLoadExtensionsBegin LoadExtensionsBegin;
        /// <summary>
        /// Attach to this event to receive notification that the loading of core framework assemblies has completed
        /// </summary>
        event ComponentLoadExtensionsComplete LoadExtensionsComplete;
        /// <summary>
        /// Call this method to begin the load process
        /// </summary>
        /// <param name="targetRegion">provide the region the modules should be loaded into</param>
        void LoadComponents(params string[] args);
        /// <summary>
        /// gets a boolean indicating whether the module loader has completed the load operation
        /// </summary>
        bool IsComplete { get; }
        /// <summary>
        /// call this method to cancel the load operation
        /// </summary>
        void Cancel();

        /// <summary>
        /// Gets the full list of all components loaded, including modules, and other component types
        /// </summary>
        IComponent[] Components { get; }

        /// <summary>
        /// Adds a component to the current load list, at the end of the current list
        /// </summary>
        /// <param name="component"></param>
        void Add(IComponent component);

        /// <summary>
        /// Adds a component with the specified name to the current load list, at the end of the current list
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        void Add(IComponent component, string name);

        /// <summary>
        /// Adds an assembly as a possible component source to the current load list, at the end of the current list
        /// </summary>
        /// <param name="component"></param>
        void Add(Assembly assembly);
    }
}

