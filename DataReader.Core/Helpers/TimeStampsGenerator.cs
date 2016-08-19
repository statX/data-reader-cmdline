﻿#region Copyright
// /*
// 
//    Copyright 2015 Patrice Thivierge Fortin
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//  
//  */
#endregion

using System;
using System.Collections.Generic;

namespace DataReader.Core
{
    public static class TimeStampsGenerator
    {

        public static List<DateTime> Get(TimeSpan interval, DateTime startTime, DateTime endTime)
        {

            var dates = new List<DateTime>();
            
            var currentTime = startTime;
            while (currentTime < endTime)
            {
                dates.Add(currentTime);
                currentTime = currentTime.AddSeconds(interval.TotalSeconds);
            }

            dates.Add(endTime);
            
            return dates;
        }

    }
}
