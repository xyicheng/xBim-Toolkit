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
        private Expression GenerateAttributeCondition(string attribute, object value, Tokens condition)
        {
            var attrNameExpr = Expression.Constant(attribute);
            var valExpr = Expression.Constant(value, typeof(object));
            var condExpr = Expression.Constant(condition);
            var scannExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("EvaluateAttributeCondition", BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, attrNameExpr, valExpr, condExpr, scannExpr);
        }

        private Expression GeneratePropertyCondition(string property, object value, Tokens condition)
        {
            var propNameExpr = Expression.Constant(property);
            var valExpr = Expression.Constant(value, typeof(object));
            var condExpr = Expression.Constant(condition);
            var scannExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("EvaluatePropertyCondition", BindingFlags.Static | BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, propNameExpr, valExpr, condExpr, scannExpr);
        }

        private static bool EvaluatePropertyCondition(IPersistIfcEntity input, string propertyName, object value, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {
            var prop = GetProperty(propertyName, input);
            //try to get attribute if any exist with this name
            if (prop == null)
            {
                var attr = GetAttributeValue(propertyName, input);
                prop = attr as IfcValue;
            }
            return EvaluateValueCondition(prop, value, condition, scanner);
        }

        private static bool EvaluateAttributeCondition(IPersistIfcEntity input, string attribute, object value, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {
            var attr = GetAttributeValue(attribute, input);
            return EvaluateValueCondition(attr, value, condition, scanner);
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

        private static bool EvaluateValueCondition(object ifcVal, object val, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
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
                    scanner.yyerror(e.Message);
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

                scanner.yyerror(val.ToString() + " is not compatible type with type of " + ifcVal.GetType());
                return false;
            }
            

            //create expression
            bool? result = null;
            switch (condition)
            {
                case Tokens.OP_EQ:
                    return left.Equals(right);
                case Tokens.OP_NEQ:
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
            scanner.yyerror("Can't compare " + left + " and " + right + ".");
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

        private static IfcValue GetProperty(string name, IPersistIfcEntity entity)
        {
            Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> pSets = null;
            IEnumerable<IfcPhysicalSimpleQuantity> quants = null;

            IfcObject obj = entity as IfcObject;
            if (obj != null)
            {
                quants = obj.GetAllPhysicalSimpleQuantities();
                pSets = obj.GetAllPropertySingleValues();
            }
            IfcTypeObject typeObj = entity as IfcTypeObject;
            if (typeObj != null)
            {
                pSets = typeObj.GetAllPropertySingleValues();
                quants = typeObj.GetAllPhysicalSimpleQuantities();
            }
            IfcMaterial material = entity as IfcMaterial;
            if (material != null)
            {
                pSets = material.GetAllPropertySingleValues();
            }

            if (pSets != null)
                foreach (var pSet in pSets)
                {
                    foreach (var prop in pSet.Value)
                    {
                        if (prop.Key.ToString().ToLower() == name.ToLower()) return prop.Value;
                    }
                }
            if (quants != null)
            {
                var quant = quants.Where(q => q.Name.ToString().ToLower() == name.ToLower()).FirstOrDefault();
                if (quant != null)
                {
                    var a = quant as IfcQuantityArea;
                    if (a != null) return a.AreaValue;

                    var c = quant as IfcQuantityCount;
                    if (c != null) return c.CountValue;

                    var l = quant as IfcQuantityLength;
                    if (l != null) return l.LengthValue;

                    var t = quant as IfcQuantityTime;
                    if (t != null) return t.TimeValue;

                    var v = quant as IfcQuantityVolume;
                    if (v != null) return v.VolumeValue;

                    var w = quant as IfcQuantityWeight;
                    if (w != null) return w.WeightValue;
                }
            }
            return null;
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
            Expression expression = GenerateAttributeCondition("Name", name, Tokens.OP_EQ);
            return Select(type, expression);
        }

        private IEnumerable<IPersistIfcEntity> Select(Type type, Expression condition = null)
        {
            //create type expression
            var evaluateMethod = GetType().GetMethod("IsOfType", BindingFlags.Static | BindingFlags.NonPublic);
            Expression typeExpr = Expression.Call(null, evaluateMethod, Expression.Constant(type), _input);

            //create body expression
            Expression exprBody = typeExpr;
            if (condition != null)
                exprBody = Expression.AndAlso(typeExpr, condition);

            return _model.Instances.Where(Expression.Lambda<Func<IPersistIfcEntity, bool>>(exprBody, _input).Compile());
        }
        #endregion

        #region TypeObject conditions 
        private Expression GenerateTypeObjectNameCondition(string typeName, Tokens condition)
        {
            var typeNameExpr = Expression.Constant(typeName);
            var condExpr = Expression.Constant(condition);
            var scanExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeObjectName", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, typeNameExpr, condExpr, scanExpr);
        }

        private Expression GenerateTypeObjectTypeCondition(Type type, Tokens condition)
        {
            var typeExpr = Expression.Constant(type, typeof(Type));
            var condExpr = Expression.Constant(condition);
            var scanExpr = Expression.Constant(Scanner);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeObjectType", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, typeExpr, condExpr, scanExpr);
        }

        private static bool EvaluateTypeObjectName(IPersistIfcEntity input, string typeName, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
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
                    scanner.yyerror("Unexpected Token in this function. Only equality or containment expected.");
                    return false;
            }
        }

        private static bool EvaluateTypeObjectType(IPersistIfcEntity input, Type type, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
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
                    scanner.yyerror(e.Message);
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
                    scanner.yyerror("Unexpected Token in this function. Only OP_EQ or OP_NEQ expected.");
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

            var evaluateMethod = GetType().GetMethod("EvaluateModelCondition");
            return Expression.Call(thisExpr, evaluateMethod, _input, valExpr, typeExpr, condExpr);
        }

        public bool EvaluateModelCondition(IPersistIfcEntity input, string value, Tokens type, Tokens condition)
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
                                attr = GetProperty(name, entity);
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


            if (group == null && typeObject == null && spatialStructure == null)
            {
                Scanner.yyerror("Only 'group', 'system', 'spatial element' or 'type object' should be in '" + aggregation + "'.");
                return;
            }
            
            //Action which will be performed
            Action perform = null;

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
        }

        public void CopyToModel(string variable, string model)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Objects manipulation
        private static string _actualPsetName = null;

        private void EvaluateSetExpression(string identifier, IEnumerable<Expression> expressions, string pSetName = null)
        {
            _actualPsetName = pSetName;
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

        private Expression GenerateSetExpression(string attrName, object newVal)
        {
            var nameExpr = Expression.Constant(attrName);
            var valExpr = Expression.Convert(Expression.Constant(newVal), typeof(object));

            var evaluateMethod = GetType().GetMethod("SetAttribute", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, nameExpr, valExpr);
        }

        private static void SetAttribute(IPersistIfcEntity input, string attrName, object newVal)
        {
            if (input == null) return;

            //if property set is specified don't even try to set attribute
            if (_actualPsetName != null)
            {
                SetProperty(input, attrName, newVal);
                return;
            }

            var attr = input.GetType().GetProperty(attrName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (attr == null)
            {
                SetProperty(input, attrName, newVal);
                return;
            }
            SetValue(attr, input, newVal);
        }

        private static void SetProperty(IPersistIfcEntity entity, string name, object newVal)
        {
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
                if (_actualPsetName != null)
                {
                    ps = obj.GetPropertySet(_actualPsetName);
                    eq = obj.GetElementQuantity(_actualPsetName);
                }
                pSets =  ps == null ? obj.GetAllPropertySets() : new List<IfcPropertySet>(){ps};
                elQuants = eq == null ? obj.GetAllElementQuantities() : new List<IfcElementQuantity>() { eq };
            }
            IfcTypeObject typeObj = entity as IfcTypeObject;
            if (typeObj != null)
            {
                if (_actualPsetName != null)
                {
                    ps = typeObj.GetPropertySet(_actualPsetName);
                    eq = typeObj.GetElementQuantity(_actualPsetName);
                }
                pSets = ps == null ? typeObj.GetAllPropertySets() : new List<IfcPropertySet>() { ps };
                elQuants = eq == null ? typeObj.GetAllElementQuantities() : new List<IfcElementQuantity>() { eq};
            }
            IfcMaterial material = entity as IfcMaterial;
            if (material != null)
            {
                if (_actualPsetName != null)
                    eps = material.GetExtendedProperties(_actualPsetName);
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

            PropertyInfo info = null;

            //set property
            if (property != null)
            {
                info = property.GetType().GetProperty("NominalValue");
                SetValue(info, property, newVal);
            }

            //set simple quantity
            else if (quantity != null)
            {
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
                    SetValue(info, quantity, newVal);
                else
                    throw new Exception("Failed to find the PropertyInfo of the simple element quantity.");
            }

            //create new property if no such a property or quantity exists
            else
            {
                string pSetName = _actualPsetName ?? Defaults.DefaultPSet;
                IfcValue val = null;
                if (newVal != null)
                    val = CreateIfcValueFromBasicValue(newVal, name);
                
                if (obj != null)
                {
                    obj.SetPropertySingleValue(pSetName, name, val);
                }
                if (typeObj != null)
                {
                    typeObj.SetPropertySingleValue(pSetName, name, val);
                }
                if (material != null)
                {
                    material.SetExtendedSingleValue(pSetName, name, val);
                }
            }
        }

        private static void SetValue(PropertyInfo info, object instance, object value)
        {
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
            catch (Exception e)
            {
                throw new Exception("Value "+ (value != null ? value.ToString() : "NULL") +" could not be set to "+ info.Name+" of type"+ instance.GetType().Name + ". Type should be compatible with " + info.MemberType);
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
            SystemsCreator creator = new SystemsCreator();

            if (name.ToLower() == "uniclass")
            {
                creator.CreateSystem(_model, SYSTEM.UNICLASS);
            }
            if (name.ToLower() == "nrm")
            {
                creator.CreateSystem(_model, SYSTEM.NRM);
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
        private Expression GenerateSpatialCondition(Tokens condition, string identifier)
        {
            var identExpr = Expression.Constant(identifier, typeof(string));
            var condExpr = Expression.Constant(condition);

            var evaluateMethod = GetType().GetMethod("EvaluateSpatialCondition", BindingFlags.NonPublic);

            return Expression.Call(null, evaluateMethod, _input, condExpr, identExpr);
        }

        private bool EvaluateSpatialCondition(IPersistIfcEntity input, Tokens condition, string identifier)
        {
            IfcProduct left = input as IfcProduct;
            if (left == null)
            {
                Scanner.yyerror(input.GetType().Name + " can't have a spatial condition.");
                return false;
            }
            IEnumerable<IfcProduct> right = _variables[identifier].OfType<IfcProduct>();
            if (right.Count() != _variables[identifier].Count())
                Scanner.yyerror("Some of the objects in "+identifier+" can't be in a spatial condition.");
            if (right.Count() == 0)
            {
                Scanner.yyerror("There are no suitable objects for spatial condition in " + identifier + ".");
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
}