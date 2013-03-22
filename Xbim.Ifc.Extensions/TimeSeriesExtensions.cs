﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.TimeSeriesResource;

namespace Xbim.Ifc.Extensions
{
    public static class TimeSeriesExtensions
    {

        public static string GetAsString(this IfcTimeSeries ifcTimeSeries)
        {
            StringBuilder timeSeries = new StringBuilder();
            string start = ifcTimeSeries.StartTime.GetAsString();
            if (!string.IsNullOrEmpty(start))
            {
                timeSeries.Append("Start:");
                timeSeries.Append(start);
                timeSeries.Append(", ");
            }
            string end = ifcTimeSeries.EndTime.GetAsString();
            if (!string.IsNullOrEmpty(end))
            {
                timeSeries.Append("End:");
                timeSeries.Append(end);
                timeSeries.Append(", ");
            }

            if (ifcTimeSeries is IfcRegularTimeSeries)
            {
                IfcRegularTimeSeries ifcRegularTimeSeries = (ifcTimeSeries as IfcRegularTimeSeries);
                timeSeries.Append("TimeStep:");
                timeSeries.Append(string.Format("{0,0:N2}", ifcRegularTimeSeries.TimeStep.Value));
                timeSeries.Append(", ");

                //values are private?

            }
            if (ifcTimeSeries is IfcIrregularTimeSeries)
            {
                //values are private?
            }

            return timeSeries.ToString();
        }

    }
}
