using namespace System;
using namespace System::Collections::Generic;

namespace Xbim
{
	namespace Shapefile 
	{
		public enum class DBFFieldType 
		{
			FTString = 0,
			FTInteger = 1,
			FTDouble = 2,
			FTLogical = 3,
		};

		public interface class IShapefileDBFWriter
		{
			//void addFields(List<DBFFieldType>^ dbfFieldTypeEnum, List<String^>^ dbfFieldName, List<int>^ fieldWidth, List<int>^ fieldDecimals);
			//void addField(DBFFieldType dbfFieldType, String^ dbfFieldName, int fieldWidth, int fieldDecimals);
			//void addValues(List<Object^>^ values);
			void addValue(Object^ value, DBFFieldType dbfFieldType, String^ dbfFieldName, int fieldWidth, int fieldDecimals);
			void writeParams();
			void clearData();
			void writeRow();
			int getID();
		};
	}
}