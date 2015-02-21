using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Caching
{
    public interface ICache
    {
        #region Event Declarations
        #endregion Event Declarations

        #region Properties
        //========================================================================================================//
        /// <summary>
        /// Gets the number of items in the cache
        /// </summary>
        int Count { get; }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Indexer returns items by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object this[string key] { get; }
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Returns a synchronization object for use in multi-threaded cache access
        /// </summary>
        object SyncRoot { get; }
        //========================================================================================================//

        #endregion Properties

        #region Methods
        //========================================================================================================//
        /// <summary>
        /// Adds a new entry to the cache using default timeouts and no dependencies
        /// </summary>
        /// <param name="key">key of item to add</param>
        /// <param name="value">value of item to add</param>
        void Add(string key, object value);
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Adds a new item to the cache using specified timeouts and dependencies
        /// </summary>
        /// <param name="key">key of item to add</param>
        /// <param name="value">value of item to add</param>
        /// <param name="absoluteExpiration">the absolute time when the item should be removed from the cache</param>
        /// <param name="slidingExpiration">the duration of time that can elapse after the item is last accessed, 
        /// before the item is removed from the cache (i.e. the item can live for 2 minutes after it was last accessed)</param>
        /// <param name="dependencies">an array of other cached keys that this item's value depends upon.  If one
        /// of those depencies expires or changes, this item will also expire and be removed.</param>
        void Add(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, string[] dependencies);
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Use this method to replace an existing entry in the cache with default timeouts and dependencies
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Insert(string key, object value);
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Use this method to replace an existing entry in the cache using specified timeouts and dependencies
        /// </summary>
        /// <param name="key">key of item to add</param>
        /// <param name="value">value of item to add</param>
        /// <param name="absoluteExpiration">the absolute time when the item should be removed from the cache</param>
        /// <param name="slidingExpiration">the duration of time that can elapse after the item is last accessed, 
        /// before the item is removed from the cache (i.e. the item can live for 2 minutes after it was last accessed)</param>
        /// <param name="dependencies">an array of other cached keys that this item's value depends upon.  If one
        /// of those depencies expires or changes, this item will also expire and be removed.</param>
        void Insert(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration, string[] dependencies);
        //========================================================================================================//

        //========================================================================================================//
        /// <summary>
        /// Remove an item from the cache
        /// </summary>
        /// <param name="key">the key of the item to remove</param>
        void Remove(string key);
        //========================================================================================================//

        /// <summary>
        /// Indicates if an entry with the given key is contain in the cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool ContainsKey(string key);
        #endregion Methods
    }
}
