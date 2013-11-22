using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QUT.Xbim.Gppg;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.Kernel;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using System.IO;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.MaterialPropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.Ifc2x3.ExternalReferenceResource;

namespace Xbim.Script
{
    internal partial class Parser
    {
        private XbimModel _model;
        private XbimVariables _variables;
        private ParameterExpression _input = Expression.Parameter(typeof(IPersistIfcEntity), "Input");

        //public properties of the parser
        public XbimVariables Variables { get { return _variables; } }
        public XbimModel Model { get { return _model; } }
        public TextWriter Output { get; set; }

        internal Parser(Scanner lex, XbimModel model): base(lex)
        {
            _model = model;
            _variables = new XbimVariables();
            if (_model == null) throw new ArgumentNullException("Model is NULL");
        }

        #region Objects creation
        private IPersistIfcEntity CreateObject(Type type, string name, string description = null)
        {
            if (_model == null) throw new ArgumentNullException("Model is NULL");
            if (name == null)
            {
                Scanner.yyerror("Name must be defined for creation of the " + type.Name + ".");
            } 

            Func<IPersistIfcEntity> create = () => {
                var result = _model.Instances.New(type);

                //set name and description
                if (result == null) return null;
                IfcRoot root = result as IfcRoot;
                if (root != null)
                {
                    root.Name = name;
                    root.Description = description;
                }
                IfcMaterial material = result as IfcMaterial;
                if (material != null)
                {
                    material.Name = name;
                }

                return result;
            };

            IPersistIfcEntity entity = null;
            if (_model.IsTransacting)
            {
                entity = create();
            }
            else
            {
                using (var txn = _model.BeginTransaction("Object creation"))
                {
                    entity = create();
                    txn.Commit();
                }
            }
            return entity;
        }

        private IfcMaterialLayerSet CreateLayerSet(string name, List<Layer> layers)
        {
            Func<IfcMaterialLayerSet> create = () => {
                return _model.Instances.New<IfcMaterialLayerSet>(ms =>
                {
                    ms.LayerSetName = name;
                    foreach (var layer in layers)
                    {
                        ms.MaterialLayers.Add_Reversible(_model.Instances.New<IfcMaterialLayer>(ml =>
                        {
                            ml.LayerThickness = layer.thickness;
                            //get material if it already exists
                            var material = _model.Instances.Where<IfcMaterial>(m => m.Name.ToString().ToLower() == layer.material).FirstOrDefault();
                            if (material == null)
                                material = _model.Instances.New<IfcMaterial>(m => m.Name = layer.material);
                            ml.Material = material;
                        }));
                    }
                });
            };

            IfcMaterialLayerSet result = null;
            if (_model.IsTransacting)
                result = create();
            else
                using (var txn = _model.BeginTransaction())
                {
                    result = create();
                    txn.Commit();
                }
            return result;
        }
        #endregion

        #region Attribute and property conditions
        private Expression GenerateValueCondition(string property, object value, Tokens condition, Tokens type)
        {
            var propNameExpr = Expression.Constant(property);
            var valExpr = Expression.Constant(value, typeof(object));
            var condExpr = Expression.Constant(condition);
            var thisExpr = Expression.Constant(this);

            string method = null;
            switch (type)
            {
                case Tokens.STRING:
                    method = "EvaluateValueCondition";
                    break;
                case Tokens.PROPERTY:
                    method = "EvaluatePropertyCondition";
                    break;
                case Tokens.ATTRIBUTE:
                    method = "EvaluateAttributeCondition";
                    break;
                default:
                    throw new ArgumentException("Unexpected value of the 'type'. Expected values: STRING, PROPERTY, ATTRIBUTE.");
            }

            var evaluateMethod = GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);

            return Expression.Call(thisExpr, evaluateMethod, _input, propNameExpr, valExpr, condExpr);
        }

        private bool EvaluateValueCondition(IPersistIfcEntity input, string propertyName, object value, Tokens condition)
        {
            //try to get attribute
            var attr = GetAttributeValue(propertyName, input);
            var prop = attr as IfcValue;
            
            //try to get property if attribute doesn't exist
            if (prop == null)
             prop = GetPropertyValue(propertyName, input);

            return EvaluateValue(prop, value, condition);
        }

        private bool EvaluatePropertyCondition(IPersistIfcEntity input, string propertyName, object value, Tokens condition)
        {
            var prop = GetPropertyValue(propertyName, input);
            return EvaluateValue(prop, value, condition);
        }

        private bool EvaluateAttributeCondition(IPersistIfcEntity input, string attribute, object value, Tokens condition)
        {
            var attr = GetAttributeValue(attribute, input);
            return EvaluateValue(attr, value, condition);
        }
        #endregion

        #region Property and attribute conditions helpers
        private static bool EvaluateNullCondition(object expected, object actual, Tokens condition)
        {
            if (expected != null && actual != null)
                throw new ArgumentException("One of the values is expected to be null.");
            switch (condition)
            {
                case Tokens.OP_EQ:
                    return expected == null && actual == null;
                case Tokens.OP_NEQ:
                    if (expected == null && actual != null) return true;
                    if (expected != null && actual == null) return true;
                    return false;
                default:
                    return false;
            }
        }

        private bool EvaluateValue(object ifcVal, object val, Tokens condition)
        {
            //special handling for null value comparison
            if (val == null || ifcVal == null)
            {
                try
                {
                    return EvaluateNullCondition(ifcVal, val, condition);
                }
                catch (Exception e)
                {
                    Scanner.yyerror(e.Message);
                    return false;
                }
            }

            //try to get values to the same level; none of the values can be null for this operation
            object left = null;
            object right = null;
            try
            {
                left = UnWrapType(ifcVal);
                right = PromoteType(GetNonNullableType(left.GetType()), val);
            }
            catch (Exception)
            {

                Scanner.yyerror(val.ToString() + " is not compatible type with type of " + ifcVal.GetType());
                return false;
            }
            

            //create expression
            bool? result = null;
            switch (condition)
            {
                case Tokens.OP_EQ:
                    if (left is string  && right is string)
                        return ((string)left).ToLower() == ((string)right).ToLower();
                    return left.Equals(right);
                case Tokens.OP_NEQ:
                    if (left is string && right is string)
                        return ((string)left).ToLower() != ((string)right).ToLower();
                    return !left.Equals(right);
                case Tokens.OP_GT:
                    result = GreaterThan(left, right);
                    if (result != null) return result ?? false;
                    break;
                case Tokens.OP_LT:
                    result = LessThan(left, right);
                    if (result != null) return result ?? false;
                    break;
                case Tokens.OP_GTE:
                    result = !LessThan(left, right);
                    if (result != null) return result ?? false;
                    break;
                case Tokens.OP_LTQ:
                    result = !GreaterThan(left, right);
                    if (result != null) return result ?? false;
                    break;
                case Tokens.OP_CONTAINS:
                    return Contains(left, right);
                case Tokens.OP_NOT_CONTAINS:
                    return !Contains(left, right);
                default:
                    throw new ArgumentOutOfRangeException("Unexpected token used as a condition");
            }
            Scanner.yyerror("Can't compare " + left + " and " + right + ".");
            return false;
        }

        private static object UnWrapType(object value)
        { 
            //enumeration
            if (value.GetType().IsEnum)
                return Enum.GetName(value.GetType(), value);

            //express type
            ExpressType express = value as ExpressType;
            if (express != null)
                return express.Value;

            return value;
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        private static bool IsOfType(Type type, IPersistIfcEntity entity)
        {
            return type.IsAssignableFrom(entity.GetType());
        }

        private static PropertyInfo GetAttributeInfo(string name, IPersistIfcEntity entity)
        {
            Type type = entity.GetType();
            return type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        }

        private static object GetAttributeValue(string name, IPersistIfcEntity entity)
        {
            PropertyInfo pInfo = GetAttributeInfo(name, entity);
            if (pInfo == null)
                return null;
            return pInfo.GetValue(entity, null);
        }

        private static PropertyInfo GetPropertyInfo(string name, IPersistIfcEntity entity, out object propertyObject)
        {
            //try to get the name of the pSet if it is encoded in there
            var split = name.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string pSetName = null;
            if (split.Count() == 2)
            {
                pSetName = split[0];
                name = split[1];
            }
            var specificPSet = pSetName != null;

            List<IfcPropertySet> pSets = null;
            IEnumerable<IfcExtendedMaterialProperties> pSetsMaterial = null;
            IEnumerable<IfcElementQuantity> elQuants = null;
            IfcPropertySingleValue property = null;
            IfcPhysicalSimpleQuantity quantity = null;
            IfcPropertySet ps = null;
            IfcElementQuantity eq = null;
            IfcExtendedMaterialProperties eps = null;

            IfcObject obj = entity as IfcObject;
            if (obj != null)
            {
                if (specificPSet)
                {
                    ps = obj.GetPropertySet(pSetName);
                    eq = obj.GetElementQuantity(pSetName);
                }
                pSets =  ps == null ? obj.GetAllPropertySets() : new List<IfcPropertySet>(){ps};
                elQuants = eq == null ? obj.GetAllElementQuantities() : new List<IfcElementQuantity>() { eq };
            }
            IfcTypeObject typeObj = entity as IfcTypeObject;
            if (typeObj != null)
            {
                if (specificPSet)
                {
                    ps = typeObj.GetPropertySet(pSetName);
                    eq = typeObj.GetElementQuantity(pSetName);
                }
                pSets = ps == null ? typeObj.GetAllPropertySets() : new List<IfcPropertySet>() { ps };
                elQuants = eq == null ? typeObj.GetAllElementQuantities() : new List<IfcElementQuantity>() { eq};
            }
            IfcMaterial material = entity as IfcMaterial;
            if (material != null)
            {
                if (specificPSet)
                    eps = material.GetExtendedProperties(pSetName);
                pSetsMaterial = eps == null ? material.GetAllPropertySets() : new List<IfcExtendedMaterialProperties>() { eps };
            }

            if (pSets != null)
                foreach (var pSet in pSets)
                {
                    foreach (var prop in pSet.HasProperties)
                    {
                        if (prop.Name.ToString().ToLower() == name.ToLower()) property = prop as IfcPropertySingleValue;
                    }
                }
            if (pSetsMaterial != null)
                foreach (var pSet in pSetsMaterial)
                {
                    foreach (var prop in pSet.ExtendedProperties)
                    {
                        if (prop.Name.ToString().ToLower() == name.ToLower()) property = prop as IfcPropertySingleValue;
                    }
                }
            if (elQuants != null)
                foreach (var quant in elQuants)
                {
                    foreach (var item in quant.Quantities)
                    {
                        if (item.Name.ToString().ToLower() == name.ToLower()) quantity = item as IfcPhysicalSimpleQuantity;
                    }
                }


            //set property
            if (property != null)
            {
                propertyObject = property;
                return property.GetType().GetProperty("NominalValue");
            }

            //set simple quantity
            else if (quantity != null)
            {
                PropertyInfo info = null;
                var qType = quantity.GetType();
                switch (qType.Name)
                {
                    case "IfcQuantityLength":
                        info = qType.GetProperty("LengthValue");
                        break;
                    case "IfcQuantityArea":
                        info = qType.GetProperty("AreaValue");
                        break;
                    case "IfcQuantityVolume":
                        info = qType.GetProperty("VolumeValue");
                        break;
                    case "IfcQuantityCount":
                        info = qType.GetProperty("CountValue");
                        break;
                    case "IfcQuantityWeight":
                        info = qType.GetProperty("WeightValue");
                        break;
                    case "IfcQuantityTime":
                        info = qType.GetProperty("TimeValue");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                if (info != null)
                {
                    propertyObject = quantity;
                    return info;
                }
            }

            propertyObject = null;
            return null;
        }


        private static IfcValue GetPropertyValue(string name, IPersistIfcEntity entity)
        {
            object pObject = null;
            var pInfo = GetPropertyInfo(name, entity, out pObject);

            if (pInfo == null) return null;

            var result = pInfo.GetValue(pObject, null);
            return result as IfcValue;
        }


        private static object PromoteType(Type targetType, object value)
        {

            if (targetType == typeof(Boolean)) return Convert.ToBoolean(value);
            if (targetType == typeof(Byte)) return Convert.ToByte(value);
            if (targetType == typeof(DateTime)) return Convert.ToDateTime(value);
            if (targetType == typeof(Decimal)) return Convert.ToDecimal(value);
            if (targetType == typeof(Double)) return Convert.ToDouble(value);
            if (targetType == typeof(float)) return Convert.ToDouble(value);
            if (targetType == typeof(Char)) return Convert.ToChar(value);
            if (targetType == typeof(Int16)) return Convert.ToInt16(value);
            if (targetType == typeof(Int32)) return Convert.ToInt32(value);
            if (targetType == typeof(Int64)) return Convert.ToInt64(value);
            if (targetType == typeof(SByte)) return Convert.ToSByte(value);
            if (targetType == typeof(Single)) return Convert.ToSingle(value);
            if (targetType == typeof(String)) return Convert.ToString(value);
            if (targetType == typeof(UInt16)) return Convert.ToUInt16(value);
            if (targetType == typeof(UInt32)) return Convert.ToUInt32(value);
            if (targetType == typeof(UInt64)) return Convert.ToUInt64(value);

            throw new Exception("Unexpected type");
        }

        private static bool? GreaterThan(object left, object right) 
        {
            try
            {
                var leftD = Convert.ToDouble(left);
                var rightD = Convert.ToDouble(right);
                return leftD > rightD;
            }
            catch (Exception)
            {
                return null;   
            }
           
        }

        private static bool? LessThan(object left, object right)
        {
            try
            {
                var leftD = Convert.ToDouble(left);
                var rightD = Convert.ToDouble(right);
                return leftD < rightD;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private static bool Contains(object left, object right)
        {
            string leftS = Convert.ToString(left);
            string rightS = Convert.ToString(right);

            return leftS.ToLower().Contains(rightS.ToLower());
        }
        #endregion

        #region Select statements
        private IEnumerable<IPersistIfcEntity> Select(Type type, string name)
        {
            if (!typeof(IfcRoot).IsAssignableFrom(type)) return new IPersistIfcEntity[]{};
            Expression expression = GenerateValueCondition("Name", name, Tokens.OP_EQ, Tokens.ATTRIBUTE);
            return Select(type, expression);
        }

        private IEnumerable<IPersistIfcEntity> Select(Type type, Expression condition = null)
        {
            MethodInfo method = _model.Instances.GetType().GetMethod("OfType", new Type[] { typeof(bool) });
            MethodInfo generic = method.MakeGenericMethod(type);
            if (condition != null)
            {
                var typeFiltered = generic.Invoke(_model.Instances, new object[] { true }) as IEnumerable<IPersistIfcEntity>;
                return typeFiltered.Where(Expression.Lambda<Func<IPersistIfcEntity, bool>>(condition, _input).Compile());
            }
            else
            {
                var typeFiltered = generic.Invoke(_model.Instances, new object[] { false }) as IEnumerable<IPersistIfcEntity>;
                return typeFiltered;
            }

        }

        private IEnumerable<IPersistIfcEntity> SelectClassification(string code)
        {
            return _model.Instances.Where<IfcClassificationReference>(c => c.ItemReference.ToString().ToLower() == code.ToLower());
        }
        
        #endregion

        #region TypeObject conditions 
        private Expression GenerateTypeObjectNameCondition(string typeName, Tokens condition)
        {
            var typeNameExpr = Expression.Constant(typeName);
            var condExpr = Expression.Constant(condition);
            var thisExpr = Expression.Constant(this);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeObjectName", BindingFlags.Instance | BindingFlags.NonPublic);
            return Expression.Call(thisExpr, evaluateMethod, _input, typeNameExpr, condExpr);
        }

        private Expression GenerateTypeObjectTypeCondition(Type type, Tokens condition)
        {
            var typeExpr = Expression.Constant(type, typeof(Type));
            var condExpr = Expression.Constant(condition);
            var thisExpr = Expression.Constant(this);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeObjectType", BindingFlags.Instance | BindingFlags.NonPublic);
            return Expression.Call(thisExpr, evaluateMethod, _input, typeExpr, condExpr);
        }

        private bool EvaluateTypeObjectName(IPersistIfcEntity input, string typeName, Tokens condition)
        {
            IfcObject obj = input as IfcObject;
            if (obj == null) return false;

            var type = obj.GetDefiningType();
           
            //null variant
            if (type == null)
            {
                return false;
            }

            switch (condition)
            {
                case Tokens.OP_EQ:
                    return type.Name == typeName;
                case Tokens.OP_NEQ:
                    return type.Name != typeName;
                case Tokens.OP_CONTAINS:
                    return type.Name.ToString().ToLower().Contains(typeName.ToLower());
                case Tokens.OP_NOT_CONTAINS:
                    return !type.Name.ToString().ToLower().Contains(typeName.ToLower());
                default:
                    Scanner.yyerror("Unexpected Token in this function. Only equality or containment expected.");
                    return false;
            }
        }

        private bool EvaluateTypeObjectType(IPersistIfcEntity input, Type type, Tokens condition)
        {
            IfcObject obj = input as IfcObject;
            if (obj == null) return false;

            var typeObj = obj.GetDefiningType();
            
            //null variant
            if (typeObj == null || type == null)
            {
                try
                {
                    return EvaluateNullCondition(typeObj, type, condition);
                }
                catch (Exception e)
                {
                    Scanner.yyerror(e.Message);
                    return false;
                }
            }

            switch (condition)
            {
                case Tokens.OP_EQ:
                    return typeObj.GetType() == type;
                case Tokens.OP_NEQ:
                    return typeObj.GetType() != type;
                default:
                    Scanner.yyerror("Unexpected Token in this function. Only OP_EQ or OP_NEQ expected.");
                    return false;
            }
        }

        private Expression GenerateTypeCondition(Expression expression) 
        {
            var function = Expression.Lambda<Func<IPersistIfcEntity, bool>>(expression, _input).Compile();
            var fceExpr = Expression.Constant(function);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeCondition", BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, fceExpr);
        }

        private static bool EvaluateTypeCondition(IPersistIfcEntity input, Func<IPersistIfcEntity, bool> function)
        {
            var obj = input as IfcObject;
            if (obj == null) return false;

            var defObj = obj.GetDefiningType();
            if (defObj == null) return false;

            return function(defObj);
        }

        #endregion

        #region Material conditions
        private Expression GenerateMaterialCondition(string materialName, Tokens condition)
        {
            Expression nameExpr = Expression.Constant(materialName);
            Expression condExpr = Expression.Constant(condition);

            var evaluateMethod = GetType().GetMethod("EvaluateMaterialCondition", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, nameExpr, condExpr);
        }

        private static bool EvaluateMaterialCondition(IPersistIfcEntity input, string materialName, Tokens condition) 
        {
            IfcRoot root = input as IfcRoot;
            if (root == null) return false;
            IModel model = root.ModelOf;

            var materialRelations = model.Instances.Where<IfcRelAssociatesMaterial>(r => r.RelatedObjects.Contains(root));
            List<string> names = new List<string>();
            foreach (var mRel in materialRelations)
            {
                names.AddRange(GetMaterialNames(mRel.RelatingMaterial));    
            }

            //convert to lower case
            for (int i = 0; i < names.Count; i++)
                names[i] = names[i].ToLower();

            switch (condition)
            {

                case Tokens.OP_EQ:
                    return names.Contains(materialName.ToLower());
                case Tokens.OP_NEQ:
                    return !names.Contains(materialName.ToLower());
                case Tokens.OP_CONTAINS:
                    foreach (var name in names)
                    {
                        if (name.Contains(materialName.ToLower())) return true;
                    }
                    break;
                case Tokens.OP_NOT_CONTAINS:
                    foreach (var name in names)
                    {
                        if (name.Contains(materialName.ToLower())) return false;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unexpected Token value.");
            }
            return false;
        }

        /// <summary>
        /// Get names of all materials involved
        /// </summary>
        /// <param name="materialSelect">Possible types of material</param>
        /// <returns>List of names</returns>
        private static List<string> GetMaterialNames(IfcMaterialSelect materialSelect)
        {
            List<string> names = new List<string>();
            
            IfcMaterial material = materialSelect as IfcMaterial;
            if (material != null) names.Add( material.Name);

            IfcMaterialList materialList = materialSelect as IfcMaterialList;
            if (materialList != null)
                foreach (var m in materialList.Materials)
                {
                    names.Add(m.Name);
                }
            
            IfcMaterialLayerSetUsage materialUsage = materialSelect as IfcMaterialLayerSetUsage;
            if (materialUsage != null)
                names.AddRange(GetMaterialNames(materialUsage.ForLayerSet));
            
            IfcMaterialLayerSet materialLayerSet = materialSelect as IfcMaterialLayerSet;
            if (materialLayerSet != null)
                foreach (var m in materialLayerSet.MaterialLayers)
                {
                    names.AddRange(GetMaterialNames(m));
                }
            
            IfcMaterialLayer materialLayer = materialSelect as IfcMaterialLayer;
            if (materialLayer != null)
                if (materialLayer.Material != null)
                    names.Add(materialLayer.Material.Name);

            return names;
        }
        #endregion

        #region Model conditions
        private Expression GenerateModelCondition(Tokens type, Tokens condition, string value)
        {
            var valExpr = Expression.Constant(value);
            var typeExpr = Expression.Constant(type);
            var condExpr = Expression.Constant(condition);
            var thisExpr = Expression.Constant(this);
            var modelExpr = Expression.Constant(_model);

            var evaluateMethod = GetType().GetMethod("EvaluateModelCondition", BindingFlags.Instance | BindingFlags.NonPublic);
            return Expression.Call(thisExpr, evaluateMethod, _input, valExpr, typeExpr, condExpr);
        }

        private bool EvaluateModelCondition(IPersistIfcEntity input, string value, Tokens type, Tokens condition)
        {
            IModel model = input.ModelOf;
            IModel testModel = null;

            foreach (var refMod in _model.RefencedModels)
            {
                switch (type)
                {
                    case Tokens.MODEL:
                        if (IsNameOfModel(refMod.Name, value))
                            testModel = refMod.Model;
                        break;
                    case Tokens.OWNER:
                        if (refMod.OwnerName.ToLower() == value.ToLower())
                            testModel = refMod.Model;
                        break;
                    case Tokens.ORGANIZATION:
                        if (refMod.OrganisationName.ToLower() == value.ToLower())
                            testModel = refMod.Model;
                        break;

                    default:
                        throw new ArgumentException("Unexpected condition. Only MODEL, OWNER or ORGANIZATION expected.");
                }
                if (testModel != null) 
                    break;
            }

            switch (condition)
            {
                case Tokens.OP_EQ:
                    return model == testModel;
                case Tokens.OP_NEQ:
                    return model != testModel;
                default:
                    throw new ArgumentException("Unexpected condition. Only OP_EQ or OP_NEQ expected.");
            }
        }

        private static bool IsNameOfModel(string modelName, string name)
        {
            var mName = modelName.ToLower();
            var sName = name.ToLower();

            if (mName == sName) return true;
            if (Path.GetFileName(mName) == sName) return true;
            if (Path.GetFileNameWithoutExtension(mName) == sName) return true;

            return false;
        }
        #endregion

        #region Variables manipulation
        private void AddOrRemoveFromSelection(string variableName, Tokens operation, object entities)
        {
            IEnumerable<IPersistIfcEntity> ent = entities as IEnumerable<IPersistIfcEntity>;
            if (ent == null) throw new ArgumentException("Entities should be IEnumerable<IPersistIfcEntity>");
            switch (operation)
            {
                case Tokens.OP_EQ:
                    _variables.AddEntities(variableName, ent);
                    break;
                case Tokens.OP_NEQ:
                    _variables.RemoveEntities(variableName, ent);
                    break;
                default:
                    throw new ArgumentException("Unexpected token. OP_EQ or OP_NEQ expected only.");
            }
        }

        private void DumpIdentifier(string identifier, string outputPath = null)
        {
            TextWriter output = null;
            if (outputPath != null)
            {
                output = new StreamWriter(outputPath, false);
            }

            StringBuilder str = new StringBuilder();
            if (Variables.IsDefined(identifier))
            {
                foreach (var entity in Variables[identifier])
                {
                    if (entity != null)
                    {
                        var name = GetAttributeValue("Name", entity);
                        str.AppendLine(String.Format("{1} #{0}: {2}", entity.EntityLabel, entity.GetType().Name, name != null ? name.ToString() : "No name defined"));
                    }
                    else
                        throw new Exception("Null entity in the dictionary");
                }
            }
            else
                str.AppendLine(String.Format("Variable {0} is not defined.", identifier));

            if (output != null)
                output.Write(str.ToString());
            Write(str.ToString());

            if (output != null) output.Close();
        }

        private void DumpAttributes(string identifier,IEnumerable<string> attrNames, string outputPath = null)
        {

            TextWriter output = null;
            try
            {
                if (outputPath != null)
                {
                    output = new StreamWriter(outputPath, false);
                }

                StringBuilder str = new StringBuilder();
                if (Variables.IsDefined(identifier))
                {
                    var header = "";
                    foreach (var name in attrNames)
                    {
                        header += name + "; ";
                    }
                    str.AppendLine(header);

                    foreach (var entity in Variables[identifier])
                    {
                        var line = "";
                        foreach (var name in attrNames)
                        {
                            //get attribute
                            var attr = GetAttributeValue(name, entity);
                            if (attr == null)
                                attr = GetPropertyValue(name, entity);
                            if (attr != null)
                                line += attr.ToString() + "; ";
                            else
                                line += " - ; ";
                        }
                        str.AppendLine(line);
                    }
                }
                else
                    str.AppendLine(String.Format("Variable {0} is not defined.", identifier));

                if (output != null)
                    output.Write(str.ToString());
                else
                    Write(str.ToString());

            }
            catch (Exception e)
            {
                Scanner.yyerror("It was not possible to dump specified content of the " + identifier + ": " + e.Message);
            }
            finally
            {
                //make sure output will not stay opened
                if (output != null) output.Close();
            }
            
        }

        private void ClearIdentifier(string identifier)
        {
            if (Variables.IsDefined(identifier))
            {
                Variables.Clear(identifier);
            }
        }

        private void CountIdentifier(string identifier)
        {
            if (Variables.IsDefined(identifier))
            {
                WriteLine(Variables[identifier].Count().ToString());
            }
        }
        #endregion

        #region Add or remove elements to and from group or type or spatial element
        private void AddOrRemove(Tokens action, string productsIdentifier, string aggregation)
        { 
        //conditions
            if (!Variables.IsDefined(productsIdentifier))
            {
                Scanner.yyerror("Variable '" + productsIdentifier + "' is not defined and doesn't contain any products.");
                return;
            }
            if (!Variables.IsDefined(aggregation))
            {
                Scanner.yyerror("Variable '" + aggregation + "' is not defined and doesn't contain any products.");
                return;
            }
            if (Variables[aggregation].Count() != 1)
            {
                Scanner.yyerror("Exactly one group, system, type or spatial element should be in '" + aggregation + "'.");
                return;
            }

            //check if all of the objects are from the actual model and not just referenced ones
            foreach (var item in Variables[productsIdentifier])
            {
                if (item.ModelOf != _model)
                {
                    Scanner.yyerror("There is an object which is from referenced model so it cannot be used in this expression. Operation canceled.");
                    return;
                }
            }
            if (Variables[aggregation].FirstOrDefault().ModelOf != _model)
            {
                Scanner.yyerror("There is an object which is from referenced model so it cannot be used in this expression. Operation canceled.");
                return;
            }

            IfcGroup group = Variables[aggregation].FirstOrDefault() as IfcGroup;
            IfcTypeObject typeObject = Variables[aggregation].FirstOrDefault() as IfcTypeObject;
            IfcSpatialStructureElement spatialStructure = Variables[aggregation].FirstOrDefault() as IfcSpatialStructureElement;
            IfcClassificationNotationSelect classification = Variables[aggregation].FirstOrDefault() as IfcClassificationNotationSelect;

            if (group == null && typeObject == null && spatialStructure == null && classification == null)
            {
                Scanner.yyerror("Only 'group', 'system', 'spatial element' or 'type object' should be in '" + aggregation + "'.");
                return;
            }
            
            //Action which will be performed
            Action perform = null;

            if (classification != null)
            {
                var objects = Variables[productsIdentifier].OfType<IfcRoot>().Cast<IfcRoot>();
                if (objects.Count() != Variables[productsIdentifier].Count())
                    Scanner.yyerror("Only objects which are subtypes of 'IfcRoot' can be assigned to classification '" + aggregation + "'.");

                perform = () => {
                    foreach (var obj in objects)
                    {

                        switch (action)
                        {
                            case Tokens.ADD:
                                classification.AddObjectToClassificationNotation(obj);
                                break;
                            case Tokens.REMOVE:
                                classification.RemoveObjectFromClassificationNotation(obj);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                        }
                    }
                };
            }

            if (group != null)
            {
                var objects = Variables[productsIdentifier].OfType<IfcObjectDefinition>().Cast<IfcObjectDefinition>();
                if (objects.Count() != Variables[productsIdentifier].Count())
                    Scanner.yyerror("Only objects which are subtypes of 'IfcObjectDefinition' can be assigned to group '" + aggregation + "'.");

                perform = () =>
                {
                    foreach (var obj in objects)
                    {

                        switch (action)
                        {
                            case Tokens.ADD:
                                group.AddObjectToGroup(obj);
                                break;
                            case Tokens.REMOVE:
                                group.RemoveObjectFromGroup(obj);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                        }
                    }
                };
            }

            if (typeObject != null)
            {
                var objects = Variables[productsIdentifier].OfType<IfcObject>().Cast<IfcObject>();
                if (objects.Count() != Variables[productsIdentifier].Count())
                    Scanner.yyerror("Only objects which are subtypes of 'IfcObject' can be assigned to 'IfcTypeObject' '" + aggregation + "'.");

                perform = () => {
                    foreach (var obj in objects)
                    {
                        switch (action)
                        {
                            case Tokens.ADD:
                                obj.SetDefiningType(typeObject, _model);
                                //if there is material layer set defined for the type material layer set usage should be defined for the elements
                                var lSet = typeObject.GetMaterial() as IfcMaterialLayerSet;
                                if (lSet != null)
                                {
                                    var usage = _model.Instances.New<IfcMaterialLayerSetUsage>(u => {
                                        u.ForLayerSet = lSet;
                                        u.DirectionSense = IfcDirectionSenseEnum.POSITIVE;
                                        u.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS1;
                                        u.OffsetFromReferenceLine = 0;
                                    });
                                    obj.SetMaterial(usage);
                                }
                                break;
                            case Tokens.REMOVE:
                                IfcRelDefinesByType rel = _model.Instances.Where<IfcRelDefinesByType>(r => r.RelatingType == typeObject && r.RelatedObjects.Contains(obj)).FirstOrDefault();
                                if (rel != null) rel.RelatedObjects.Remove_Reversible(obj);
                                //remove material layer set usage if any exist. It is kind of indirect relation.
                                var lSet2 = typeObject.GetMaterial() as IfcMaterialLayerSet;
                                var usage2 = obj.GetMaterial() as IfcMaterialLayerSetUsage;
                                if (lSet2 != null && usage2 != null && usage2.ForLayerSet == lSet2)
                                {
                                    //the best would be to delete usage2 from the model but that is not supported bz the XbimModel at the moment
                                    var rel2 = _model.Instances.Where<IfcRelAssociatesMaterial>(r => 
                                            r.RelatingMaterial as IfcMaterialLayerSetUsage == usage2 && 
                                            r.RelatedObjects.Contains(obj)
                                        ).FirstOrDefault();
                                    if (rel2 != null) rel2.RelatedObjects.Remove_Reversible(obj);
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                        }
                    }
                };
            }

            if (spatialStructure != null)
            {
                var objects = Variables[productsIdentifier].OfType<IfcProduct>().Cast<IfcProduct>();
                if (objects.Count() != Variables[productsIdentifier].Count())
                    Scanner.yyerror("Only objects which are subtypes of 'IfcProduct' can be assigned to 'IfcSpatialStructureElement' '" + aggregation + "'.");

                perform = () =>
                {
                    foreach (var obj in objects)
                    {
                        switch (action)
                        {
                            case Tokens.ADD:
                                spatialStructure.AddElement(obj);
                                break;
                            case Tokens.REMOVE:
                                IfcRelContainedInSpatialStructure rel = _model.Instances.Where<IfcRelContainedInSpatialStructure>(r => r.RelatingStructure == spatialStructure && r.RelatedElements.Contains(obj)).FirstOrDefault();
                                if (rel != null) rel.RelatedElements.Remove_Reversible(obj);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                        }
                    }
                };
            }

            if (perform == null) return;

            //perform action
            if (!_model.IsTransacting)
            {
                using (var txn = _model.BeginTransaction("Group manipulation"))
                {
                    perform();
                    txn.Commit();
                }
            }
            else
                perform();
        }
        #endregion

        #region Model manipulation
        public void OpenModel(string path)
        {
            if (!File.Exists(path))
            {
                Scanner.yyerror("File doesn't exist: " + path);
                return;
            }
            try
            {
                string ext = Path.GetExtension(path).ToLower();
                if (ext != ".xbim" || ext != ".xbimf")
                    _model.CreateFrom(path, null, null, true);
                else
                    _model.Open(path, XbimExtensions.XbimDBAccess.ReadWrite);
                ModelChanged(_model);
            }
            catch (Exception e)
            {
                Scanner.yyerror("File '"+path+"' can't be used as an input file. Model was not opened: " + e.Message);
            }
        }

        public void CloseModel()
        {
            try
            {
                _model.Close();
                _variables.Clear();
                _model = XbimModel.CreateTemporaryModel();
                ModelChanged(_model);
            }
            catch (Exception e)
            {

                Scanner.yyerror("Model could not have been closed: " + e.Message);
            }
            
        }

        public void ValidateModel()
        {
            try
            {
                TextWriter errOutput = new StringWriter();
                var errCount = _model.Validate(errOutput, XbimExtensions.ValidationFlags.All);

                if (errCount != 0)
                {
                    WriteLine("Number of errors: " + errCount.ToString());
                    WriteLine(errOutput.ToString());
                }
                else
                    WriteLine("No errors in the model.");
            }
            catch (Exception e)
            {
                Scanner.yyerror("Model could not be validated: " + e.Message);
            }
        }

        public void SaveModel(string path)
        {
            try
            {
                _model.SaveAs(path);
            }
            catch (Exception e)
            {
                Scanner.yyerror("Model was not saved: " + e.Message);   
            }
        }

        public void AddReferenceModel(string refModel, string organization, string owner)
        {
            _model.AddModelReference(refModel, organization, owner);
            ModelChanged(_model);
        }

        public void CopyToModel(string variable, string model)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Objects manipulation
        private void EvaluateSetExpression(string identifier, IEnumerable<Expression> expressions)
        {
            if (identifier == null || expressions == null) throw new ArgumentNullException();

            Action<string, IEnumerable<Expression>> perform = (ident, exprs) => {
                var entities = _variables.GetEntities(ident);
                if (entities == null) return;
                foreach (var expression in exprs)
                {
                    try
                    {
                        var action = Expression.Lambda<Action<IPersistIfcEntity>>(expression, _input).Compile();
                        entities.ToList().ForEach(action);
                    }
                    catch (Exception e)
                    {
                        Scanner.yyerror(e.Message);
                    }
                }    
            };

            if (_model.IsTransacting)
                perform(identifier, expressions);
            else
                using (var txn = _model.BeginTransaction("Setting properties and attribues"))
                {
                    perform(identifier, expressions);
                    txn.Commit();
                }
        }

        private Expression GenerateSetExpression(string attrName, object newVal, Tokens type)
        {
            var nameExpr = Expression.Constant(attrName);
            var valExpr = Expression.Convert(Expression.Constant(newVal), typeof(object));
            var thisExpr = Expression.Constant(this);

            string methodName = null;
            switch (type)
            {
                case Tokens.STRING:
                    methodName = "SetAttributeOrProperty";
                    break;
                case Tokens.PROPERTY:
                    methodName = "SetProperty";
                    break;
                case Tokens.ATTRIBUTE:
                    methodName = "SetAttribute";
                    break;
                default:
                    throw new ArgumentException("Unexpected value of the type. STRING, PROPERTY and ATTRIBUTE tokens expected only.");
            }

            var evaluateMethod = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            return Expression.Call(thisExpr, evaluateMethod, _input, nameExpr, valExpr);
        }

        private void SetAttributeOrProperty(IPersistIfcEntity input, string attrName, object newVal)
        {
            //try to set attribute as a priority
            var attr = GetAttributeInfo(attrName, input);
            if (attr != null)
                SetValue(attr, input, newVal);
            else
                //set property if no such an attribute exist
                SetProperty(input, attrName, newVal);
        }

        private void SetAttribute(IPersistIfcEntity input, string attrName, object newVal)
        {
            if (input == null) return;

            var attr = GetAttributeInfo(attrName, input);
            SetValue(attr, input, newVal);
        }

        private void SetProperty(IPersistIfcEntity entity, string name, object newVal)
        {
            //try to get existing property
            object pObject = null;
            var pInfo = GetPropertyInfo(name, entity, out pObject);
            if (pInfo != null)
            {
            SetValue(pInfo, pObject, newVal);
            }

            //create new property if no such a property or quantity exists
            else
            {
                //try to get the name of the pSet if it is encoded in there
                var split = name.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                string pSetName = null;
                if (split.Count() == 2)
                {
                    pSetName = split[0];
                    name = split[1];
                }

                //prepare potential objects
                IfcObject obj = entity as IfcObject;
                IfcTypeObject typeObj = entity as IfcTypeObject;
                IfcMaterial material = entity as IfcMaterial;

                //set new property in specified or default property set
                pSetName = pSetName ?? Defaults.DefaultPSet;
                IfcValue val = null;
                if (newVal != null)
                    val = CreateIfcValueFromBasicValue(newVal, name);

                if (obj != null)
                {
                    obj.SetPropertySingleValue(pSetName, name, val);
                }
                else if (typeObj != null)
                {
                    typeObj.SetPropertySingleValue(pSetName, name, val);
                } 
                else if (material != null)
                {
                    material.SetExtendedSingleValue(pSetName, name, val);
                }
            }
        }

        private void SetValue(PropertyInfo info, object instance, object value)
        {
            if (info == null)
            {
                Scanner.yyerror("It is not possible to set value of the property or attribute which doesn't exist.");
                return;
            }
            try
            {
                if (value != null)
                {
                    var targetType = info.PropertyType.IsNullableType()
                        ? Nullable.GetUnderlyingType(info.PropertyType)
                        : info.PropertyType;

                    object newValue = null;
                    if (!targetType.IsInterface && !targetType.IsAbstract && !targetType.IsEnum)
                        newValue = Activator.CreateInstance(targetType, value);
                    else if (targetType.IsEnum)
                    {
                        //this can throw exception if the name is not correct
                        newValue = Enum.Parse(targetType, value.ToString(), true);
                    }
                    else
                        newValue = CreateIfcValueFromBasicValue(value, info.Name);
                    //this will throw exception if the types are not compatible
                    info.SetValue(instance, newValue, null);
                }
                else
                    //this can throw error if the property can't be null (like structure)
                    info.SetValue(instance, null, null);
            }
            catch (Exception)
            {
                throw new Exception("Value "+ (value != null ? value.ToString() : "NULL") +" could not have been set to "+ info.Name + " of type"+ instance.GetType().Name + ". Type should be compatible with " + info.MemberType);
            }
            
        }

        private static IfcValue CreateIfcValueFromBasicValue(object value, string propName)
        {
            Type type = value.GetType();
            if (type == typeof(int))
                return new IfcInteger((int)value);
            if (type == typeof(string))
                return new IfcText((string)value);
            if (type == typeof(double))
                return new IfcNumericMeasure((double)value);
            if (type == typeof(bool))
                return new IfcBoolean((bool)value);

            throw new Exception("Unexpected type of the new value " + type.Name + " for property " + propName);
        }

        private Expression GenerateSetMaterialExpression(string materialIdentifier)
        {
            var entities = _variables.GetEntities(materialIdentifier);

            if (entities == null)
            {
                Scanner.yyerror("There should be exactly one material in the variable " + materialIdentifier);
                return Expression.Empty();
            }
            var count = entities.Count();
            if (count != 1)
            {
                Scanner.yyerror("There should be only one object in the variable " + materialIdentifier);
                return Expression.Empty();
            }
            var material = entities.FirstOrDefault() as IfcMaterialSelect;
            if (material == null)
            {
                Scanner.yyerror("There should be exactly one material in the variable " + materialIdentifier);
                return Expression.Empty();
            }

            var materialExpr = Expression.Constant(material);
            var scanExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("SetMaterial", BindingFlags.NonPublic|BindingFlags.Static);
            return Expression.Call(null, evaluateMethod, _input, materialExpr, scanExpr);
        }

        private static void SetMaterial(IPersistIfcEntity entity, IfcMaterialSelect material, AbstractScanner<ValueType, LexLocation> scanner)
        {
            if (entity == null || material == null) return;

            var materialSelect = material as IfcMaterialSelect;
            if (materialSelect == null)
            {
                scanner.yyerror(material.GetType() + " can't be used as a material");
                return;
            }
            var root = entity as IfcRoot;
            if (root == null)
            {
                scanner.yyerror(root.GetType() + " can't have a material assigned.");
                return;
            }


            IModel model = material.ModelOf;
            var matSet = material as IfcMaterialLayerSet;
            if (matSet != null)
            {
                var element = root as IfcElement;
                if (element != null)
                {
                    var usage = model.Instances.New<IfcMaterialLayerSetUsage>(mlsu => {
                        mlsu.DirectionSense = IfcDirectionSenseEnum.POSITIVE;
                        mlsu.ForLayerSet = matSet;
                        mlsu.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS1;
                        mlsu.OffsetFromReferenceLine = 0;
                    });
                    var rel = model.Instances.New<IfcRelAssociatesMaterial>(r => {
                        r.RelatedObjects.Add_Reversible(root);
                        r.RelatingMaterial = usage;
                    });
                    return;
                }
            }

            var matUsage = material as IfcMaterialLayerSetUsage;
            if (matUsage != null)
            {
                var typeElement = root as IfcElementType;
                if (typeElement != null)
                {
                    //change scope to the layer set for the element type. It will be processed in a standard way than
                    materialSelect = matUsage.ForLayerSet;
                }
            }

            //find existing relation
            var matRel = model.Instances.Where<IfcRelAssociatesMaterial>(r => r.RelatingMaterial == materialSelect).FirstOrDefault();
            if (matRel == null)
                //create new if none exists
                matRel = model.Instances.New<IfcRelAssociatesMaterial>(r => r.RelatingMaterial = materialSelect);
            //insert only if it is not already there
            if (!matRel.RelatedObjects.Contains(root)) matRel.RelatedObjects.Add_Reversible(root);

        }
        #endregion

        #region Thickness conditions
        private Expression GenerateThicknessCondition(double thickness, Tokens condition)
        {
            var thickExpr = Expression.Constant(thickness);
            var condExpr = Expression.Constant(condition);

            var evaluateMethod = GetType().GetMethod("EvaluateThicknessCondition", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, thickExpr, condExpr);
        }

        private static bool EvaluateThicknessCondition(IPersistIfcEntity input, double thickness, Tokens condition)
        {
            IfcRoot root = input as IfcRoot;
            if (input == null) return false;

            double? value = null;
            var materSel = root.GetMaterial();
            IfcMaterialLayerSetUsage usage = materSel as IfcMaterialLayerSetUsage;
            if (usage != null)
                if (usage.ForLayerSet != null) 
                    value = usage.ForLayerSet.MaterialLayers.Aggregate(0.0, (current, layer) => current + layer.LayerThickness);
            IfcMaterialLayerSet set = materSel as IfcMaterialLayerSet;
            if (set != null)
                value = set.TotalThickness;
            if (value == null)
                return false;
            switch (condition)
            {
                case Tokens.OP_EQ:
                    return thickness.AlmostEquals(value ?? 0);
                case Tokens.OP_NEQ:
                    return !thickness.AlmostEquals(value ?? 0);
                case Tokens.OP_GT:
                    return value > thickness;
                case Tokens.OP_LT:
                    return value < thickness;
                case Tokens.OP_GTE:
                    return value >= thickness;
                case Tokens.OP_LTQ:
                    return value <= thickness;
                default:
                    throw new ArgumentException("Unexpected value of the condition");
            }
        }
        #endregion

        #region Creation of classification systems
        private void CreateClassification(string name)
        {
            ClassificationCreator creator = new ClassificationCreator();

            try
            {
                creator.CreateSystem(_model, name);
            }
            catch (Exception e)
            {
                Scanner.yyerror("Classification {0} couldn't have been created: {1}", name, e.Message);
            }
        }
        #endregion

        #region Group conditions
        private Expression GenerateGroupCondition(Expression expression) 
        {
            var function = Expression.Lambda<Func<IPersistIfcEntity, bool>>(expression, _input).Compile();
            var fceExpr = Expression.Constant(function);

            var evaluateMethod = GetType().GetMethod("EvaluateGroupCondition", BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, fceExpr);
        }

        private static bool EvaluateGroupCondition(IPersistIfcEntity input, Func<IPersistIfcEntity, bool> function)
        {
            foreach (var item in GetGroups(input))
            {
                if (function(item)) return true;
            }
            return false;
        }

        private static IEnumerable<IfcGroup> GetGroups(IPersistIfcEntity input)
        {
            IModel model = input.ModelOf;
            var obj = input as IfcObjectDefinition;
            if (obj != null)
            {
                var rels = model.Instances.Where<IfcRelAssignsToGroup>(r => r.RelatedObjects.Contains(input));
                foreach (var rel in rels)
                {
                    yield return rel.RelatingGroup;

                    //recursive search for upper groups in the hierarchy
                    foreach (var gr in GetGroups(rel.RelatingGroup))
                    {
                        yield return gr;
                    }
                }
            }
        }
        #endregion

        #region Spatial conditions
        private Expression GenerateSpatialCondition(Tokens op, Tokens condition, string identifier)
        {
            IEnumerable<IfcProduct> right = _variables[identifier].OfType<IfcProduct>();
            if (right.Count() == 0)
            {
                Scanner.yyerror("There are no suitable objects for spatial condition in " + identifier + ".");
                return Expression.Empty();
            }
            if (right.Count() != _variables[identifier].Count())
                Scanner.yyerror("Some of the objects in " + identifier + " can't be in a spatial condition.");

            var rightExpr = Expression.Constant(right);
            var condExpr = Expression.Constant(condition);
            var opExpr = Expression.Constant(op);
            var scanExpr = Expression.Constant(Scanner);
            var thisExpr = Expression.Constant(this);

            var evaluateMethod = GetType().GetMethod("EvaluateSpatialCondition", BindingFlags.NonPublic | BindingFlags.Instance);

            return Expression.Call(thisExpr, evaluateMethod, _input, opExpr, condExpr, rightExpr, scanExpr);
        }

        private bool EvaluateSpatialCondition(IPersistIfcEntity input, Tokens op, Tokens condition, IEnumerable<IfcProduct> right)
        {
            IfcProduct left = input as IfcProduct;
            if (left == null)
            {
                Scanner.yyerror(input.GetType().Name + " can't have a spatial condition.");
                return false;
            }

            switch (condition)
            {
                case Tokens.NORTH_OF:
                    break;
                case Tokens.SOUTH_OF:
                    break;
                case Tokens.WEST_OF:
                    break;
                case Tokens.EAST_OF:
                    break;
                case Tokens.ABOVE:
                    break;
                case Tokens.BELOW:
                    break;
                case Tokens.SPATIALLY_EQUALS:
                    break;
                case Tokens.DISJOINT:
                    break;
                case Tokens.INTERSECTS:
                    break;
                case Tokens.TOUCHES:
                    break;
                case Tokens.CROSSES:
                    break;
                case Tokens.WITHIN:
                    break;
                case Tokens.SPATIALLY_CONTAINS:
                    break;
                case Tokens.OVERLAPS:
                    break;
                case Tokens.RELATE:
                    break;
                default:
                    break;
            }

            throw new NotImplementedException();
        }
        #endregion


        #region Existance conditions
        private Expression GenerateExistanceCondition(Tokens existanceCondition, string modelName)
        {
            throw new NotImplementedException();
        }

        private Expression GenerateExistanceCondition(Tokens conditionA, string modelA, Tokens conditionB, string modelB)
        {
            throw new NotImplementedException();
        }
        #endregion

        /// <summary>
        /// Unified function so that the same output can be send 
        /// to the Console and to the optional text writer as well.
        /// </summary>
        /// <param name="message">Message for the output</param>
        private void WriteLine(string message)
        {
            Console.WriteLine(message);
            if (Output != null)
                Output.WriteLine(message);
        }

        /// <summary>
        /// Unified function so that the same output can be send 
        /// to the Console and to the optional text writer as well.
        /// </summary>
        /// <param name="message">Message for the output</param>
        private void Write(string message)
        {
            Console.Write(message);
            if (Output != null)
                Output.Write(message);
        }

        //event to be used in the event driven environment
        public event ModelChangedHandler OnModelChanged;
        private void ModelChanged(XbimModel newModel)
        {
            if (OnModelChanged != null)
                OnModelChanged(this, new ModelChangedEventArgs(newModel));
        }
    }

    internal struct Layer
    {
        public string material;
        public double thickness;
    }

    public static class TypeExtensions
    {
        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType
            && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }

        public static bool AlmostEquals(this double number, double value)
        {
            return Math.Abs(number - value) < 0.000000001;
        }
    }

    public delegate void ModelChangedHandler(object sender, ModelChangedEventArgs e);

    public class ModelChangedEventArgs : EventArgs
    {
        private XbimModel _newModel;
        public XbimModel NewModel { get { return _newModel; } }
        public ModelChangedEventArgs(XbimModel newModel)
        {
            _newModel = newModel;
        }
    }
}
