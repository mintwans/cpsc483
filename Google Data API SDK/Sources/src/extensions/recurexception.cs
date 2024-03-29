/* Copyright (c) 2006 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections;
using System.Text;
using System.Xml;
using Google.GData.Client;

namespace Google.GData.Extensions {

    /// <summary>
    /// GData schema extension describing an RFC 2445 recurrence rule.
    /// </summary>
    public class RecurrenceException : IExtensionElement
    {
        /// <summary>optional nested entry</summary>
        protected EntryLink entryLink; 

        /// <summary>specialized exception or not</summary>
        protected bool isSpecialized; 

        /// <summary>
        ///  Nested entry (optional).
        /// </summary>
        public EntryLink EntryLink
        {
            get { return entryLink; }
            set { entryLink = value; }
        }

        //////////////////////////////////////////////////////////////////////
        // Date: 7/3/2006 
        /// <summary>accessor method public bool Specialized </summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool Specialized 
        {
            get {return this.isSpecialized;}
            set {this.isSpecialized = value;}
        }
        /// <summary> holds the value property</summary>
        protected string value;
        /// <summary>
        ///  Accessor method for the Value property
        /// </summary>
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }


        #region Recurrence Parser
        //////////////////////////////////////////////////////////////////////
        /// <summary>parses an xml node to create a Recurrence object</summary> 
        /// <param name="node">Recurrence node</param>
        /// <param name="parser">AtomFeedParser to use</param>
        /// <returns> the created Recurrence object</returns>
        //////////////////////////////////////////////////////////////////////
        public static RecurrenceException ParseRecurrenceException(XmlNode node, AtomFeedParser parser)
        {
            Tracing.TraceCall();
            RecurrenceException exception  = null;

            Tracing.Assert(node != null, "node should not be null");

            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            object localname = node.LocalName;
            if (localname.Equals(GDataParserNameTable.XmlRecurrenceExceptionElement))
            {
                exception = new RecurrenceException(); 
                if (node.Attributes != null)
                {
                    if (node.Attributes[GDataParserNameTable.XmlAttributeSpecialized] != null)
                    {
                        exception.Specialized = bool.Parse(node.Attributes[GDataParserNameTable.XmlAttributeSpecialized].Value); 
                    }
                }

                if (node.HasChildNodes)
                {
                    XmlNode childNode = node.FirstChild;
                    while (childNode != null && childNode is XmlElement)
                    {
                        if (childNode.LocalName == GDataParserNameTable.XmlEntryLinkElement)
                        {
                            exception.EntryLink = EntryLink.ParseEntryLink(childNode, parser); 
                        }
                        childNode = childNode.NextSibling;
                    }
                }
            }

            if (exception.EntryLink == null)
            {
                throw new ArgumentException("g:recurringException/entryLink is required.");
            }
            return exception; 
        }



        #endregion

        #region overloaded for persistence

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return GDataParserNameTable.XmlRecurrenceExceptionElement; }
        }


        /// <summary>
        /// Persistence method for the RecurrenceException object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement(BaseNameTable.gDataPrefix, XmlName, BaseNameTable.gNamespace);
            writer.WriteEndElement();
        }
        #endregion
    }
}
