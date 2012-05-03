#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    ListRange.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

namespace Xbim.Presentation
{
    public struct ListRange
    {
        /// <summary>
        ///   start index of the range in the list
        /// </summary>
        public int Start;

        /// <summary>
        ///   Number of elements to include in the range
        /// </summary>
        public int Count;

        public int End
        {
            get { return Start + Count; }
        }
    }
}