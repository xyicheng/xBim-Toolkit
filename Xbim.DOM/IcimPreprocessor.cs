using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.DOM
{
    public class IcimPreprocessor
    {
        private IModel _model;
        private IModel Model
        {
            get { return _model; }
        }
        
        /// <summary>
        /// Preprocessing of the data for the ICIM
        /// </summary>
        /// <param name="document"></param>
        public IcimPreprocessor(IModel model)
        {
            _model = model;
        }

        /*
         * DOM rules 
         * - unique material names
         * - unique building element type names
         * - elements have their types
         * 
         *Analytical 
         *- get volume from geometry
         *- compute volumes of materials based on layers and volumes
         *- generate fresh owner history
         *- set default types for non-ICIM element types
         */

    }
}
