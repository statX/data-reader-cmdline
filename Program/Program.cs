#region Copyright

//  Copyright 2015 Patrice Thivierge Fortin
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using DataReader.Core;
using log4net;

namespace DataReader.CommandLine
{
    /// <summary>
    /// This program was built to make data read comparisons between different PI Data Archives
    /// ex: 
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {

            PIConnection piConnection;
            var _logger = LogManager.GetLogger(typeof(Program));
            var writer = Console.Out;

            try
            {

              

                var options = new CommandLineOptions();


                if (Parser.Default.ParseArguments(args, options))
                {

                    var readerSettings = new DataReaderSettings();
                    piConnection = new PIConnection(options.Server);

                    if (options.testTagSearch != null && options.testTagSearch.Length > 0)
                    {
                        _logger.Info("Search test started...");

                        piConnection.Connect();

                        var search = new TagsLoader(piConnection.GetPiServer());
                        foreach (var s in options.testTagSearch)
                        {
                            var tags = search.Search(s).ToList();
                            _logger.WarnFormat("Found {0} tags with query {1}", tags.Count, s);

                            if (options.testTagSearchPrintAllTags)
                            {
                                tags.ForEach(t => _logger.InfoFormat("Tag: {0}, PointClass: {1}", t.Name, t.PointClass));
                            }

                        }

                    }

                    if (options.TagQueries != null && options.TagQueries.Length > 0)
                    {

                        _logger.Info("Data reader starting...");

                        IDataReader dataReader;
                        piConnection.Connect();

                        if (options.EventsPerDay > 0 && options.TagsCount > 0)
                        {
                            var type = options.UseParallel? DataReaderSettings.ReadingType.Parallel: DataReaderSettings.ReadingType.Bulk;

                            readerSettings.AutoTune(type, options.EventsPerDay,options.TagsCount,options.EventsPerRead);
                        }
                        

                        // starts the data writer

                        _logger.Info("Creating worker objects...");
                        var dataWriter = new DataWriter(options.OutfileName,options.EventsPerFile);
                        var dataProcessor = new DataProcessor(options.EnableWrite, dataWriter);

                        dataReader = options.UseParallel
                            ? (IDataReader) new DataReaderParallel(readerSettings, dataProcessor)
                            : new DataReaderBulk(readerSettings, dataProcessor);

                        var orchestrator = new Orchestrator(options.StartTime, options.EndTime, readerSettings.TimeIntervalPerDataRequest, dataReader);
                        var tagsLoader = new TagsLoader(piConnection.GetPiServer(), options.TagQueries, readerSettings.TagGroupSize, orchestrator);
                        var statistics=new Statistics();

                        // starts the orchestrator
                        _logger.Info("Starting workers...");
                        var tagsLoaderTask = tagsLoader.Run();
                        var writerTask = dataWriter.Run();
                        var processorTask = dataProcessor.Run();
                        var orchestratorTask = orchestrator.Run();
                        var dataReaderTask = dataReader.Run();
                        var statsTask = statistics.Run();




                        // starts the data reader
                        Task.WaitAll(orchestratorTask, writerTask, dataReaderTask, tagsLoaderTask, processorTask);

                        statistics.Stop();

                        Task.WaitAll(statsTask);

                        _logger.Info("All tasks completed successfully");

                    }






                    // DEBUG
                    //  Console.ReadKey();

                    // exit ok
                    Environment.Exit(0);
                }
                else
                {
                    // exit with error
                    Environment.Exit(1);
                }
            }


            catch (Exception ex)
            {
                Console.SetOut(writer);
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}