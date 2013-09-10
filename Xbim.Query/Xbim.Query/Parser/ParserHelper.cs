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

namespace Xbim.Query
{
    internal partial class Parser
    {
        private XbimModel _model;
        private XbimVariables _variables;
        private ParameterExpression _input = Expression.Parameter(typeof(IPersistIfcEntity), "Input");

        //public properties of the parser
        public XbimVariables Variables { get { return _variables; } }

        internal Parser(Scanner lex, XbimModel model): base(lex)
        {
            _model = model;
            _variables = new XbimVariables();
            if (_model == null) throw new ArgumentNullException("Model is NULL");
        }

        #region Attribute and property conditions
        private IPersistIfcEntity CreateObject(Type type, string name, string description = null)
        {
            if (_model == null) throw new ArgumentNullException("Model is NULL");
            if (name == null)
            {
                Scanner.yyerror("Name must be defined for creation of the " + type.Name + ".");
            } 

            IPersistIfcEntity entity = null;
            if (_model.IsTransacting)
            {
                entity = Create(type, name, description);
            }
            else
            {
                using (var txn = _model.BeginTransaction("Object creation"))
                {
                    entity = Create(type, name, description);
                    txn.Commit();
                }
            }
            return entity;
        }

        private IPersistIfcEntity Create(Type type, string name, string description)
        {
            var entity = _model.Instances.New(type);

            //set name and description
            if (entity == null) return null;
            IfcRoot root = entity as IfcRoot;
            if (root != null)
            {
                root.Name = name;
                root.Description = description;
            }
            IfcMaterial material = entity as IfcMaterial;
            if (material != null)
            {
                material.Name = name;
            }

            return entity;
        }

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
                var attr = GetAttribute(propertyName, input);
                prop = attr as IfcValue;
            }
            return EvaluateValueCondition(prop, value, condition, scanner);
        }

        private static bool EvaluateAttributeCondition(IPersistIfcEntity input, string attribute, object value, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {

            var attr = GetAttribute(attribute, input);
            if (attr == null) 
            {
                scanner.yyerror("Attribute " + attribute + " is not defined in objects of type: " + input.GetType() + ".");
                return false;
            }
            return EvaluateValueCondition(attr, value, condition, scanner);
        }
        #endregion

        #region Property and attribute conditions helpers
        private static bool EvaluateValueCondition(object ifcVal, object val, Tokens condition, AbstractScanner<ValueType, LexLocation> scanner)
        {
            //special handling for null value comparison
            if (val == null)
            {
                if (condition == Tokens.OP_EQ && ifcVal == null)
                    return true;
                if (condition == Tokens.OP_EQ && ifcVal != null)
                    return false;
                if (condition == Tokens.OP_NEQ && ifcVal == null)
                    return false;
                if (condition == Tokens.OP_NEQ && ifcVal != null)
                    return true;
                scanner.yyerror("NULL value can be used only with operators IS and IS_NOT.");
                return false;
            }
            //no comparison can be made
            if (ifcVal == null)
                return false;

            //try to get values to the same level; none of the values can be null for this operation
            object left = UnWrapType(ifcVal);
            object right = PromoteType(GetNonNullableType(left.GetType()), val);

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

        private static object GetAttribute(string name, IPersistIfcEntity entity)
        {
            Type type = entity.GetType();
            PropertyInfo pInfo = type.GetProperty(name);
            if (pInfo == null)
                return null;
            return pInfo.GetValue(entity, new object[] { });
        }

        private static IfcValue GetProperty(string name, IPersistIfcEntity entity)
        {
            IfcObject obj = entity as IfcObject;
            Dictionary<IfcLabel, Dictionary<IfcIdentifier, IfcValue>> pSets = null;
            if (obj != null)
            {
                pSets = obj.GetAllPropertySingleValues();
            }
            IfcTypeObject typeObj = entity as IfcTypeObject;
            if (typeObj != null)
            {
                pSets = typeObj.GetAllPropertySingleValues();
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

            var evaluateMethod = GetType().GetMethod("EvaluateTypeObjectName", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, typeNameExpr, condExpr);
        }

        private Expression GenerateTypeObjectTypeCondition(Type type, Tokens condition)
        {
            var typeExpr = Expression.Constant(type);
            var condExpr = Expression.Constant(condition);

            var evaluateMethod = GetType().GetMethod("EvaluateTypeObjectType", BindingFlags.Static | BindingFlags.NonPublic);
            return Expression.Call(null, evaluateMethod, _input, typeExpr, condExpr);
        }

        private static bool EvaluateTypeObjectName(IPersistIfcEntity input, string typeName, Tokens condition)
        {
            IfcObject obj = input as IfcObject;
            if (obj == null) return false;

            var type = obj.GetDefiningType();

            switch (condition)
            {
                case Tokens.OP_EQ:
                    return type.Name == typeName;
                case Tokens.OP_NEQ:
                    return type.Name != typeName;
                default:
                    throw new ArgumentOutOfRangeException("Unexpected Token in this function. Only OP_EQ or OP_NEQ expected.");
            }
        }

        private static bool EvaluateTypeObjectType(IPersistIfcEntity input, Type type, Tokens condition)
        {
            IfcObject obj = input as IfcObject;
            if (obj == null) return false;

            var typeObj = obj.GetDefiningType();
            switch (condition)
            {
                case Tokens.OP_EQ:
                    return typeObj.GetType() == type;
                case Tokens.OP_NEQ:
                    return typeObj.GetType() != type;
                default:
                    throw new ArgumentOutOfRangeException("Unexpected Token in this function. Only OP_EQ or OP_NEQ expected.");
            }
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

            IEnumerable<IPersistIfcEntity> entities = null;
            StringBuilder str = new StringBuilder();
            if (Variables.TryGetValue(identifier, out entities))
            {
                foreach (var entity in entities)
                {
                    if (entity != null)
                    {
                        var name = GetAttribute("Name", entity);
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
            else
                Console.Write(str.ToString());

            if (output != null) output.Close();
        }

        private void DumpAttributes(string identifier,IEnumerable<string> attrNames, string outputPath = null)
        {
            TextWriter output = null;
            if (outputPath != null)
            {
                output = new StreamWriter(outputPath, false);
            }

            IEnumerable<IPersistIfcEntity> entities = null;
            StringBuilder str = new StringBuilder();
            if (Variables.TryGetValue(identifier, out entities))
            {
                foreach (var entity in entities)
                {
                    var line = "";
                    foreach (var name in attrNames)
                    {
                        //get attribute
                        var attr = GetAttribute(name, entity);
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
                Console.Write(str.ToString());

            if (output != null) output.Close();
        }

        private void ClearIdentifier(string identifier)
        {
            if (Variables.ContainsKey(identifier))
            {
                Variables[identifier] = new IPersistIfcEntity[]{};
            }
        }
        #endregion

        #region Add or remove elements to and from gtoup or type
        private void AddOrRemoveToGroupOrType(Tokens action, string productsIdentifier, string groupOrType)
        { 
        //conditions
            if (!Variables.IsDefined(productsIdentifier))
            {
                Scanner.yyerror("Variable '" + productsIdentifier + "' is not defined and doesn't contain any products.");
                return;
            }
            if (!Variables.IsDefined(groupOrType))
            {
                Scanner.yyerror("Variable '" + groupOrType + "' is not defined and doesn't contain any products.");
                return;
            }
            if (Variables[groupOrType].Count() != 1)
            {
                Scanner.yyerror("Exactly one group, system or type object should be in '" + groupOrType + "'.");
                return;
            }


            Type targetType = Variables[groupOrType].FirstOrDefault().GetType();
            var isGroup = typeof(IfcGroup).IsAssignableFrom(targetType);
            var isTypeObject = typeof(IfcTypeObject).IsAssignableFrom(targetType);

            if (!isGroup && !isTypeObject)
            {
                Scanner.yyerror("Only 'group', 'system' or 'type object' should be in '" + groupOrType + "'.");
                return;
            }
            

            if (isGroup)
            {
                if (Variables[productsIdentifier].OfType<IfcObjectDefinition>().Count() != Variables[productsIdentifier].Count())
                {
                    Scanner.yyerror("Only objects which are subtypes of 'IfcObjectDefinition' can be assigned to group '" + groupOrType + "'.");
                    return;
                }


                IfcGroup group = Variables[groupOrType].FirstOrDefault() as IfcGroup;
                if (!_model.IsTransacting)
                {
                    using (var txn = _model.BeginTransaction("Group manipulation"))
                    {
                        AddOrRemoveToGroup(action, Variables[productsIdentifier], group);
                        txn.Commit();
                    }
                }
                else
                    AddOrRemoveToGroup(action, Variables[productsIdentifier], group);

                return;
            }

            if (isTypeObject)
            {
                if (Variables[productsIdentifier].OfType<IfcObject>().Count() != Variables[productsIdentifier].Count())
                {
                    Scanner.yyerror("Only objects which are subtypes of 'IfcObject' can be assigned to 'IfcTypeObject' '" + groupOrType + "'.");
                    return;
                }

                IfcTypeObject typeObject = Variables[groupOrType].FirstOrDefault() as IfcTypeObject;
                if (!_model.IsTransacting)
                {
                    using (var txn = _model.BeginTransaction("TypeObject manipulation"))
                    {
                        AddOrRemoveToType(action, Variables[productsIdentifier], typeObject);
                        txn.Commit();
                    }
                }
                else
                    AddOrRemoveToType(action, Variables[productsIdentifier], typeObject);

                return;
            }
        }

        private void AddOrRemoveToGroup(Tokens action, IEnumerable<IPersistIfcEntity> objects, IfcGroup group)
        {
            foreach (var prod in objects)
            {

                switch (action)
                {
                    case Tokens.ADD:
                        group.AddObjectToGroup(prod as IfcObjectDefinition);
                        break;
                    case Tokens.REMOVE:
                        group.RemoveObjectFromGroup(prod as IfcObjectDefinition);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                }
            }
        }

        private void AddOrRemoveToType(Tokens action, IEnumerable<IPersistIfcEntity> objects, IfcTypeObject type)
        {
            foreach (var obj in objects)
            {
                switch (action)
                {
                    case Tokens.ADD:
                        (obj as IfcObject).SetDefiningType(type, _model);
                        break;
                    case Tokens.REMOVE:
                        IfcRelDefinesByType rel = _model.Instances.Where<IfcRelDefinesByType>(r => r.RelatingType == type&& r.RelatedObjects.Contains(obj as IfcObject)).FirstOrDefault();
                        if (rel != null) rel.RelatedObjects.Remove_Reversible(obj as IfcObject);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Unexpected action. Only ADD or REMOVE can be used in this context.");
                }
            }
        }
        #endregion

        #region Model manipulation
        public void OpenModel(string path)
        {
            try
            {
                if (Path.GetExtension(path).ToLower() == ".ifc")
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

        public void SaveModel(string path)
        {
            try
            {
                _model.SaveAs(path, XbimStorageType.IFC);
            }
            catch (Exception e)
            {
                Scanner.yyerror("Model was not saved: " + e.Message);   
            }
        }
        #endregion
    }
}
