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
	public class Database
	{
		private Global.Version version;
		private string name;
		private List<Object> objects = new List<Object>();

		// TODO: Fix
		private static uint crc32(byte[] message, uint len)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			uint mask = 0;
			uint crc;

			crc = 0xFFFFFFFF;

			for (uint i = 0; i < len; i++)
			{
				crc = crc ^  message[i];

				for (int j = 7; j >= 0; j--)
				{
					mask = (uint) -(crc & 1);
					crc = (crc >> 1) ^ (0xEDB88320 & mask);
				}
			}

			return ~crc;
		}

		private static uint crc32(byte[] message, uint offs, uint len)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			uint mask = 0;
			uint crc;

			crc = 0xFFFFFFFF;

			for (uint i = offs; i < (offs + len); i++)
			{
				crc = crc ^  message[i];

				for (int j = 7; j >= 0; j--)
				{
					mask = (uint) -(crc & 1);
					crc = (crc >> 1) ^ (0xEDB88320 & mask);
				}
			}

			return ~crc;
		}

		public Database()
		{
			name = "";
			version = Global.Version.VERSION_INVALID;
		}
		public Database(string dbName, Global.Version ver)
		{
			name = dbName;
			version = ver;
		}
		public Database(string dbName)
		{
			name = dbName;
			version = Global.Version.VERSION_LATEST;
		}

		~Database()
		{
			for (int i = 0; i < objects.Count; i++)
				objects[i] = null;
		}

		public void Read(ref Buffer buffer)
		{
			version = (Global.Version)buffer.ReadBytesShort();

			if (version == Global.Version.VERSION_INVALID) throw new ArgumentOutOfRangeException("version", "Invalid database version!");
			if (version > Global.Version.VERSION_LATEST) throw new ArgumentOutOfRangeException("version", "Unsupported version!");

			switch (version)
			{
				case Global.Version.VERSION_1_0:
					name = buffer.ReadBytesString();

					buffer.AddOffset(sizeof(uint)); //we skip the size (don't need it)

					ushort objectCount = (ushort)buffer.ReadBytesShort();

					for (ushort i = 0; i < objectCount; i++)
					{
						Object obj = new Object();

						obj.Read(ref buffer);
						AddObject(obj);
					}

					break;

				case Global.Version.VERSION_2_0:
					//throw new NotImplementedException();

					name = buffer.ReadBytesString();

					uint checksum = (uint)buffer.ReadBytesInt32();

					uint p = buffer.Position;
					uint size = (uint)buffer.ReadBytesInt32() - sizeof(short) - sizeof(short) - (uint)name.Length - sizeof(uint);

					if(crc32(buffer.Data, p, size) != checksum) throw new ArgumentOutOfRangeException("crc32", "Checksum mismatch!");

					// objectCount already defined in case VERSION_1_0
					objectCount = (ushort)buffer.ReadBytesShort();

					for (ushort i = 0; i < objectCount; i++)
					{
						Object obj = new Object();

						obj.Read(ref buffer);
						AddObject(obj);
					}

					break;

				default:
					throw new ArgumentOutOfRangeException("version", "Invalid database version!");
			}
		}

		public bool Write(ref Buffer buffer)
		{
			if (!buffer.HasSpace((uint)Size)) return false;

			buffer.WriteBytes<ushort>((ushort)version);

			if(version == Global.Version.VERSION_INVALID) throw new ArgumentOutOfRangeException("version", "Invalid database version!");

			switch (version)
			{
			case Global.Version.VERSION_1_0:
				if(objects.Count > 65536) throw new OverflowException("Too many objects!");
				if(this.Size > 4294967296) throw new OverflowException("Database size is too big!"); // 2^32, maximum database size

				buffer.WriteBytes(name);
				buffer.WriteBytes<uint>((uint)Size);
				buffer.WriteBytes<ushort>((ushort)objects.Count);

				foreach (Object obj in objects)
					obj.Write(ref buffer);

				break;

			case Global.Version.VERSION_2_0:
				//throw new NotImplementedException();

				if(objects.Count > 65536) throw new OverflowException("Too many objects!");
				if(this.Size > 4294967296) throw new OverflowException("Database size is too big!"); // 2^32, maximum database size

				uint size = (uint)this.Size - sizeof(short) - sizeof(short) - (uint)name.Length - sizeof(uint);

				Buffer tempBuffer = new Buffer(size);

				buffer.WriteBytes(name);

				tempBuffer.WriteBytes((uint)this.Size);
				tempBuffer.WriteBytes((ushort)objects.Count);

				foreach (Object obj in objects)
					obj.Write(ref tempBuffer);

				uint checksum = crc32(tempBuffer.Data, size);

				buffer.WriteBytes(checksum);
				buffer.Copy(ref tempBuffer);

				break;

			default:
				throw new ArgumentOutOfRangeException("version", "Invalid database version!");
			}

			return true;
		}

		public Object GetObject(string name)
		{
			foreach(Object obj in objects)
				if (obj.Name == name) return obj;

			return null;
		}

		public void AddObject(Object obj) { objects.Add(obj); }

		#region Properties
		public List<Object> Objects
		{
			get { return objects; }
		}

		public Global.Version Version
		{
			get { return version; }
			set
			{
				if (value <= Global.Version.VERSION_LATEST)
					version = value;
				else
					throw new ArgumentOutOfRangeException("version", "Invalid database version!");
			}
		}

		public string Name
		{
			get { return name; }
			set { if (string.IsNullOrEmpty(value) == false) name = value; }
		}

		public uint Size
		{
			get
			{
				uint ret = sizeof(short);

				switch (version)
				{
					case Global.Version.VERSION_1_0:
						ret += sizeof(short) + (uint)name.Length + sizeof(int) + sizeof(short);
						break;

					case Global.Version.VERSION_2_0:
						ret += sizeof(short) + (uint)name.Length + sizeof(int) + sizeof(int) + sizeof(short);
						break;

					default:
						throw new ArgumentOutOfRangeException("version", "Cannot calculate the database size with an unknown database version!"); // Invalid version
				}

				foreach (Object obj in objects)
					ret += obj.Size;

				return ret;
			}
		}
		#endregion
	};
}