﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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

namespace BoboBrowse.Net.Client
{
    using BoboBrowse.Net.Impl;
    using BoboBrowse.Net.Service;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class BoboCmdlineApp : IDisposable
    {
        public enum ResultCodes
        {
            SUCCESS = 0,
            NO_INDEX_PROVIDED = 1,
            DIRECTORY_INVALID = 2,
            DIRECTORY_DOESNT_EXIST = 4
        }

        private readonly BrowseRequestBuilder _reqBuilder;
        private readonly IBrowseService _svc;

        public BoboCmdlineApp(IBrowseService svc)
        {
            _svc = svc;
            _reqBuilder = new BrowseRequestBuilder();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_svc != null)
            {
                _svc.Dispose();
            }
        }

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine(@"Index directory argument is required. Example: bobo.exe ""C:\IndexDirectory\"". Press a key to exit.");
                Console.ReadKey();
                return (int)ResultCodes.NO_INDEX_PROVIDED;
            }

            string path = string.Empty;

            try
            {
                path = Path.GetDirectoryName(Path.GetFullPath(args[0]));
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("ERROR: The directory doesn't exist. Press a key to exit.");
                    Console.ReadKey();
                    return (int)ResultCodes.DIRECTORY_DOESNT_EXIST;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(@"ERROR: Argument passed must be a valid directory or file name.  Press a key to exit." + 
                    Environment.NewLine + "Original Error: " + Environment.NewLine + e.ToString());
                Console.ReadKey();
                return (int)ResultCodes.DIRECTORY_INVALID;
            }

            DirectoryInfo idxDir = new DirectoryInfo(path);
            Console.WriteLine("Index directory: " + idxDir.FullName);
            Console.WriteLine("Welcome to the bobo console utility. Type 'help' to see a list of supported commands.");

            IBrowseService svc = new BrowseServiceImpl(idxDir);
            string line;

            using (var app = new BoboCmdlineApp(svc))
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine("bobo> ");

                        line = Console.ReadLine();
                        if (line.Equals("exit", StringComparison.OrdinalIgnoreCase) || line.Equals("q", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        app.ProcessCommand(line);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }

            return (int)ResultCodes.SUCCESS;
        }

        private void ProcessCommand(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            string[] parsed = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parsed.Length == 0) return;

            string cmd = parsed[0];

            string[] args = new string[parsed.Length - 1];
            if (args.Length > 0)
            {
                Array.Copy(parsed, 1, args, 0, args.Length);
            }

            if ("help".Equals(cmd, StringComparison.OrdinalIgnoreCase) || "?".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(@"help or ? - prints this message");
                Console.WriteLine(@"exit or q - quits");
                Console.WriteLine(@"query <query string> - sets query text");
                Console.WriteLine(@"facetspec <name>:<minHitCount>,<maxCount>,<isExpand>,<orderby> - add facet spec");
                Console.WriteLine(@"page <offset>:<count> - set paging parameters");
                Console.WriteLine(@"select <name>:<value1>,<value2>... - add selection, with ! in front of value indicates a not");
                Console.WriteLine(@"sort <name>:<dir>,... - set sort specs (false for ascending, true for descending)");
                Console.WriteLine(@"showReq: shows current request");
                Console.WriteLine(@"clear: clears current request");
                Console.WriteLine(@"clearSelections: clears all selections");
                Console.WriteLine(@"clearSelection <name>: clear selection specified");
                Console.WriteLine(@"clearFacetSpecs: clears all facet specs");
                Console.WriteLine(@"clearFacetSpec <name>: clears specified facetspec");
                Console.WriteLine(@"browse - executes a search");
            }
            else if ("query".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                if (parsed.Length < 2)
                {
                    Console.WriteLine(@"query not defined.");
                }
                else
                {
                    string queryString = parsed[1];
                    if (!string.IsNullOrEmpty(queryString))
                    {
                        var qparser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "contents", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
                        Query q;
                        try
                        {
                            q = qparser.Parse(queryString);
                            _reqBuilder.Request.Query = q;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                }
            }
            else if ("facetspec".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                if (parsed.Length < 2)
                {
                    Console.WriteLine("facetspec not defined.");
                }
                else
                {
                    try
                    {
                        string fspecString = parsed[1];
                        string[] parts = fspecString.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					    string name = parts[0];
					    string fvalue=parts[1];
                        string[] valParts = fvalue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					    if (valParts.Length != 4){
                            Console.WriteLine(@"spec must of of the form <minhitcount>,<maxcount>,<isExpand>,<orderby>");
					    }
					    else
                        {
                            int minHitCount = 1;
						    int maxCount = 5;
						    bool expand = false;
						    FacetSpec.FacetSortSpec sort = FacetSpec.FacetSortSpec.OrderHitsDesc;
						    try
                            {
						   	    minHitCount = int.Parse(valParts[0]);
						    }
						    catch
                            {
							    Console.WriteLine("default min hitcount = 1 is applied.");
						    }
						    try
                            {
							    maxCount = int.Parse(valParts[1]);
						    }
						    catch
                            {
							    Console.WriteLine("default maxCount = 5 is applied.");
						    }
						    try
                            {
							    expand = bool.Parse(valParts[2]);
						    }
						    catch
                            {
							    Console.WriteLine("default expand=false is applied.");
						    }

                            if ("hits".Equals(valParts[3]))
                            {
                                sort = FacetSpec.FacetSortSpec.OrderHitsDesc;
                            }
                            else
                            {
                                sort = FacetSpec.FacetSortSpec.OrderValueAsc;
                            }

                            _reqBuilder.ApplyFacetSpec(name, minHitCount, maxCount, expand, sort);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            else if ("select".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                if (parsed.Length < 2)
                {
				    Console.WriteLine("selection not defined.");
			    }
			    else
                {
                    try
                    {
                        string selString = parsed[1];
                        string[] parts = selString.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					    string name = parts[0];
					    string selList = parts[1];
                        string[] sels = selList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					    foreach (string sel in sels)
                        {
						    bool isNot = false;
						    string val = sel;
						    if (sel.StartsWith("!")){
							    isNot=true;
							    val = sel.Substring(1);
						    }
						    if (!string.IsNullOrEmpty(val))
                            {
							    _reqBuilder.AddSelection(name, val, isNot);
						    }
					    }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            else if ("page".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string pageString = parsed[1];
                    string[] parts = pageString.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    _reqBuilder.Offset = int.Parse(parts[0]);
                    _reqBuilder.Count = int.Parse(parts[1]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            else if ("clearFacetSpec".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                if (parsed.Length < 2)
                {
				    Console.WriteLine("facet spec not defined.");
			    }
			    else
                {
				    string name = parsed[1];
				    _reqBuilder.ClearFacetSpec(name);
			    }
            }
            else if ("clearSelection".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                if (parsed.Length < 2)
                {
                    Console.WriteLine("selection name not defined.");
                }
                else
                {
                    string name = parsed[1];
                    _reqBuilder.ClearSelection(name);
                }
            }
            else if ("clearSelections".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                _reqBuilder.ClearSelections();
            }
            else if ("clearFacetSpecs".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                _reqBuilder.ClearFacetSpecs();
            }
            else if ("clear".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                _reqBuilder.Clear();
            }
            else if ("showReq".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                BrowseRequest req = _reqBuilder.Request;
			    Console.WriteLine(req.ToString());
            }
            else if ("sort".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                if (parsed.Length == 2)
                {
				    string sortString = parsed[1];
                    string[] sorts = sortString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				    var sortList = new List<SortField>();
				    foreach (var sort in sorts)
                    {
                        string[] sortParams = sort.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					    bool rev = false;
					    if (sortParams.Length > 0)
                        {
                            string sortName = sortParams[0];
                            if (sortParams.Length > 1)
                            {
                                try
                                {
                                    rev = bool.Parse(sortParams[1]);
                                }
                                catch(Exception e)
                                {
                                    Console.WriteLine(e.Message + ", default rev to false");
                                }
                            }
                            sortList.Add(new SortField(sortName, SortField.STRING, rev));
					    }
				    }
				    _reqBuilder.ApplySort(sortList.ToArray());
			    }
			    else
                {
				    _reqBuilder.ApplySort(null);
			    }
            }
            else if ("browse".Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                BrowseRequest req = _reqBuilder.Request;
			
			    BrowseResult res = _svc.Browse(req);
			    string output = BrowseResultFormatter.FormatResults(res);
			    Console.WriteLine(output);
            }
            else
            {
                Console.WriteLine("Unknown command: " + cmd + ", do help for list of supported commands");
            }
        }
    }
}
