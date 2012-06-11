using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;

namespace Xbim.COBieExtensions
{
    public class COBieCICStage1
    {
        IModel _model;
        public COBieCICStage1(IModel model)
        {
            _model = model;
        }

        public COBieP01Registration P01_Registration
        {
            get
            {
                return new COBieP01Registration(_model);
            }
        }
    }
}
