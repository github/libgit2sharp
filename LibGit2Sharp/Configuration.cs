﻿using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides access to the '.git\config' configuration for a repository.
    /// </summary>
    public class Configuration : IDisposable
    {
        private readonly ConfigurationSafeHandle handle;

        public Configuration(Repository repository)
        {
            Ensure.Success(NativeMethods.git_repository_config(out handle, repository.Handle, null, null));
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }
        }

        /// <summary>
        ///   Get a configuration value for a key. Keys are in the form 'section.name'.
        ///   <para>
        ///     For example in  order to get the value for this in a .git\config file:
        /// 
        ///     [core]
        ///     bare = true
        /// 
        ///     You would call:
        /// 
        ///     bool isBare = repo.Config.Get&lt;bool&gt;("core.bare");
        ///   </para>
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)GetString(key);
            }

            if (typeof(T) == typeof(bool))
            {
                return (T)(object)GetBool(key);
            }

            if (typeof(T) == typeof(int))
            {
                return (T)(object)GetInt(key);
            }

            if (typeof(T) == typeof(long))
            {
                return (T)(object)GetLong(key);
            }

            return default(T);
        }

        private bool GetBool(string key)
        {
            bool value;
            Ensure.Success(NativeMethods.git_config_get_bool(handle, key, out value));
            return value;
        }

        private int GetInt(string key)
        {
            int value;
            Ensure.Success(NativeMethods.git_config_get_int(handle, key, out value));
            return value;
        }

        private long GetLong(string key)
        {
            long value;
            Ensure.Success(NativeMethods.git_config_get_long(handle, key, out value));
            return value;
        }

        private string GetString(string key)
        {
            IntPtr value;
            Ensure.Success(NativeMethods.git_config_get_string(handle, key, out value));
            return value.MarshallAsString();
        }

        /// <summary>
        ///   Set a configuration value for a key. Keys are in the form 'section.name'.
        ///   <para>
        ///     For example in order to set the value for this in a .git\config file:
        ///   
        ///     [test]
        ///     boolsetting = true
        ///   
        ///     You would call:
        ///   
        ///     repo.Config.Set("test.boolsetting", true);
        ///   </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set<T>(string key, T value)
        {
            if (typeof(T) == typeof(string))
            {
                Ensure.Success(NativeMethods.git_config_set_string(handle, key, (string)(object)value));
            }

            if (typeof(T) == typeof(bool))
            {
                Ensure.Success(NativeMethods.git_config_set_bool(handle, key, (bool)(object)value));
            }

            if (typeof(T) == typeof(int))
            {
                Ensure.Success(NativeMethods.git_config_set_int(handle, key, (int)(object)value));
            }

            if (typeof(T) == typeof(long))
            {
                Ensure.Success(NativeMethods.git_config_set_long(handle, key, (long)(object)value));
            }
        }
    }
}
