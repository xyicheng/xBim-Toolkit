#region XbimHeader

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
using Xbim.Ifc2x3.MaterialPropertyResource;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.Extensions
{
    public static class MaterialExtension
    {
        
        public static void SetExtendedSingleValue(this IfcMaterial material, IModel model, string pSetName,
                                                  string propertyName, IfcValue value)
        {
            IfcExtendedMaterialProperties pSet = GetExtendedProperties(material, model, pSetName) ??
                                                 model.Instances.New<IfcExtendedMaterialProperties>(ps =>
                                                                                              {
                                                                                                  ps.Material = material;
                                                                                                  ps.Name = pSetName;
                                                                                              });
            IfcPropertySingleValue singleValue = GetExtendedSingleValue(material, model, pSetName, propertyName);
            if (singleValue == null)
            {
                singleValue = model.Instances.New<IfcPropertySingleValue>(sv =>
                                                                    {
                                                                        sv.Name = propertyName;
                                                                        sv.NominalValue = value;
                                                                    });
                pSet.ExtendedProperties.Add_Reversible(singleValue);
            }
            else
            {
                // CM 03/09/2013 - Added this as before hand if the property already existed nothing would be set
                singleValue.NominalValue = value;
            }
        }

        public static void SetExtendedSingleValue(this IfcMaterial material, string pSetName, string propertyName,
                                                  IfcValue value)
        {
            IModel model = material.ModelOf;
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
            IModel model = material.ModelOf;
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
            IModel model = material.ModelOf;
            return GetExtendedSingleValueValue(material, model, pSetName, propertyName);
        }

        /// <summary>
        /// CM 23/09/2013 - Created this to allow an Extended Materials set to be passed directly. 
        /// THis Saves on processing time which can be very slow when looping.
        /// </summary>
        /// <returns></returns>
        public static IfcValue GetExtendedPropertyNominalValue(this IfcMaterial material, IModel model,
                                                                    IfcExtendedMaterialProperties pSet, string propertyName)
        {
            if (pSet == null) return null;

            IfcPropertySingleValue result =
                pSet.ExtendedProperties.OfType<IfcPropertySingleValue>().Where(sv => sv.Name == propertyName).FirstOrDefault();

            if (result == null) return null;

            return result.NominalValue;
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
            IModel model = material.ModelOf;
            DeleteExtendedSingleValue(material, model, pSetName, propertyName);
        }

        public static IfcExtendedMaterialProperties GetExtendedProperties(this IfcMaterial material, IModel model,
                                                                          string pSetName)
        {
            IfcExtendedMaterialProperties result =
                model.Instances.Where<IfcExtendedMaterialProperties>(
                    pSet => pSet.Name == pSetName && pSet.Material == material).FirstOrDefault();

            //IEnumerable<IfcExtendedMaterialProperties> resultTemp =
            //    model.Instances.OfType<IfcExtendedMaterialProperties>();

            return result;
        }

        public static IfcExtendedMaterialProperties GetExtendedProperties(this IfcMaterial material, string pSetName)
        {
            IModel model = material.ModelOf;
            return GetExtendedProperties(material, model, pSetName);
        }

        public static List<IfcExtendedMaterialProperties> GetAllExtendedPropertySets(this IfcMaterial material,
                                                                                     IModel model)
        {
            return model.Instances.Where<IfcExtendedMaterialProperties>(pSet => pSet.Material == material).ToList();
        }

        public static List<IfcExtendedMaterialProperties> GetAllPropertySets(this IfcMaterial material)
        {
            IModel model = (material as IPersistIfcEntity).ModelOf;
            return model.Instances.Where<IfcExtendedMaterialProperties>(pset => pset.Material == material).ToList();
        }

        public static Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> GetAllPropertySingleValues(
            this IfcMaterial material)
        {
            IModel model = material.ModelOf;
            return GetAllPropertySingleValues(material, model);
        }

        public static Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> GetAllPropertySingleValues(
            this IfcMaterial material, IModel model)
        {
            IEnumerable<IfcExtendedMaterialProperties> pSets =
                model.Instances.Where<IfcExtendedMaterialProperties>(pset => pset.Material == material);
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