#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcActor.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Xbim.Ifc2x3.ActorResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.Kernel
{
    public class ActorCollection : ObservableCollection<IfcActor>
    {
    }

    /// <summary>
    ///   The IfcActor defines all actors or human agents involved in a project during its full life cycle. 
    ///   It facilitates the use of person and organization definitions in the resource part of the IFC object model.
    /// </summary>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcActor : IfcObject
    {
        #region Constructors & Initialisers

        #endregion

        #region Fields

        private IfcActorSelect _theActor;

        #endregion

        #region Ifc Properties

        /// <summary>
        ///   Reference to the relationship that associates the actor to an object. Can be an Organization, Person or PersonOrganization
        /// </summary>

        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcActorSelect TheActor
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _theActor;
            }
            set
            {
                if (value == null || value is IfcPerson || value is IfcOrganization || value is IfcPersonAndOrganization)
                    this.SetModelValue(this, ref _theActor, value, v => TheActor = v, "TheActor");
                else
                    throw new ArgumentException(
                        "Illegal Actor type, must be Organization, Person,  PersonOrganization or null", "TheActor");
            }
        }

        #endregion

        #region Ifc Inverse Relationships

        /// <summary>
        ///   Information about the actor.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcRelAssignsToActor> IsActingUpon
        {
            get { return ModelOf.InstancesWhere<IfcRelAssignsToActor>(a => a.RelatingActor == this); }
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Returns the Actor as a Person, if it is a Person else null
        /// </summary>
        public IfcPerson TheActorAsPerson
        {
            get { return _theActor as IfcPerson; }
        }

        /// <summary>
        ///   Returns the Actor as a PersonAndOrganization, if it is a PersonAndOrganization else null
        /// </summary>
        public IfcPersonAndOrganization TheActorAsPersonAndOrganization
        {
            get { return _theActor as IfcPersonAndOrganization; }
        }

        /// <summary>
        ///   Returns the Actor as a PersonAndOrganization, if it is a PersonAndOrganization else null
        /// </summary>
        public IfcOrganization TheActorAsOrganization
        {
            get { return _theActor as IfcOrganization; }
        }

        #endregion
    }
}