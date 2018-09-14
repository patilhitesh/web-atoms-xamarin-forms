﻿using System;
using System.Linq;

namespace WebAtoms
{
    /// <summary>
    /// Returns an instance of HttpClient with persistable cookie storage, 
    /// use with DI only
    /// </summary>
    public interface IWebClient : IDisposable
    {


        /// <summary>
        /// Do not ever dispose if used as singleton...
        /// </summary>
        System.Net.Http.HttpClient Client { get; }

        /// <summary>
        /// User Agent 
        /// </summary>
        string UserAgent { get; set; }


        /// <summary>
        /// 
        /// </summary>
        void ClearCookies();

    }
}
