﻿////////////////////////////////////////////////////////////////////////////////////////////////////////
// LLBLGen Pro is (c) 2002-2013 Solutions Design. All rights reserved.
// http://www.llblgen.com
////////////////////////////////////////////////////////////////////////////////////////////////////////
// COPYRIGHTS:
// Copyright (c)2002-2013 Solutions Design. All rights reserved.
// http://www.llblgen.com
// 
// This LLBLGen Pro Contrib library is released under the following license: (BSD2)
//
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met: 
//
// 1) Redistributions of source code must retain the above copyright notice, this list of 
//    conditions and the following disclaimer. 
//   
// 2) Redistributions in binary form must reproduce the above copyright notice, this list of 
//    conditions and the following disclaimer in the documentation and/or other materials 
//    provided with the distribution. 
// 
// THIS SOFTWARE IS PROVIDED BY SOLUTIONS DESIGN ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL SOLUTIONS DESIGN OR CONTRIBUTORS BE LIABLE FOR 
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, 
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
// USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
//
// The views and conclusions contained in the software and documentation are those of the authors 
// and should not be interpreted as representing official policies, either expressed or implied, 
// of Solutions Design. 
//////////////////////////////////////////////////////////////////////
// Contributers to the code:
//		- Frans Bouma [FB]
//////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace SD.LLBLGen.Pro.ORMSupportClasses.Contrib
{
	/// <summary>
	/// Implementation of IResultsetCache which utilizes the .NET MemoryCache class. It instantiates a new MemoryCache instance
	/// so multiple instances of MemoryCachedResultsetCache will create multiple MemoryCache instances.
	/// </summary>
	public class MemoryCachedResultsetCache : IResultsetCache
	{
		#region Class Member Declarations
		private MemoryCache _memoryCache;
		private Dictionary<CacheKey, Guid> _cacheKeyToMemoryCacheKey;
		#endregion


		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryCachedResultsetCache"/> class.
		/// </summary>
		/// <param name="name">The name to use to look up configuration information. This is passed to the MemoryCache constructor</param>
		/// <param name="config">A collection of name/value pairs of configuration information to use for configuring the cache. This is passed
		/// to the MemoryCache constructor</param>
		/// <remarks>See for details on the parameters of this constructor the MemoryCache constructor documentation:
		/// http://msdn.microsoft.com/en-us/library/system.runtime.caching.memorycache.memorycache.aspx
		/// </remarks>
		public MemoryCachedResultsetCache(string name, NameValueCollection config)
		{
			_memoryCache = new MemoryCache(name, config);
			_cacheKeyToMemoryCacheKey = new Dictionary<CacheKey, Guid>();
		}


		/// <summary>
		/// Adds the specified toCache to this cache under the key specified for the duration specified
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="toCache">The resultset to cache</param>
		/// <param name="duration">The duration how long the resultset will stay in the cache.</param>
		/// <remarks>
		/// If an object is already present under 'key', Add is a no-op.
		/// </remarks>
		public void Add(CacheKey key, CachedResultset toCache, TimeSpan duration)
		{
			Add(key, toCache, duration, false);
		}


		/// <summary>
		/// Adds the specified toCache to this cache under the key specified for the duration specified
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="toCache">The resultset to cache</param>
		/// <param name="duration">The duration how long the resultset will stay in the cache.</param>
		/// <param name="overwriteIfPresent">if set to <c>true</c> it will replace an existing cached set with the one specified.</param>
		public void Add(CacheKey key, CachedResultset toCache, TimeSpan duration, bool overwriteIfPresent)
		{
			var keyToUse = GetMemoryCacheKey(key);
			var policyToUse = ProduceCacheItemPolicy(duration);
			if(overwriteIfPresent)
			{
				_memoryCache.Set(keyToUse, toCache, policyToUse);
			}
			else
			{
				_memoryCache.Add(keyToUse, toCache, policyToUse);
			}
		}


		/// <summary>
		/// Gets the cached resultset under the key specified.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>
		/// The cached resultset, if present, otherwise null
		/// </returns>
		public CachedResultset Get(CacheKey key)
		{
			return _memoryCache.Get(GetMemoryCacheKey(key)) as CachedResultset;
		}


		/// <summary>
		/// Purges the resultset cached under the key specified from the cache, if present.
		/// </summary>
		/// <param name="key">The key.</param>
		public void PurgeResultset(CacheKey key)
		{
			_memoryCache.Remove(GetMemoryCacheKey(key));
		}


		/// <summary>
		/// Produces the cache item policy for the item to cache, based on the duration specified
		/// </summary>
		/// <param name="duration">The duration.</param>
		/// <returns></returns>
		protected virtual CacheItemPolicy ProduceCacheItemPolicy(TimeSpan duration)
		{
			return new CacheItemPolicy() { AbsoluteExpiration = new DateTimeOffset(DateTime.Now, duration) };
		}


		/// <summary>
		/// Gets the memory cache key to use for the cachekey specified.
		/// </summary>
		/// <param name="originalKey">The original key.</param>
		/// <returns>the string equivalent of the guid associated with the cachekey specified</returns>
		private string GetMemoryCacheKey(CacheKey originalKey)
		{
			Guid toReturn = Guid.Empty;
			using(TimedLock.Lock(_memoryCache))
			{
				if(!_cacheKeyToMemoryCacheKey.TryGetValue(originalKey, out toReturn))
				{
					toReturn = Guid.NewGuid();
					_cacheKeyToMemoryCacheKey.Add(originalKey, toReturn);
				}
			}
			return toReturn.ToString();
		}
	}
}