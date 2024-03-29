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
using System.IO;
using System.Collections;
using System.Text;
using System.Xml;
using Google.GData.Client;

namespace Google.GData.Extensions {

    /// <summary>
    /// GData schema extension describing a nested entry link.
    /// </summary>
    public class EntryLink : IExtensionElement
    {

        /// <summary>holds the href property of the EntryLink element</summary>
        protected string href;
        /// <summary>holds the readOnlySet property of the EntryLink element</summary>
        protected bool readOnly;
        /// <summary>holds the AtomEntry  property of the EntryLink element</summary>
        protected AtomEntry entry;
        /// <summary>holds the rel attribute of the EntyrLink element</summary> 
        protected string rel;

        private bool readOnlySet; 

        /// <summary>
        /// Entry  URI
        /// </summary>
        public string Href
        {
            get { return href; }
            set { href = value; }
        }

        /// <summary>
        /// Read only flag.
        /// </summary>
        public bool ReadOnly
        {
            get { return this.readOnly; }
            set { this.readOnly = value; this.readOnlySet = true; }
        }

        /// <summary>
        ///  Nested entry (optional).
        /// </summary>
        public AtomEntry Entry
        {
            get { return entry; }
            set { entry = value; }
        }

         //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Rel</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Rel
        {
            get {return this.rel;}
            set {this.rel = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        #region EntryLink Parser
        //////////////////////////////////////////////////////////////////////
        /// <summary>parses an xml node to create an EntryLink object</summary> 
        /// <param name="node">entrylink node</param>
        /// <param name="parser">AtomFeedParser to use</param>
        /// <returns> the created EntryLink object</returns>
        //////////////////////////////////////////////////////////////////////
        public static EntryLink ParseEntryLink(XmlNode node, AtomFeedParser parser)
        {
            Tracing.TraceCall();
            EntryLink link = null;
            Tracing.Assert(node != null, "node should not be null");
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            

            object localname = node.LocalName;
            if (localname.Equals(GDataParserNameTable.XmlEntryLinkElement))
            {
                link = new EntryLink();
                if (node.Attributes != null)
                {
                    if (node.Attributes[GDataParserNameTable.XmlAttributeHref] != null)
                    {
                        link.Href = node.Attributes[GDataParserNameTable.XmlAttributeHref].Value;
                    }

                    if (node.Attributes[GDataParserNameTable.XmlAttributeReadOnly] != null)
                    {
                        link.ReadOnly = node.Attributes[GDataParserNameTable.XmlAttributeReadOnly].Value.Equals(Utilities.XSDTrue);
                    }

                    if (node.Attributes[GDataParserNameTable.XmlAttributeRel] != null)
                    {
                        link.Rel = node.Attributes[GDataParserNameTable.XmlAttributeRel].Value;
                    }

                }

                if (node.HasChildNodes)
                {
                    XmlNode entryChild = node.FirstChild;

                    while (entryChild != null && entryChild is XmlElement)
                    {
                        if (entryChild.LocalName == AtomParserNameTable.XmlAtomEntryElement &&
                            entryChild.NamespaceURI == BaseNameTable.NSAtom)
                        {

                            if (link.Entry == null)
                            {
                                XmlReader reader = new XmlNodeReader(entryChild);
                                // move the reader to the first node
                                reader.Read();
                                parser.NewAtomEntry += new FeedParserEventHandler(link.OnParsedNewEntry); 
                                parser.ParseEntry(reader); 
                            }
                            else
                            {
                                throw new ArgumentException("Only one entry is allowed inside the g:entryLink");
                            }
                        }
                        entryChild = entryChild.NextSibling;
                    }
                }

            }

            return link;
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Event chaining. We catch this from the AtomFeedParser
        /// we want to set this to our property, and do not add the entry to the collection
        /// </summary> 
        /// <param name="sender"> the object which send the event</param>
        /// <param name="e">FeedParserEventArguments, holds the feed entry</param> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        internal void OnParsedNewEntry(object sender, FeedParserEventArgs e)
        {
            // by default, if our event chain is not hooked, add it to the collection
            Tracing.TraceCall("received new item notification");
            Tracing.Assert(e != null, "e should not be null");
            if (e == null)
            {
                throw new ArgumentNullException("e"); 
            }

            if (e.CreatingEntry == false)
            {
                if (e.Entry != null)
                {
                    // add it to the collection
                    Tracing.TraceMsg("\t new EventEntry found"); 
                    this.Entry = e.Entry; 
                    e.DiscardEntry = true; 
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////



        #endregion

        #region overloaded for persistence

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return GDataParserNameTable.XmlEntryLinkElement; }
        }

        /// <summary>
        /// Used to save the EntryLink instance into the passed in xmlwriter
        /// </summary>
        /// <param name="writer">the XmlWriter to write into</param>
        public void Save(XmlWriter writer)
        {
            if (Utilities.IsPersistable(this.Href) ||
                this.readOnlySet ||
                this.entry != null) 
            {
                writer.WriteStartElement(BaseNameTable.gDataPrefix, XmlName, BaseNameTable.gNamespace);
                if (Utilities.IsPersistable(this.Href))
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeHref, this.Href);
                }
                if (Utilities.IsPersistable(this.Rel))
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeRel, this.Rel);
                }
                if (this.readOnlySet)
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeReadOnly, 
                                                Utilities.ConvertBooleanToXSDString(this.ReadOnly));
                }
    
                if (entry != null)
                {
                    entry.SaveToXml(writer);
                }
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}
