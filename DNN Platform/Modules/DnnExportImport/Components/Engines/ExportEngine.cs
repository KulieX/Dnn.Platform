﻿#region Copyright
//
// DotNetNuke® - http://www.dnnsoftware.com
// Copyright (c) 2002-2017
// by DotNetNuke Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Dnn.ExportImport.Components.Dto;
using Dnn.ExportImport.Components.Entities;
using Dnn.ExportImport.Components.Interfaces;
using Dnn.ExportImport.Components.Models;
using DotNetNuke.Framework.Reflections;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Scheduling;
using Newtonsoft.Json;

namespace Dnn.ExportImport.Components.Engines
{
    public class ExportImportEngine
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(ExportImportEngine));

        public int ProgressPercentage
        {
            get
            {
                //TODO:
                throw new NotImplementedException();
            }
        }

        public ExportImportResult Export(ExportImportJob exportJob, ScheduleHistoryItem scheduleHistoryItem)
        {
            var exportDto = JsonConvert.DeserializeObject<ExportDto>(exportJob.JobObject);
            if (exportDto == null)
            {
                exportJob.CompletedOn = DateTime.UtcNow;
                exportJob.JobStatus = JobStatus.DoneFailure;
                return null; //TODO: return result
            }

            if (!exportDto.ItemsToExport.Any())
            {
                exportJob.CompletedOn = DateTime.UtcNow;
                exportJob.JobStatus = JobStatus.DoneFailure;
                scheduleHistoryItem.AddLogNote("<br/>No items selected for exporting");
                return null; //TODO: return result
            }

            var implementors = GetPortableImplementors().OrderBy(x => x.Priority).ToArray();
            foreach (var portable2Obj in implementors)
            {
                var selected = exportDto.ItemsToExport.FirstOrDefault(
                    x => x.Equals(portable2Obj.Category, StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrEmpty(selected))
                {
                    continue;
                }

                portable2Obj.ExportData(exportJob);
                //TODO:
                return null;
            }

            //TODO: export pages

            exportJob.JobStatus = JobStatus.InProgress;
            return null; //TODO: return result
        }

        public ExportImportResult Import(ExportImportJob importJob, ScheduleHistoryItem scheduleHistoryItem)
        {
            var importDto = JsonConvert.DeserializeObject<ImportDto>(importJob.JobObject);
            if (importDto == null)
            {
                importJob.CompletedOn = DateTime.UtcNow;
                importJob.JobStatus = JobStatus.DoneFailure;
            }

            foreach (var portable2Object in GetPortableImplementors())
            {
                //TODO: select items from database and if any then
            }

            importJob.JobStatus = JobStatus.InProgress;
            throw new NotImplementedException();
        }

        private static IEnumerable<IPortable2> GetPortableImplementors()
        {
            var types = GetAllAppStartEventTypes();

            foreach (var type in types)
            {
                IPortable2 portable2Type;
                try
                {
                    portable2Type = Activator.CreateInstance(type) as IPortable2;
                }
                catch (Exception e)
                {
                    Logger.ErrorFormat("Unable to create {0} while calling IPortable2 implementors. {1}",
                                       type.FullName, e.Message);
                    portable2Type = null;
                }

                if (portable2Type != null)
                {
                    yield return portable2Type;
                }
            }
        }

        private static IEnumerable<Type> GetAllAppStartEventTypes()
        {
            var typeLocator = new TypeLocator();
            return typeLocator.GetAllMatchingTypes(
                t => t != null && t.IsClass && !t.IsAbstract && t.IsVisible &&
                     typeof(IPortable2).IsAssignableFrom(t));
        }
    }
}