﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoMaS.Attributes
{
    /// <summary>
    /// Describes a class as command manager
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class CommandManagerAttribute : Attribute
    {
        /// <summary>
        /// Clear labeling of the manager
        /// </summary>
        public readonly object Tag;
        /// <summary>
        /// Namespaces where commands are to be registered
        /// </summary>
        public readonly string[] CommandNamespaces;

        /// <summary>
        /// Describes a class as command manager
        /// </summary>
        /// <param name="tag">Clear labeling of the manager</param>
        public CommandManagerAttribute(object tag)
        {
            Tag = tag;
        }
        /// <summary>
        /// Describes a class as command manager
        /// </summary>
        /// <param name="tag">Clear labeling of the manager</param>
        /// <param name="commandNamespaces">Namespaces where commands are to be registered</param>
        public CommandManagerAttribute(object tag, params string[] commandNamespaces)
        {
            Tag = tag;
            CommandNamespaces = commandNamespaces;
        }
    }
}