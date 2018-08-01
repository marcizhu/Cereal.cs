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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Cereal.Global;

namespace Cereal
{
	public class Array
	{
		private string name;
		private uint count; // item count
		private DataType dataType;
		private uint size = 0;
		private byte[] data;

		private void SetData<T>(DataType type, T[] value, string arrayName)
		{
			if (type == DataType.DATA_UNKNOWN) return;

			name = arrayName;
			count = (uint)value.Length;
			dataType = type;

			if (data != null)
				data = null;

			data = new byte[SizeOf(type) * count];

			if((count * Marshal.SizeOf(typeof(T))) > 4294967296) throw new OverflowException("Array size is too big!"); // Maximum item count (overflow of pointer and buffer)

			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				pointer = Writer.WriteBytes<T>(data, pointer, value[i]);
			}
		}

		private void SetData(DataType type, bool[] value, string arrayName)
		{
			if (type == DataType.DATA_UNKNOWN) return;

			name = arrayName;
			count = (uint)value.Length;
			dataType = type;

			if (data != null)
				data = null;

			data = new byte[count];

			if((count * Marshal.SizeOf(typeof(bool))) > 4294967296) throw new OverflowException("Array size is too big!"); // Maximum item count (overflow of pointer and buffer)
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				pointer = Writer.WriteBytes(data, pointer, value[i]);
			}
		}

		void SetData(DataType type, string[] value, string name)
		{
			count = (uint)value.Length;
			dataType = type;

			if (data != null)
				data = null;

			size = 0;

			for (uint i = 0; i < count; i++)
			{
				size += 2;
				size += (uint)value[i].Length;
			}

			data = new byte[size];

			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				pointer = Writer.WriteBytes(data, pointer, value[i]);
			}
		}

		// public
		public Array() { SetData<byte>(DataType.DATA_UNKNOWN, null, ""); }
		public Array(string name, byte[] value) { SetData<byte>(DataType.DATA_CHAR, value, name); }
		public Array(string name, bool[] value) { SetData(DataType.DATA_BOOL, value, name); }
		public Array(string name, char[] value) { SetData<char>(DataType.DATA_CHAR, value, name); }
		public Array(string name, short[] value) { SetData<short>(DataType.DATA_SHORT, value, name); }
		public Array(string name, int[] value) { SetData<int>(DataType.DATA_INT, value, name); }
		public Array(string name, float[] value) { SetData<float>(DataType.DATA_FLOAT, value, name); }
		public Array(string name, UInt64[] value) { SetData<UInt64>(DataType.DATA_LONG_LONG, value, name); }
		public Array(string name, double[] value) { SetData<double>(DataType.DATA_DOUBLE, value, name); }
		public Array(string name, string[] value) { SetData(DataType.DATA_STRING, value, name); }

		~Array()
		{
			if (data != null)
				data = null;
		}

		public bool Write(ref Buffer buffer)
		{
			if (!buffer.HasSpace(Size)) return false;

			buffer.WriteBytes<byte>((byte)DataType.DATA_ARRAY);
			buffer.WriteBytes(name);
			buffer.WriteBytes<byte>((byte)dataType);
			buffer.WriteBytes<uint>(count);

			uint s;

			if (dataType != DataType.DATA_STRING)
				s = SizeOf(dataType) * count;
			else
				s = size;

			buffer.Copy(data, s);

			return true;
		}

		public void Read(ref Buffer buffer)
		{
			DataType type = (DataType)buffer.ReadBytesByte();

			Debug.Assert(type == DataType.DATA_ARRAY);

			name = buffer.ReadBytesString();

			dataType = (DataType)buffer.ReadBytesByte();
			count = (uint)buffer.ReadBytesInt32();

			if (data != null)
				data = null;

			if (dataType != DataType.DATA_STRING)
			{
				data = new byte[count * SizeOf(dataType)];

				System.Array.Copy(buffer.Data, buffer.Position, data, 0, count * SizeOf(dataType));

				buffer.AddOffset(count * SizeOf(dataType));
			}
			else
			{
				uint start = buffer.Position;

				for (uint i = 0; i < count; i++)
				{
					buffer.ReadBytesString();
				}

				size = buffer.Position - start;

				data = new byte[size];

				System.Array.Copy(buffer.Data, start, data, 0, size);
			}
		}

		public List<T> GetArray<T>()
		{
			/*List<T> ret = new List<T>();

			uint pointer = 0;

			for (int i = 0; i < count; i++)
			{
				ret.Add(Reader.readBytes<T>(data, pointer));

				pointer += (uint)Marshal.SizeOf(typeof(T));
			}

			return ret;*/
			throw new NotImplementedException();
		}

		public List<string> GetArray()
		{
			List<string> ret = new List<string>();

			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				ret.Add(Reader.ReadBytesString(data, pointer));

				pointer += (ushort)Reader.ReadBytesShort(data, pointer) + (uint)sizeof(ushort);
			}

			return ret;
		}

		// This returns the data in little endian (necessary for >1 byte data types like shorts or ints)
		#region GetRawArray()
		public bool[] GetRawArray(bool[] mem)
		{
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				mem[i] = Reader.ReadBytesBool(data, pointer);

				pointer += (uint)sizeof(bool);
			}

			return mem;
		}

		public byte[] GetRawArray(byte[] mem)
		{
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				mem[i] = Reader.ReadBytesByte(data, pointer);

				pointer += (uint)sizeof(byte);
			}

			return mem;
		}

		public char[] GetRawArray(char[] mem)
		{
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				mem[i] = Reader.ReadBytesChar(data, pointer);

				pointer += (uint)sizeof(char);
			}

			return mem;
		}

		public short[] GetRawArray(short[] mem)
		{
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				mem[i] = Reader.ReadBytesShort(data, pointer);

				pointer += (uint)sizeof(short);
			}

			return mem;
		}

		public float[] GetRawArray(float[] mem)
		{
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				mem[i] = Reader.ReadBytesFloat(data, pointer);

				pointer += (uint)sizeof(float);
			}

			return mem;
		}

		public double[] GetRawArray(double[] mem)
		{
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				mem[i] = Reader.ReadBytesDouble(data, pointer);

				pointer += (uint)sizeof(double);
			}

			return mem;
		}

		public int[] GetRawArray(int[] mem)
		{
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				mem[i] = Reader.ReadBytesInt32(data, pointer);

				pointer += (uint)sizeof(int);
			}

			return mem;
		}

		public Int64[] GetRawArray(Int64[] mem)
		{
			uint pointer = 0;

			for (uint i = 0; i < count; i++)
			{
				mem[i] = Reader.ReadBytesInt64(data, pointer);

				pointer += (uint)sizeof(UInt64);
			}

			return mem;
		}

		public string[] GetRawArray(string[] mem)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Properties
		public string Name
		{
			get { return name; }
			set { if (string.IsNullOrEmpty(value) == false) name = value; }
		}

		public uint ItemCount
		{
			get { return count; }
		}

		public uint Size
		{
			get
			{
				if (dataType != DataType.DATA_STRING)
				{
					return sizeof(byte) + sizeof(short) + (uint)Name.Length + sizeof(byte) + sizeof(int) + count * SizeOf(dataType);
				}
				else
				{
					return sizeof(byte) + sizeof(short) + (uint)Name.Length + sizeof(byte) + sizeof(int) + size;
				}
			}
		}

		public DataType DataType
		{
			get { return dataType; }
		}
		#endregion
	}
}