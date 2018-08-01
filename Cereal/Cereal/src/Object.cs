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
using static Cereal.Global;

namespace Cereal
{
	public class Object
	{
		private string name;

		public Object(string objName) { name = objName; }
		public Object() { }

		~Object()
		{
			for (int i = 0; i < Arrays.Count; i++)
				Arrays[i] = null;

			for (int i = 0; i < Fields.Count; i++)
				Fields[i] = null;
		}

		public bool Write(ref Buffer buffer)
		{
			if (!buffer.HasSpace(Size)) return false;

			if(Fields.Count > 65535) throw new OverflowException("Too many fields!");
			if(Arrays.Count > 65535) throw new OverflowException("Too many arrays!");

			buffer.WriteBytes<byte>((byte)DataType.DATA_OBJECT);
			buffer.WriteBytes(name);
			buffer.WriteBytes<ushort>((ushort)Fields.Count);

			foreach (Field field in Fields)
				field.Write(ref buffer);

			buffer.WriteBytes<ushort>((ushort)Arrays.Count);

			foreach (Array array in Arrays)
				array.Write(ref buffer);

			return true;
		}

		public void AddField(Field field) { Fields.Add(field); }
		public void AddArray(Array array) { Arrays.Add(array); }

		public Field GetField(string name)
		{
			foreach (Field field in Fields)
				if (field.Name == name) return field;

			return null;
		}

		public Array GetArray(string name)
		{
			foreach(Array array in Arrays)
				if (array.Name == name) return array;

			return null;
		}

		public void Read(ref Buffer buffer)
		{
			byte type = buffer.ReadBytesByte();

			Debug.Assert(type == (byte)Global.DataType.DATA_OBJECT);

			name = buffer.ReadBytesString();

			ushort fieldCount = (ushort)buffer.ReadBytesShort();

			for (int i = 0; i < fieldCount; i++)
			{
				Field field = new Field();

				field.Read(ref buffer);
				AddField(field);
			}

			ushort arrayCount = (ushort)buffer.ReadBytesShort();

			for (int i = 0; i < arrayCount; i++)
			{
				Array array = new Array();

				array.Read(ref buffer);
				AddArray(array);
			}
		}

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
				uint ret = sizeof(byte) + sizeof(short) + (uint)name.Length + sizeof(short) + sizeof(short);

				foreach (Field field in Fields)
					ret += field.Size;

				foreach (Array array in Arrays)
					ret += array.Size;

				return ret;
			}
		}

		public List<Field> Fields { get; set; } = new List<Field>();
		public List<Array> Arrays { get; set; } = new List<Array>();
		#endregion
	}
}