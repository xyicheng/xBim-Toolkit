using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Common.Logging
{
    public class Event
    {
        public DateTime EventTime { get; internal set; }
        public String EventLevel { get; internal set; }
        public String Message { get; internal set; }
        public String User { get; internal set; }
        public String Logger { get; internal set; }
        public String Method { get; internal set; }
    }
}
