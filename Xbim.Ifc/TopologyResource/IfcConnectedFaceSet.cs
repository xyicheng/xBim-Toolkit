#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcConnectedFaceSet.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.TopologyResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcConnectedFaceSet : IfcTopologicalRepresentationItem, IFaceBasedModel
    {
        private XbimSet<IfcFace> _cfsFaces;
        
        public IfcConnectedFaceSet()
        {
            _cfsFaces = new XbimSet<IfcFace>(this);
        }



        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The set of faces arcwise connected along common edges or vertices.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.Set, 1)]
        public XbimSet<IfcFace> CfsFaces
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _cfsFaces;
            }
            set { ModelManager.SetModelValue(this, ref _cfsFaces, value, v => CfsFaces = v, "CfsFaces"); }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            if (propIndex == 0)
            {
                _cfsFaces.Add((IfcFace) value.EntityVal);
            }
            else
                throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                  this.GetType().Name.ToUpper()));
        }

        #endregion

        public override string WhereRule()
        {
            return "";
        }

        #region IFaceBasedModel Members

        IEnumerable<IFace> IFaceBasedModel.Faces
        {
            get { return CfsFaces.Cast<IFace>(); }
        }

        #endregion


        public bool IsPolygonal
        {
            get
            {
                foreach (var face in _cfsFaces)
                {
                    if(!face.IsPolygonal)
                        return false;
                }
                return true;
            }
        }
    }
}