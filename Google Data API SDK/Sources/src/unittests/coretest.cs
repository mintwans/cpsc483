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
#define USE_TRACING
#define DEBUG

using System;
using System.IO;
using System.Xml; 
using System.Collections;
using System.Configuration;
using System.Net;
using System.Web;
using NUnit.Framework;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.Calendar;




namespace Google.GData.Client.UnitTests
{
    [TestFixture]
    public class CoreTestSuite : BaseTestClass
    {
        //////////////////////////////////////////////////////////////////////
        /// <summary>default empty constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public CoreTestSuite()
        {
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>[Test] public QueryObjectTest()</summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void QueryObjectTest()
        {
            Tracing.TraceInfo("Entering QueryObject Test"); 

            FeedQuery query = new FeedQuery();
            query.Uri = new Uri(this.defaultHost);

            AtomCategory aCat = new AtomCategory("Test", new AtomUri("urn:test.com")); 

            QueryCategory qCat = new QueryCategory(aCat);

            query.Categories.Add(qCat);


            aCat = new AtomCategory("TestNotAndOr", new AtomUri("urn:test.com")); 
            qCat = new QueryCategory(aCat);
            qCat.Operator = QueryCategoryOperator.OR; 
            qCat.Excluded = true; 

            query.Categories.Add(qCat);


            aCat = new AtomCategory("ANDTHISONE", new AtomUri("")); 
            qCat = new QueryCategory(aCat);
            query.Categories.Add(qCat);

            aCat = new AtomCategory("AnotherOrWithoutCategory"); 
            qCat = new QueryCategory(aCat);
            qCat.Operator = QueryCategoryOperator.OR; 
            qCat.Excluded = true; 
            query.Categories.Add(qCat);

            query.Query = "Hospital";
            query.NumberToRetrieve = 20; 
            Tracing.TraceInfo("query: "  + query.Uri);  

            Uri uri =  query.Uri; 

            Tracing.TraceInfo("Uri: query= " + uri.Query); 
            query.Uri = uri; 

            Tracing.TraceInfo("Parsed Query URI: " + query.Uri); 

            Assert.IsTrue(uri.AbsolutePath.Equals(query.Uri.AbsolutePath), "both query URIs should be identical"); 
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>[Test] creates a feed object from scratch</summary> 
        //////////////////////////////////////////////////////////////////////
        [Test]public void CreateFeed() 
        {
            Tracing.TraceInfo("Entering Create Feed Test"); 
            AtomFeed feed = new AtomFeed(new Uri("http://dummy"), null);

            AtomEntry entry; 

            for (int i = 1; i <= this.iIterations; i++)
            {
                entry = ObjectModelHelper.CreateAtomEntry(i);
                feed.Entries.Add(entry);
            }

            Tracing.TraceInfo("now persisting feed"); 

            ObjectModelHelper.DumpAtomObject(feed, CreateDumpFileName("CreateFeed"));

            Tracing.TraceInfo("now loadiing feed from disk");

            Service service = new Service();
            service.RequestFactory = this.factory; 

            FeedQuery query = new FeedQuery();
            query.Uri = new Uri(CreateUriFileName("CreateFeed"));

            feed = service.Query(query);

            Assert.IsTrue(feed.Entries != null, "Feed.Entries should not be null");
            Assert.AreEqual(this.iIterations, feed.Entries.Count, "Feed.Entries should have 50 elements");
            if (feed.Entries != null)
            {
                for (int i = 1; i <= this.iIterations; i++)
                {
                    entry = ObjectModelHelper.CreateAtomEntry(i);
                    AtomEntry theOtherEntry = feed.Entries[i-1];
                    Assert.IsTrue(ObjectModelHelper.IsEntryIdentical(entry, theOtherEntry));
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>[Test] creates a new entry, saves and loads it back</summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void CreateEntrySaveAndLoad()
        {

            Tracing.TraceMsg("Entering Create/Save/Load test");

            AtomEntry entry = ObjectModelHelper.CreateAtomEntry(1);

            ObjectModelHelper.DumpAtomObject(entry, CreateDumpFileName("CreateEntrySaveAndLoad"));


            // let's try loading this... 
            Service service = new Service();
            service.RequestFactory = this.factory; 

            FeedQuery query = new FeedQuery();
            query.Uri = new Uri(CreateUriFileName("CreateEntrySaveAndLoad"));
            AtomFeed feed = service.Query(query);
            Assert.IsTrue(feed.Entries != null, "Feed.Entries should not be null");
            Assert.AreEqual(1, feed.Entries.Count, "Feed.Entries should have ONE element");
            // that feed should have ONE entry
            if (feed.Entries != null)
            {
                AtomEntry theOtherEntry = feed.Entries[0];
                Assert.IsTrue(ObjectModelHelper.IsEntryIdentical(entry, theOtherEntry));
            }

        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>[Test] creates a new entry, saves and loads it back
        ///   uses HTML content to test the persistence/encoding code
        /// </summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void CreateHTMLEntrySaveAndLoad()
        {

            Tracing.TraceMsg("Entering CreateHTMLEntrySaveAndLoad");

            AtomEntry entry = ObjectModelHelper.CreateAtomEntry(1);
            entry.Content.Type = "html"; 
            entry.Content.Content = HttpUtility.HtmlDecode("<b>this is a &lt;test&gt;</b>"); 

            Tracing.TraceMsg("Content: " + entry.Content.Content);

            ObjectModelHelper.DumpAtomObject(entry, CreateDumpFileName("CreateHTMLEntrySaveAndLoad"));


            // let's try loading this... 
            Service service = new Service();
            service.RequestFactory = this.factory; 

            FeedQuery query = new FeedQuery();
            query.Uri = new Uri(CreateUriFileName("CreateHTMLEntrySaveAndLoad"));
            AtomFeed feed = service.Query(query);
            Assert.IsTrue(feed.Entries != null, "Feed.Entries should not be null");
            Assert.AreEqual(1, feed.Entries.Count, "Feed.Entries should have ONE element");
            // that feed should have ONE entry
            if (feed.Entries != null)
            {
                AtomEntry theOtherEntry = feed.Entries[0];
                Tracing.TraceMsg("Loaded Content: " + theOtherEntry.Content.Content);
                Assert.IsTrue(ObjectModelHelper.IsEntryIdentical(entry, theOtherEntry));
            }

        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>[Test] creates a new entry, saves and loads it back
        ///   uses XHTML content to test the persistence/encoding code
        /// </summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void CreateXHTMLEntrySaveAndLoad()
        {

            Tracing.TraceMsg("Entering CreateXHTMLEntrySaveAndLoad");

            AtomEntry entry = ObjectModelHelper.CreateAtomEntry(1);
            entry.Content.Type = "xhtml"; 
            entry.Content.Content = HttpUtility.HtmlDecode("<div xmlns=\"http://www.w3.org/2005/Atom\"><b>this is a test</b></div>"); 

            Tracing.TraceMsg("Content: " + entry.Content.Content);

            ObjectModelHelper.DumpAtomObject(entry, CreateDumpFileName("CreateXHTMLEntrySaveAndLoad"));

            Tracing.TraceMsg("saved in... CreateXHTMLEntrySaveAndLoad");


            // let's try loading this... 
            Service service = new Service();
            service.RequestFactory = this.factory; 

            FeedQuery query = new FeedQuery();
            query.Uri = new Uri(CreateUriFileName("CreateXHTMLEntrySaveAndLoad"));
            AtomFeed feed = service.Query(query);

            Tracing.TraceMsg("loaded in... CreateXHTMLEntrySaveAndLoad");

            Assert.IsTrue(feed.Entries != null, "Feed.Entries should not be null");
            Assert.AreEqual(1, feed.Entries.Count, "Feed.Entries should have ONE element");


            // that feed should have ONE entry
            if (feed.Entries != null)
            {
                Tracing.TraceMsg("checking entries... CreateXHTMLEntrySaveAndLoad");
                AtomEntry theOtherEntry = feed.Entries[0];

                Assert.IsTrue(theOtherEntry.Content != null, "the entry should have a content element");
                Assert.IsTrue(theOtherEntry.Content.Type.Equals("xhtml"), "the entry should have a content element of type xhtml");
                Assert.IsTrue(theOtherEntry.Content.Content != null, "the entry should have a content element that is not empty");

                Tracing.TraceMsg("Loaded Content: " + theOtherEntry.Content.Content);
                Assert.IsTrue(ObjectModelHelper.IsEntryIdentical(entry, theOtherEntry));
                Tracing.TraceMsg("done comparing entries... CreateXHTMLEntrySaveAndLoad");
            }

        }
        /////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////
        /// <summary>[Test] creates a new feed, saves and loads it back</summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void CreateFeedObjectSaveAndLoad()
        {

            Tracing.TraceMsg("Entering CreateFeedObjectSaveAndLoad test");

            Service service = new Service(); 
            AtomFeed feed=new AtomFeed(new Uri("http://www.atomfeed.com/"),service);
            feed.Self="http://www.atomfeed.com/self";
            feed.Feed="http://www.atomfeed.com/feed";
            feed.NextChunk="http://www.atomfeed.com/next";
            feed.PrevChunk="http://www.atomfeed.com/prev";
            feed.Post = "http://www.atomfeed.com/post"; 

            ObjectModelHelper.DumpAtomObject(feed, CreateDumpFileName("CreateFeedSaveAndLoad"));


            // let's try loading this... 
            service.RequestFactory = this.factory; 

            FeedQuery query = new FeedQuery();
            query.Uri = new Uri(CreateUriFileName("CreateFeedSaveAndLoad"));

            feed = service.Query(query);


            Assert.AreEqual("http://www.atomfeed.com/self", feed.Self, "Feed.Self is not correct");
            Assert.AreEqual("http://www.atomfeed.com/feed", feed.Feed, "Feed.Feed is not correct");
            Assert.AreEqual("http://www.atomfeed.com/next", feed.NextChunk, "Feed.Next is not correct");
            Assert.AreEqual("http://www.atomfeed.com/prev", feed.PrevChunk, "Feed.Prev is not correct");
            Assert.AreEqual("http://www.atomfeed.com/post", feed.Post, "Feed.Post is not correct");

        }
        /////////////////////////////////////////////////////////////////////////////

        


        ////////////////////////////////////////////////////////////////////
        /// <summary>[Test] creates a new entry, saves and loads it back</summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void CreateEmptyEntrySaveAndLoad()
        {

            Tracing.TraceMsg("Entering Create/Save/Load test");

            AtomEntry entry = ObjectModelHelper.CreateAtomEntry(1);

            entry.Content.Type = "text";
            entry.Content.Content = ""; 

            ObjectModelHelper.DumpAtomObject(entry, CreateDumpFileName("CreateEmptyEntrySaveAndLoad"));


            // let's try loading this... 
            Service service = new Service();
            service.RequestFactory = this.factory; 

            FeedQuery query = new FeedQuery();
            query.Uri = new Uri(CreateUriFileName("CreateEmptyEntrySaveAndLoad"));
            AtomFeed feed = service.Query(query);
            Assert.IsTrue(feed.Entries != null, "Feed.Entries should not be null");
            Assert.AreEqual(1, feed.Entries.Count, "Feed.Entries should have ONE element");
            // that feed should have ONE entry
            if (feed.Entries != null)
            {
                AtomEntry theOtherEntry = feed.Entries[0];
                Assert.IsTrue(ObjectModelHelper.IsEntryIdentical(entry, theOtherEntry), "Entries should be identical");
            }

        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>[Test] queries the remote feed, saves it, loads it and compares it</summary> 
        /// <param name="uriToQuery">the host to access, including query parameters</param>
        //////////////////////////////////////////////////////////////////////
        internal void RemoteHostQueryAndCompare(Uri uriToQuery)
        {

            Tracing.TraceMsg("Entering RemoteHostQueryAndCompare");

            int iCount = 0; 
            FeedQuery query = new FeedQuery();
            query.Uri = uriToQuery; 

            Service service = new Service();
            service.RequestFactory = this.factory; 

            AtomFeed f = service.Query(query);

            ObjectModelHelper.DumpAtomObject(f, CreateDumpFileName("QueryRemoteHost"));

            iCount = f.Entries.Count;

            // let's try loading this... 
            Service service2 = new Service();
            FeedQuery query2 = new FeedQuery();
            query2.Uri = new Uri(CreateUriFileName("QueryRemoteHost"));

            AtomFeed feed = service2.Query(query);
            Assert.AreEqual(iCount, feed.Entries.Count, "loaded feed has different number of entries");


            Tracing.TraceInfo("Comparing feed objects as source"); 
            Assert.IsTrue(ObjectModelHelper.IsSourceIdentical(f, feed), "Feeds are not identical"); 
            if (feed.Entries != null)
            {
                AtomEntry theOtherEntry;

                Tracing.TraceInfo("Comparing Entries"); 
                for (int i = 0; i < feed.Entries.Count; i++)
                {
                    theOtherEntry = feed.Entries[i];
                    Assert.IsTrue(ObjectModelHelper.IsEntryIdentical(f.Entries[i], theOtherEntry), "Entries are not identical");
                }

            }

            Tracing.TraceInfo("Leaving RemoteHostQueryAndCompare for : " + uriToQuery.AbsoluteUri); 

        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>[Test] walks over the list of remotehosts out of the 
        /// unitTestExternalHosts
        /// add key="Host1" value="http://www.franklinmint.fm/2005/09/26/test_entry2.xml" 
        /// section in the config file and queries and compares the object model
        /// </summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void RemoteHostQueryTest()
        {
            Tracing.TraceInfo("Entering RemoteHostQueryTest()"); 
            if (this.externalHosts != null)
            {
                for (int i=0; i< this.iIterations; i++) 
                {
                    Tracing.TraceInfo("Having a dictionary RemoteHostQueryTest()"); 
                    foreach (DictionaryEntry de in this.externalHosts )
                    {
                        Tracing.TraceInfo("Using DictionaryEntry for external Query: " + de.Value); 
                        Uri uriToQuery = new Uri((string) de.Value); 
                        RemoteHostQueryAndCompare(uriToQuery); 
    
                    }
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>creates a number or rows and delets them again</summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void DefaultHostExtensionTest()
        {
            Tracing.TraceMsg("Entering DefaultHostExtensionTest");

            if (this.strRemoteHost != null) 
            {

                FeedQuery query = new FeedQuery();
                Service service = new Service();

                service.NewAtomEntry += new FeedParserEventHandler(this.OnParsedNewEntry); 
                service.NewExtensionElement += new ExtensionElementEventHandler(this.OnNewExtensionElement);


                service.RequestFactory  =  (IGDataRequestFactory) new GDataLoggingRequestFactory(this.ServiceName, this.ApplicationName); 

                query.Uri = new Uri(this.strRemoteHost);

                AtomFeed returnFeed = service.Query(query);

                ObjectModelHelper.DumpAtomObject(returnFeed,CreateDumpFileName("ExtensionFeed")); 
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>tests the tokenizer collection</summary> 
        //////////////////////////////////////////////////////////////////////
        [Test] public void TestTokenCollection() 
        {
            Tracing.TraceMsg("Entering TestTokenCollection");
            String toTest = "Test=Test?other=whatever\nTest2=Line2?other=whatishere";

            TokenCollection tokens = new TokenCollection(toTest, new char[] {'\n', '='});
            TokenCollection tokenSmart = new TokenCollection(toTest, '=', true, 2);

            int iTokens = 0;
            foreach (string token in tokens)
            {
                // tokens should have 5 tokens, as the = signs split into 5
                iTokens++;
                if (iTokens == 1)
                {
                    Assert.IsTrue(token.Equals("Test"), "The first token should be Test, but it is: " + token);
                }
                if (iTokens == 4)
                {
                    Assert.IsTrue(token.Equals("Test2"), "The fourth token should be Test2 but it is: " + token);
                }
            }

            iTokens = 0;
            foreach (string token in tokenSmart)
            {
                // tokens should have 5 tokens, as the = signs split into 5
                iTokens++;
                if (iTokens == 1)
                {
                    Assert.IsTrue(token.Equals("Test"), "The first smart token should be Test, but it is: " + token);
                }
                if (iTokens == 4)
                {
                    Assert.IsTrue(token.Equals("Line2?other=whatishere"), "The fourth smart token should be whatishere, but it is: " + token);
                }
            }



        }
  
    } /// end of CoreTestSuite
}




