using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.COBie.Formatters
{
	public class DataSetFormatter : ICOBieFormatter
	{
		public void Format(COBieReader data)
		{
			throw new NotImplementedException();

			//DataSet dsSheets = new DataSet();

			// DataTable dt;

			// xml 
			//string filePath = "cobieData.xml";
			//XmlTextWriter textWriter = new XmlTextWriter(filePath, null);   

			//if (_cobieContracts.Count > 0)
			//{
			//    dt = ToDataTable(_cobieContracts.ToArray(), "COBieContract");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//if (_cobieAssemblies.Count > 0)
			//{
			//    dt = ToDataTable(_cobieAssemblies.ToArray(), "COBieAssemblyRow");
			//    dsSheets.Tables.Add(dt.Copy());

			//    //ToXML(_cobieAssemblies.ToArray(), "COBieAssemblyRow", textWriter);

			//}



			//if (_cobieComponents.Count > 0)
			//{
			//    dt = ToDataTable(_cobieComponents.ToArray(), "COBieComponent");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//if (_cobieFacilities.Count > 0)
			//{
			//    dt = ToDataTable(_cobieFacilities.ToArray(), "COBieFacility");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//if (_cobieFloors.Count > 0)
			//{
			//    dt = ToDataTable(_cobieFloors.ToArray(), "COBieFloor");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//if (_cobieSpaces.Count > 0)
			//{
			//    dt = ToDataTable(_cobieSpaces.ToArray(), "COBieSpace");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//if (_cobieZones.Count > 0)
			//{
			//    dt = ToDataTable(_cobieZones.ToArray(), "COBieZone");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//if (_cobieTypes.Count > 0)
			//{
			//    dt = ToDataTable(_cobieTypes.ToArray(), "COBieType");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//if (_cobieSystems.Count > 0)
			//{
			//    dt = ToDataTable(_cobieTypes.ToArray(), "COBieSystem");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//if (_cobieConnections.Count > 0)
			//{
			//    dt = ToDataTable(_cobieTypes.ToArray(), "COBieConnection");
			//    dsSheets.Tables.Add(dt.Copy());
			//}

			//return dsSheets;
		}

		public void PopulateErrors()
		{
			//// loop through all the sheets and preopare error dataset
			//if (_cobieFloors.Rows.Count > 0)
			//{
			//    // loop through each floor row
			//    IEnumerable<PropertyInfo> Properties = typeof(COBieFloorRow).GetProperties(BindingFlags.Public | BindingFlags.Instance)
			//                                    .Where(prop => prop.GetSetMethod() != null);

			//    foreach (COBieFloorRow row in _cobieFloors.Rows)
			//    {
			//        // loop through each column, get its attributes and check if column value matches the attributes constraints
			//        foreach (PropertyInfo propInfo in Properties)
			//        {
			//            COBieCell cell = row[propInfo.Name];
			//            COBieError err = GetCobieError(cell, "COBieFloor");

			//            // check for primary key
			//            if (HasDuplicateFloorValues(_cobieFloors, cell.CellValue))
			//            {
			//                err.ErrorDescription = cell.CellValue + " duplication";
			//                err.ErrorType = COBieError.ErrorTypes.PrimaryKey_Violation;
			//            }

			//            if (err.ErrorType != COBieError.ErrorTypes.None) _cobieErrors.Add(err);
			//        }
			//    }
			//}
		}
	}
}
