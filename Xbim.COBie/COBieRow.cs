using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.COBie.Data;
using System.Security.Cryptography;
using System.Runtime.Serialization;

namespace Xbim.COBie
{
	/// <summary>
	/// Abstract base class for Rows
	/// </summary>
    [Serializable()]
    public abstract class COBieRow
    {
        public ICOBieSheet<COBieRow> ParentSheet;
        public int RowNumber { get;  set; }

        public COBieRow(ICOBieSheet<COBieRow> parentSheet)
        {
            ParentSheet = parentSheet;
        }

        public string GetPrimaryKeyValue
        {
            get
            {
                int c = ParentSheet.KeyColumns.Count();
                if (c == 1)
                {
                    var value = ParentSheet.KeyColumns.First().PropertyInfo.GetValue(this, null);
                    if (value != null)
                        return value as String;
                    else
                        return "";
                }
                else if (c > 1)
                {
                    List<string> keyValues = new List<string>(c);
                    foreach (var keyProp in ParentSheet.KeyColumns)
                    {
                        if (keyProp.PropertyInfo.GetValue(this, null) != null)
                            keyValues.Add(keyProp.PropertyInfo.GetValue(this, null).ToString());
                        else
                            keyValues.Add("");
                        
                    }
                    return string.Join(";", keyValues);
                }
                return null;
            }
        }

        public COBieCell this[int i]
        {
            get
            {
                COBieColumn cobieColumn = ParentSheet.Columns.Where(idxcol => idxcol.Key == i).Select(idxcol => idxcol.Value).FirstOrDefault();
                if (cobieColumn != null)
                {
                    object pVal = cobieColumn.PropertyInfo.GetValue(this, null);

                    string cellValue = (pVal != null) ? pVal.ToString() : Constants.DEFAULT_STRING;
                    COBieCell thiscell = new COBieCell(cellValue, cobieColumn);
                    return thiscell;
                }
                
                
                return null;
            }
            set
            {
                COBieColumn cobieColumn = ParentSheet.Columns.Where(idxcol => idxcol.Key == i).Select(idxcol => idxcol.Value).FirstOrDefault();
                if (cobieColumn != null)
                    cobieColumn.PropertyInfo.SetValue(this, value.CellValue, null);

            }
            
        }

        

        public COBieCell this[string name]
        {
            get
            {   
                COBieColumn cobieColumn = ParentSheet.Columns.Where(idxcol => idxcol.Value.ColumnName == name).Select(idxcol => idxcol.Value).FirstOrDefault();
                if (cobieColumn != null)
                {
                    object pVal = cobieColumn.PropertyInfo.GetValue(this, null);
                    string cellValue = (pVal != null) ? pVal.ToString() : Constants.DEFAULT_STRING;
                    COBieCell thiscell = new COBieCell(cellValue, cobieColumn);
                    return thiscell;
                }
                return null;
            }
        }

        public int RowCount
        {
            get
            {
                return ParentSheet.Columns.Count;
            }
        }

        
       

        /// <summary>
        /// Row hash value
        /// </summary>
        public string RowHashValue
        {
            get { return GenerateRowHash(); }
        } 

        /// <summary>
        /// Hash value of the row in BuildIndices on sheet validation
        /// </summary>
        public string InitialRowHashValue { get; private set; }
        /// <summary>
        /// Hash value set for the row in BuildIndices on sheet validation
        /// </summary>
        public void SetInitialRowHash()
        {
            InitialRowHashValue = RowHashValue;
        }

        /// <summary>
        /// Return the concatenation of the row values
        /// </summary>
        /// <returns>string value of all rows added together</returns>
        private string ConcatRowValues()
        {
            string value = "";
            StringBuilder stringBld = new StringBuilder();
            for (int i = 0; i < RowCount; i++)
            {
                value = this[i].CellValue;
                if (string.IsNullOrEmpty(value)) value = "";
                stringBld.Append(value);
            }
            return stringBld.ToString().ToLower();
        }

        /// <summary>
        /// Generate the Hash code for the row values
        /// </summary>
        /// <returns></returns>
        private string GenerateRowHash()
        {
            string value = "";
            string rowValue = ConcatRowValues(); //get the row cell values concatenated together
            using (MD5 md5hash = MD5.Create())
            {
                value = GetRowHash(md5hash, rowValue);
            }
            return value;
        }

        /// <summary>
        /// Generate the Hash code for the row values
        /// </summary>
        /// <returns></returns>
        private string GeneratePrimaryKeyHash()
        {
            string value = "";
            string keyValue = GetPrimaryKeyValue; //get the row cell values concatenated together
            using (MD5 md5hash = MD5.Create())
            {
                value = GetRowHash(md5hash, keyValue);
            }
            return value;
        }

        /// <summary>
        /// Get the MD5 Hash value
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="rowValue">string to hash</param>
        /// <returns> hexadecimal string</returns>
        public string GetRowHash(MD5 md5, string rowValue)
        {
            //string rowValue = ConcatRowValues(); //get the row cell values concatenated together
            byte[] rowData = md5.ComputeHash(Encoding.UTF8.GetBytes(rowValue));
            StringBuilder stringBld = new StringBuilder();
            // Loop through each byte of the hashed rowData  
            for (int i = 0; i < rowData.Length; i++)
            {
                stringBld.Append(rowData[i].ToString("x2"));//format as a hexadecimal string. 
            }
            return stringBld.ToString();// Return the hexadecimal string. 
        }

       
        


    }
}
