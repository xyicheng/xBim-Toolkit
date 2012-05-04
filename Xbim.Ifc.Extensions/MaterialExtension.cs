﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    MaterialExtension.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc.MaterialPropertyResource;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class MaterialExtension
    {
        public static void SetExtendedSingleValue(this IfcMaterial material, IModel model, string pSetName,
                                                  string propertyName, IfcValue value)
        {
            IfcExtendedMaterialProperties pSet = GetExtendedProperties(material, model, pSetName) ??
                                                 model.New<IfcExtendedMaterialProperties>(ps =>
                                                                                              {
                                                                                                  ps.Material = material;
                                                                                                  ps.Name = pSetName;
                                                                                              });
            IfcPropertySingleValue singleValue = GetExtendedSingleValue(material, model, pSetName, propertyName);
            if (singleValue == null)
            {
                singleValue = model.New<IfcPropertySingleValue>(sv =>
                                                                    {
                                                                        sv.Name = propertyName;
                                                                        sv.NominalValue = value;
                                                                    });
                pSet.ExtendedProperties.Add_Reversible(singleValue);
            }
        }

        public static void SetExtendedSingleValue(this IfcMaterial material, string pSetName, string propertyName,
                                                  IfcValue value)
        {
            IModel model = ModelManager.ModelOf(material);
            SetExtendedSingleValue(material, model, pSetName, propertyName, value);
        }

        public static IfcPropertySingleValue GetExtendedSingleValue(this IfcMaterial material, IModel model,
                                                                    string pSetName, string propertyName)
        {
            IfcExtendedMaterialProperties pSet = GetExtendedProperties(material, model, pSetName);
            if (pSet == null) return null;

            IfcPropertySingleValue result =
                pSet.ExtendedProperties.Where<IfcPropertySingleValue>(sv => sv.Name == propertyName).FirstOrDefault();
            return result;
        }

        public static IfcPropertySingleValue GetExtendedSingleValue(this IfcMaterial material, string pSetName,
                                                                    string propertyName)
        {
            IModel model = ModelManager.ModelOf(material);
            return GetExtendedSingleValue(material, model, pSetName, propertyName);
        }

        public static IfcValue GetExtendedSingleValueValue(this IfcMaterial material, IModel model, string pSetName,
                                                           string propertyName)
        {
            IfcExtendedMaterialProperties pSet = GetExtendedProperties(material, model, pSetName);
            if (pSet == null) return null;

            IfcPropertySingleValue singleValue = GetExtendedSingleValue(material, model, pSetName, propertyName);
            if (singleValue == null) return null;

            IfcValue result = singleValue.NominalValue;
            return result;
        }

        public static IfcValue GetExtendedSingleValueValue(this IfcMaterial material, string pSetName,
                                                           string propertyName)
        {
            IModel model = ModelManager.ModelOf(material);
            return GetExtendedSingleValueValue(material, model, pSetName, propertyName);
        }

        public static void DeleteExtendedSingleValue(this IfcMaterial material, IModel model, string pSetName,
                                                     string propertyName)
        {
            IfcExtendedMaterialProperties pSet = GetExtendedProperties(material, model, pSetName);
            if (pSet == null) return;

            IfcPropertySingleValue singleValue = GetExtendedSingleValue(material, model, pSetName, propertyName);
            if (singleValue == null) return;

            singleValue.NominalValue = null;
        }

        public static void DeleteExtendedSingleValue(this IfcMaterial material, string pSetName, string propertyName)
        {
            IModel model = ModelManager.ModelOf(material);
            DeleteExtendedSingleValue(material, model, pSetName, propertyName);
        }

        public static IfcExtendedMaterialProperties GetExtendedProperties(this IfcMaterial material, IModel model,
                                                                          string pSetName)
        {
            IfcExtendedMaterialProperties result =
                model.InstancesWhere<IfcExtendedMaterialProperties>(
                    pSet => pSet.Name == pSetName && pSet.Material == material).FirstOrDefault();
            IEnumerable<IfcExtendedMaterialProperties> resultTemp =
                model.InstancesOfType<IfcExtendedMaterialProperties>();
            return result;
        }

        public static IfcExtendedMaterialProperties GetExtendedProperties(this IfcMaterial material, string pSetName)
        {
            IModel model = ModelManager.ModelOf(material);
            return GetExtendedProperties(material, model, pSetName);
        }

        public static List<IfcExtendedMaterialProperties> GetAllExtendedPropertySets(this IfcMaterial material,
                                                                                     IModel model)
        {
            return model.InstancesWhere<IfcExtendedMaterialProperties>(pSet => pSet.Material == material).ToList();
        }

        public static List<IfcExtendedMaterialProperties> GetAllPropertySets(this IfcMaterial material)
        {
            IModel model = (material as IPersistIfcEntity).ModelOf;
            return model.InstancesWhere<IfcExtendedMaterialProperties>(pset => pset.Material == material).ToList();
        }

        public static Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> GetAllPropertySingleValues(
            this IfcMaterial material)
        {
            IModel model = ModelManager.ModelOf(material);
            return GetAllPropertySingleValues(material, model);
        }

        public static Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> GetAllPropertySingleValues(
            this IfcMaterial material, IModel model)
        {
            IEnumerable<IfcExtendedMaterialProperties> pSets =
                model.InstancesWhere<IfcExtendedMaterialProperties>(pset => pset.Material == material);
            Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> result =
                new Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>>();

            foreach (IfcExtendedMaterialProperties pSet in pSets)
            {
                Dictionary<IfcIdentifier, IfcValue> value = new Dictionary<IfcIdentifier, IfcValue>();
                IfcLabel psetName = pSet.Name;
                foreach (IfcProperty prop in pSet.ExtendedProperties)
                {
                    IfcPropertySingleValue singleVal = prop as IfcPropertySingleValue;
                    if (singleVal == null) continue;
                    value.Add(prop.Name, singleVal.NominalValue);
                }
                result.Add(psetName, value);
            }
            return result;
        }
    }
}