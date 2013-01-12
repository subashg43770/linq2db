﻿using System;
using System.Data;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;

using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace LinqToDB.DataProvider
{
	public class OracleDataProvider : DataProviderBase
	{
		public OracleDataProvider() : base(new OracleMappingSchema())
		{
			SetCharField("Char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("NChar", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<OracleDataReader,OracleBFile       >((r,i) => r.GetOracleBFile       (i));
			SetProviderField<OracleDataReader,OracleBinary      >((r,i) => r.GetOracleBinary      (i));
			SetProviderField<OracleDataReader,OracleBlob        >((r,i) => r.GetOracleBlob        (i));
			SetProviderField<OracleDataReader,OracleClob        >((r,i) => r.GetOracleClob        (i));
			SetProviderField<OracleDataReader,OracleDate        >((r,i) => r.GetOracleDate        (i));
			SetProviderField<OracleDataReader,OracleDecimal     >((r,i) => r.GetOracleDecimal     (i));
			SetProviderField<OracleDataReader,OracleIntervalDS  >((r,i) => r.GetOracleIntervalDS  (i));
			SetProviderField<OracleDataReader,OracleIntervalYM  >((r,i) => r.GetOracleIntervalYM  (i));
			SetProviderField<OracleDataReader,OracleRef         >((r,i) => r.GetOracleRef         (i));
			SetProviderField<OracleDataReader,OracleString      >((r,i) => r.GetOracleString      (i));
			SetProviderField<OracleDataReader,OracleTimeStamp   >((r,i) => r.GetOracleTimeStamp   (i));
			SetProviderField<OracleDataReader,OracleTimeStampLTZ>((r,i) => r.GetOracleTimeStampLTZ(i));
			SetProviderField<OracleDataReader,OracleTimeStampTZ >((r,i) => r.GetOracleTimeStampTZ (i));
			SetProviderField<OracleDataReader,OracleXmlType     >((r,i) => r.GetOracleXmlType     (i));

			SetProviderField<OracleDataReader,DateTimeOffset,OracleTimeStampTZ> ((r,i) => GetOracleTimeStampTZ (r, i));
			SetProviderField<OracleDataReader,DateTimeOffset,OracleTimeStampLTZ>((r,i) => GetOracleTimeStampLTZ(r, i));
		}

		static DateTimeOffset GetOracleTimeStampTZ(OracleDataReader rd, int idx)
		{
			var tstz = rd.GetOracleTimeStampTZ(idx);
			return new DateTimeOffset(
				tstz.Year, tstz.Month,  tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second, (int)tstz.Millisecond,
				TimeSpan.Parse(tstz.TimeZone.TrimStart('+')));
		}

		static DateTimeOffset GetOracleTimeStampLTZ(OracleDataReader rd, int idx)
		{
			var tstz = rd.GetOracleTimeStampLTZ(idx).ToOracleTimeStampTZ();
			return new DateTimeOffset(
				tstz.Year, tstz.Month,  tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second, (int)tstz.Millisecond,
				TimeSpan.Parse(tstz.TimeZone.TrimStart('+')));
		}

		public override string Name           { get { return ProviderName.Oracle;     } }
		public override Type   ConnectionType { get { return typeof(OracleConnection); } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new OracleConnection(connectionString);
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(OracleDataReader));
		}

		#region Overrides

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (dataType == DataType.Undefined && value != null)
				dataType = MappingSchema.GetDataType(value.GetType());

			switch (dataType)
			{
				case DataType.DateTimeOffset  :
					if (value is DateTimeOffset)
					{
						var dto  = (DateTimeOffset)value;
						var zone = dto.Offset.ToString("hh\\:mm");
						if (!zone.StartsWith("-") && !zone.StartsWith("+"))
							zone = "+" + zone;
						value = new OracleTimeStampTZ(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, dto.Millisecond, zone);
					}
					break;
				case DataType.Boolean    :
					dataType = DataType.Byte;
					if (value is bool)
						value = (bool)value ? (byte)1 : (byte)0;
					break;
				case DataType.Byte       : dataType = DataType.Int16;   break;
				case DataType.SByte      : dataType = DataType.Int16;   break;
				case DataType.UInt16     : dataType = DataType.Int32;   break;
				case DataType.UInt32     : dataType = DataType.Int64;   break;
				case DataType.UInt64     : dataType = DataType.Decimal; break;
				case DataType.VarNumeric : dataType = DataType.Decimal; break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Guid       :
					if (value is Guid) value = ((Guid)value).ToByteArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Single         : ((OracleParameter)parameter).OracleDbType = OracleDbType.BinaryFloat;  break;
				case DataType.Double         : ((OracleParameter)parameter).OracleDbType = OracleDbType.BinaryDouble; break;
				case DataType.Text           : ((OracleParameter)parameter).OracleDbType = OracleDbType.Clob;         break;
				case DataType.NText          : ((OracleParameter)parameter).OracleDbType = OracleDbType.NClob;        break;
				case DataType.Image          : ((OracleParameter)parameter).OracleDbType = OracleDbType.Blob;         break;
				case DataType.Binary         : ((OracleParameter)parameter).OracleDbType = OracleDbType.Blob;         break;
				case DataType.VarBinary      : ((OracleParameter)parameter).OracleDbType = OracleDbType.Blob;         break;
				case DataType.Date           : ((OracleParameter)parameter).OracleDbType = OracleDbType.Date;         break;
				case DataType.SmallDateTime  : ((OracleParameter)parameter).OracleDbType = OracleDbType.Date;         break;
				case DataType.DateTime2      : ((OracleParameter)parameter).OracleDbType = OracleDbType.TimeStamp;    break;
				case DataType.DateTimeOffset : ((OracleParameter)parameter).OracleDbType = OracleDbType.TimeStampTZ;  break;
				case DataType.Guid           : ((OracleParameter)parameter).OracleDbType = OracleDbType.Raw;          break;
				default                      : base.SetParameterType(parameter, dataType);                            break;
			}
		}

		#endregion
	}
}
