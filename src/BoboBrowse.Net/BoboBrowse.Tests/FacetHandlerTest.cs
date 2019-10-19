﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 3.2.0
namespace BoboBrowse.Tests
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class FacetHandlerTest
    {
        private Directory _ramDir;
        private class NoopFacetHandler : FacetHandler<FacetDataNone>
        {
            public NoopFacetHandler(string name)
                : base(name)
            {
            }

            public NoopFacetHandler(string name, IEnumerable<string> dependsOn)
                : base(name, dependsOn)
            {
            }

            public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> selectionProperty)
            {
                return null;
            }

            public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
            {
                return null;
            }

            public override string[] GetFieldValues(BoboIndexReader reader, int id)
            {
                return null;
            }

            public override DocComparatorSource GetDocComparatorSource()
            {
                return null;
            }

            public override FacetDataNone Load(BoboIndexReader reader)
            {
                return null;
            }

            public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
            {
                return null;
            }
        }

        public FacetHandlerTest()
        {
            _ramDir = new RAMDirectory();
            try
            {
                using (var writer = new IndexWriter(_ramDir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), IndexWriter.MaxFieldLength.UNLIMITED))
                {
                }
            }
            catch
            {
                Assert.Fail("unable to load test");
            }
        }

        [Test]
        public void TestFacetHandlerLoad()
        {
            var reader = IndexReader.Open(_ramDir, true);

            var list = new List<IFacetHandler>();
            var h1 = new NoopFacetHandler("A");
            list.Add(h1);

            var s2 = new HashSet<string>();
            s2.Add("A");
            s2.Add("C");
            s2.Add("D");
            var h2 = new NoopFacetHandler("B", s2);
            list.Add(h2);

            var s3 = new HashSet<string>();
            s3.Add("A");
            s2.Add("D");
            var h3 = new NoopFacetHandler("C", s3);
            list.Add(h3);

            var s4 = new HashSet<string>();
            s4.Add("A");
            var h4 = new NoopFacetHandler("D", s4);
            list.Add(h4);

            var s5 = new HashSet<string>();
            s5.Add("E");
            var h5 = new NoopFacetHandler("E", s5);
            list.Add(h5);


            using (var boboReader = BoboIndexReader.GetInstance(reader, list, null))
            {

                using (var browser = new BoboBrowser(boboReader))
                {
                    var s6 = new HashSet<string>();
                    s6.Add("A");
                    s6.Add("B");
                    s6.Add("C");
                    s6.Add("D");
                    browser.SetFacetHandler(new NoopFacetHandler("runtime", s6));

                    var expected = new HashSet<string>();
                    expected.Add("A");
                    expected.Add("B");
                    expected.Add("C");
                    expected.Add("D");
                    expected.Add("E");
                    expected.Add("runtime");

                    var facetsLoaded = browser.FacetNames;

                    foreach (var name in facetsLoaded)
                    {
                        if (expected.Contains(name))
                        {
                            expected.Remove(name);
                        }
                        else
                        {
                            Assert.Fail(name + " is not in expected set.");
                        }
                    }

                    if (expected.Count > 0)
                    {
                        Assert.Fail("some facets not loaded: " + string.Join(", ", expected.ToArray()));
                    }
                }
            }
        }

        [Test]
        public void TestNegativeLoad()
        {
            var reader = IndexReader.Open(_ramDir, true);

            var list = new List<IFacetHandler>();
            var s1 = new HashSet<string>();
            s1.Add("E");
            var h1 = new NoopFacetHandler("A", s1);
            list.Add(h1);

            var s2 = new HashSet<string>();
            s2.Add("A");
            s2.Add("C");
            s2.Add("D");
            var h2 = new NoopFacetHandler("B", s2);
            list.Add(h2);

            var s3 = new HashSet<string>();
            s3.Add("A");
            s2.Add("D");
            var h3 = new NoopFacetHandler("C", s3);
            list.Add(h3);

            var s4 = new HashSet<string>();
            s4.Add("A");
            var h4 = new NoopFacetHandler("D", s4);
            list.Add(h4);

            var s5 = new HashSet<string>();
            s5.Add("E");
            var h5 = new NoopFacetHandler("E", s5);
            list.Add(h5);

            using (var boboReader = BoboIndexReader.GetInstance(reader, list, null))
            {

                using (var browser = new BoboBrowser(boboReader))
                {
                    var expected = new HashSet<string>();
                    expected.Add("A");
                    expected.Add("B");
                    expected.Add("C");
                    expected.Add("D");
                    expected.Add("E");

                    var facetsLoaded = browser.FacetNames;

                    foreach (var name in facetsLoaded)
                    {
                        if (expected.Contains(name))
                        {
                            expected.Remove(name);
                        }
                        else
                        {
                            Assert.Fail(name + " is not in expected set.");
                        }
                    }

                    if (expected.Count > 0)
                    {
                        if (expected.Count == 4)
                        {
                            expected.Remove("A");
                            expected.Remove("B");
                            expected.Remove("C");
                            expected.Remove("D");
                            if (expected.Count > 0)
                            {
                                Assert.Fail("some facets not loaded: " + string.Join(", ", expected.ToArray()));
                            }
                        }
                        else
                        {
                            Assert.Fail("incorrect number of left over facets: " + string.Join(", ", expected.ToArray()));
                        }
                    }
                }
            }
        }
    }
}
