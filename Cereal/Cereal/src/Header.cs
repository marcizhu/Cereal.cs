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

namespace Cereal
{
	public class Header
	{
		List<Database> databases = new List<Database>();

		public Header() { }

		~Header()
		{
			for (int i = 0; i < databases.Count; i++)
				databases[i] = null;
		}

		public void Read(ref Buffer buffer)
		{
			ushort magic = (ushort)buffer.ReadBytesShort();

			Debug.Assert(magic == Global.MAGIC_NUMBER);

			byte count = buffer.ReadBytesByte();

			List<uint> offsets = new List<uint>();

			for (byte i = 0; i < count; i++)
			{
				offsets.Add((uint)buffer.ReadBytesInt32());
			}

			foreach (uint offs in offsets)
			{
				Debug.Assert(buffer.Position == offs);

				buffer.Position = offs;

				Database db = new Database();

				db.Read(ref buffer);
				AddDatabase(db);
			}
		}

		public bool Write(ref Buffer buffer)
		{
			if (!buffer.HasSpace(Size)) return false;

			if(databases.Count > 255) throw new OverflowException("Too many databases!");

			buffer.WriteBytes<ushort>(Global.MAGIC_NUMBER);
			buffer.WriteBytes<byte>((byte)databases.Count);

			uint offset = (uint)(sizeof(short) + sizeof(byte) + (sizeof(uint) * databases.Count));

			for (int i = 0; i < databases.Count; i++)
			{
				buffer.WriteBytes<uint>(offset);

				offset += (uint)databases[i].Size;
			}

			foreach (Database db in databases)
				db.Write(ref buffer);

			return true;
		}

		public void AddDatabase(Database db) { databases.Add(db); }

		public Database GetDatabase(string name)
		{
			foreach (Database db in databases)
				if (db.Name == name) return db;

			return null;
		}

		#region Properties
		public uint Size
		{
			get
			{
				uint ret = (uint)(sizeof(short) + sizeof(byte) + (sizeof(uint) * databases.Count));

				foreach (Database db in databases)
					ret += (uint)db.Size;

				return ret;
			}
		}

		public List<Database> Databases
		{
			get { return databases; }
		}
		#endregion
	}
}