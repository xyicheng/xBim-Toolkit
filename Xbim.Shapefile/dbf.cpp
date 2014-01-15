#pragma once
#include "stdafx.h"
#include "shapefile.h"
#include <vcclr.h>
#include <msclr/marshal.h>
using namespace msclr::interop;
using namespace Xbim::Shapefile;


namespace Xbim 
{
	namespace Shapefile
	{
		DBFObjectData::DBFObjectData(String^ SHPname)
		{
			marshal_context ^ context = gcnew marshal_context();
			const char* name = context->marshal_as<const char*>(SHPname);
			//delete context;

			this->dbfHandle = DBFCreate(name);
			this->shpID = 0;
			this->locked = false;

			this->dbfFieldTypeEnum = gcnew List<DBFFieldType>();
			this->dbfFieldName = gcnew List<String^>();
			this->fieldWidth = gcnew List<int>();
			this->fieldDecimals = gcnew List<int>();

			this->values = gcnew List<Object^>();
		}

		//void DBFObjectData::addFields(List<DBFFieldType>^ dbfFieldTypeEnum, List<String^>^ dbfFieldName, List<int>^ fieldWidth, List<int>^ fieldDecimals)
		//{
		//	int count = this->dbfFieldName->Count;
		//	if (count != dbfFieldName->Count || count != fieldWidth->Count || count != fieldDecimals->Count)
		//	{
		//		throw "All fields must be defined by all of their parameters (type, name, width and number of decimals)";
		//	}
		//
		//	if (this->locked)
		//	{
		//		throw "Once data has been written into the file, it is impossible to add a new field.";
		//	}
		//
		//	if (this->dbfFieldName->Count == 0) 
		//	{
		//		this->dbfFieldTypeEnum = dbfFieldTypeEnum ;
		//		this->dbfFieldName = dbfFieldName ;
		//		this->fieldWidth = fieldWidth ;
		//		this->fieldDecimals = fieldDecimals ;
		//	}
		//	else
		//	{
		//		this->dbfFieldTypeEnum->AddRange(dbfFieldTypeEnum) ;
		//		this->dbfFieldName->AddRange(dbfFieldName) ;
		//		this->fieldWidth->AddRange(fieldWidth) ;
		//		this->fieldDecimals->AddRange(fieldDecimals) ;
		//	}
		//}

		//void DBFObjectData::addField(DBFFieldType dbfFieldType, String^ dbfFieldName, int fieldWidth, int fieldDecimals)
		//{
		//	if (this->locked)
		//	{
		//		throw "Once data has been written into the file, it is impossible to add a new field.";
		//	}
		//	this->dbfFieldTypeEnum->Add(dbfFieldType) ;
		//	this->dbfFieldName->Add(dbfFieldName) ;
		//	this->fieldWidth->Add(fieldWidth) ;
		//	this->fieldDecimals->Add(fieldDecimals) ;
		//}

		//void DBFObjectData::addValues(List<Object^>^ values)
		//{
		//	if ((this->dbfFieldTypeEnum->Count - this->values->Count) < values->Count)
		//	{
		//		throw "Number of inserting values is bigger than number of defined fields.";
		//	}
		//
		//	if (this->values->Count == 0)
		//	{
		//		this->values = values;
		//	}
		//	else
		//	{
		//		this->values->AddRange(values);
		//	}
		//}

		void DBFObjectData::addValue(Object^ value, DBFFieldType dbfFieldType, String^ dbfFieldName, int fieldWidth, int fieldDecimals)
		{
			if (!this->locked)
			{
				if (fieldWidth == 0) 
					fieldWidth = 1;
				this->dbfFieldTypeEnum->Add(dbfFieldType) ;
				this->dbfFieldName->Add(dbfFieldName) ;
				this->fieldWidth->Add(fieldWidth) ;
				this->fieldDecimals->Add(fieldDecimals) ;
			}
			this->values->Add(value);
		}


		void DBFObjectData::writeParams()
		{
			Console::WriteLine("Shape ID:");
			Console::WriteLine(this->shpID);
			Console::WriteLine("Field data types:");
			for each (int i in dbfFieldTypeEnum)
			{
				Console::Write("{0} ", (DBFFieldType)i);
			}
			Console::WriteLine();
			Console::WriteLine("Field name:");
			for each (String^ i in dbfFieldName)
			{
				Console::Write("{0} ", i);
			}
			Console::WriteLine();
			Console::WriteLine("Decimal numbers:");
			for each (int i in fieldDecimals)
			{
				Console::Write("{0} ", i);
			}
			Console::WriteLine();
			Console::WriteLine("Values:");
			for each (Object^ i in values)
			{
				Console::Write("{0} ", i);
			}
			Console::WriteLine();
		}

		void DBFObjectData::clearData()
		{
			this->values->Clear();
		}

		void DBFObjectData::writeRow()
		{	
			int count = this->dbfFieldTypeEnum->Count;
			if (count != this->values->Count)
			{
				throw gcnew Exception("Some fields does not have value.");
			}

			if (!this->locked)
			{
				for(int i = 0; i < count; i++) 
				{
					const char* fieldName = nullptr;
					marshal_context ^ context = gcnew marshal_context();
					fieldName = context->marshal_as<const char*>((String^)(this->dbfFieldName[i]));
					//delete context;

					DBFAddField(this->dbfHandle, fieldName, (int)this->dbfFieldTypeEnum[i], this->fieldWidth[i], this->fieldDecimals[i]);
				}

				this->locked = true;
			}

			for (int i = 0; i < count; i++)
			{
				int type = (int)this->dbfFieldTypeEnum[i];

				const char* str = nullptr;
				if (type == FTString) 
				{
					marshal_context ^ context = gcnew marshal_context();
					str = context->marshal_as<const char*>((String^)(this->values[i]));
					//delete context;
				}
				int valInt = 0;
				double valDouble = 0;
				char valBool = 'F';
				Object^ value = this->values[i];

				switch (type) 
				{
				case FTString: 
					DBFWriteStringAttribute( this->dbfHandle, this->shpID, i, str);
					break;

				case FTInteger: 
					valInt = Convert::ToInt32(value);
					DBFWriteIntegerAttribute( this->dbfHandle, this->shpID, i, valInt );
					break;

				case FTDouble: 
					valDouble = Convert::ToDouble(value);
					DBFWriteDoubleAttribute( this->dbfHandle, this->shpID, i, valDouble );
					break;

				case FTLogical: 
					valBool = Convert::ToBoolean(value)?'T':'F';
					DBFWriteLogicalAttribute( this->dbfHandle, this->shpID, i, valBool );
					break;

				default:
					throw gcnew Exception("Data type of the field not specified.");
				}
			}

			this->clearData();
			this->shpID++;
		}

		DBFObjectData::~DBFObjectData()
		{
			delete this->dbfFieldTypeEnum;
			delete this->dbfFieldName;
			delete this->fieldWidth;
			delete this->fieldDecimals;
			delete this->values;

			//calling finalizer
			this->!DBFObjectData();
		}

		DBFObjectData::!DBFObjectData()
		{
			DBFClose(this->dbfHandle);
			//I can't use this method, because all resources are freed by the previous C function and next calling of free raises assertion.
			//this->InstanceCleanup();
		}

	}
}