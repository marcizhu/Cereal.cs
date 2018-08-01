//  Cereal: A C++/C# Serialization library
//  Copyright (C) 2016  The Cereal Team
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Cereal.Global;

namespace Cereal
{
	public class Field
	{
		private string name;
		private DataType dataType;
		private byte[] data = null;

		private void SetData<T>(DataType type, T value, string fName)
		{
			dataType = type;
			name = fName;

			if (data != null)
				data = null;

			//Setting the data
			data = new byte[Marshal.SizeOf(typeof(T))];
			Writer.WriteBytes<T>(data, 0, value);
		}

		private void SetData(DataType type, bool value, string fName)
		{
			dataType = type;
			name = fName;

			if (data != null)
				data = null;

			//Setting the data
			data = new byte[1];
			Writer.WriteBytes(data, 0, value);
		}

		private void SetData(DataType type, float value, string fName)
		{
			dataType = type;
			name = fName;

			if (data != null)
				data = null;

			//Setting the data
			data = new byte[sizeof(float)];
			Writer.WriteBytes(data, 0, value);
		}

		private void SetData(DataType type, double value, string fName)
		{
			dataType = type;
			name = fName;

			if (data != null)
				data = null;

			//Setting the data
			data = new byte[sizeof(double)];
			Writer.WriteBytes(data, 0, value);
		}

		private void SetData(DataType type, string value, string fName)
		{
			dataType = type;
			name = fName;

			//Setting the data
			if (data != null)
				data = null;

			data = new byte[value.Length + sizeof(short)];

			uint ptr = Writer.WriteBytes<ushort>(data, 0, (ushort)value.Length);

			for (int i = 0; i < value.Length; i++)
			{
				ptr = Writer.WriteBytes<char>(data, ptr, value[i]);
			}
		}

		//constructor for each field type
		public Field()
		{
			data = null;
			dataType = DataType.DATA_UNKNOWN;
			name = "";
		}

		public Field(string name, byte value) { SetData<byte>(DataType.DATA_CHAR /* | MOD_UNSIGNED*/, value, name); }
		public Field(string name, bool value) { SetData(DataType.DATA_BOOL, value, name); }
		public Field(string name, char value) { SetData<char>(DataType.DATA_CHAR, value, name); }
		public Field(string name, short value) { SetData<short>(DataType.DATA_SHORT, value, name); }
		public Field(string name, int value) { SetData<int>(DataType.DATA_INT, value, name); }
		public Field(string name, Int64 value) { SetData<Int64>(DataType.DATA_LONG_LONG, value, name); }
		public Field(string name, float value) { SetData(DataType.DATA_FLOAT, value, name); }
		public Field(string name, double value) { SetData(DataType.DATA_DOUBLE, value, name); }
		public Field(string name, string value) { SetData(DataType.DATA_STRING, value, name); }

		~Field()
		{
			if (data != null)
				data = null;
		}

		public bool Write(ref Buffer buffer)
		{
			if (!buffer.HasSpace(Size)) return false;

			buffer.WriteBytes<byte>((byte)DataType.DATA_FIELD);
			buffer.WriteBytes(name);
			buffer.WriteBytes<byte>((byte)dataType); //write data type

			if (dataType != DataType.DATA_STRING)
			{
				for (int i = 0; i < SizeOf(dataType); i++)
				{
					buffer.WriteBytes<byte>(data[i]);
				}
			}
			else
			{
				short len = Reader.ReadBytesShort(data, 0);
				len += 2;

				for (int i = 0; i<len; i++)
				{
					buffer.WriteBytes<byte>(data[i]);
				}
			}

			return true;
		}

		public void Read(ref Buffer buffer)
		{
			byte type = buffer.ReadBytesByte();

			Debug.Assert(type == (byte)Global.DataType.DATA_FIELD);

			string sname = buffer.ReadBytesString();

			DataType dataType = (DataType)buffer.ReadBytesByte();

			switch (dataType)
			{
				case DataType.DATA_BOOL: SetData(dataType, buffer.ReadBytesBool(), sname); break;
				case DataType.DATA_CHAR: SetData<byte>(dataType, buffer.ReadBytesByte(), sname); break;
				case DataType.DATA_SHORT: SetData<short>(dataType, buffer.ReadBytesShort(), sname); break;
				case DataType.DATA_INT: SetData<int>(dataType, buffer.ReadBytesInt32(), sname); break;
				case DataType.DATA_LONG_LONG: SetData<Int64> (dataType, (long)buffer.ReadBytesInt64(), sname); break;
				case DataType.DATA_FLOAT: SetData(dataType, buffer.ReadBytesFloat(), sname); break;
				case DataType.DATA_DOUBLE: SetData(dataType, buffer.ReadBytesDouble(), sname); break;
				case DataType.DATA_STRING: SetData(dataType, buffer.ReadBytesString(), sname); break;
				default: throw new ArgumentOutOfRangeException("dataType", "Invalid data type!");
			}
		}

		public byte GetByte() { return Reader.ReadBytesByte(data, 0); }
		public bool GetBool() { return Reader.ReadBytesBool(data, 0); }
		public char GetChar() { return Reader.ReadBytesChar(data, 0); }
		public short GetShort() { return Reader.ReadBytesShort(data, 0); }
		public int GetInt32() { return Reader.ReadBytesInt32(data, 0); }
		public float GetFloat() { return Reader.ReadBytesFloat(data, 0); }
		public Int64 GetInt64() { return Reader.ReadBytesInt64(data, 0); }
		public double GetDouble() { return Reader.ReadBytesDouble(data, 0); }
		public string GetString() { return Reader.ReadBytesString(data, 0); }

		#region Properties
		public string Name
		{
			get { return name; }
			set { if (string.IsNullOrEmpty(value) == false) name = value; }
		}

		public uint Size
		{
			get
			{
				if (dataType == DataType.DATA_STRING)
				{
					return (uint)(sizeof(byte) + sizeof(short) + name.Length + sizeof(byte) + sizeof(short) + (ushort)Reader.ReadBytesShort(data, 0));
				}

				return (uint)(sizeof(byte) + sizeof(short) + name.Length + sizeof(byte) + SizeOf(dataType));
			}
		}

		public DataType DataType
		{
			get { return DataType; }
		}
		#endregion
	}
}