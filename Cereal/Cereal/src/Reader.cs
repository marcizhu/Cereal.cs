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

namespace Cereal
{
	public static class Reader
	{
		public static Int64 ReadBytesInt64(byte[] src, uint pointer)
		{
			int hiByte = ReadBytesInt32(src, pointer);
			int loByte = ReadBytesInt32(src, pointer + sizeof(int));

			Int64 temp = (uint)hiByte;
			temp = temp << (sizeof(int) * 8);
			temp |= (uint)loByte;

			return temp;
		}

		public static int ReadBytesInt32(byte[] src, uint pointer)
		{
			int ret = 0;

			for(int i = 0; i < sizeof(int); i++)
			{
				ret |= src[pointer + i] << ((sizeof(int) * 8 - 8) - (i * 8));
			}

			return ret;
		}

		public static bool ReadBytesBool(byte[] src, uint pointer) { return src[pointer] != 0; }

		public static short ReadBytesShort(byte[] src, uint pointer)
		{
			int ret = 0;

			for (int i = 0; i < sizeof(short); i++)
			{
				ret |= src[pointer + i] << ((sizeof(short) * 8 - 8) - (short)(i * 8));
			}

			return (short)ret;
		}

		public static byte ReadBytesByte(byte[] src, uint pointer) { return src[pointer]; }

		public static char ReadBytesChar(byte[] src, uint pointer) { return (char)src[pointer]; }

		public static float ReadBytesFloat(byte[] src, uint pointer)
		{
			uint value = (uint)ReadBytesInt32(src, pointer);

			byte[] result = new byte[sizeof(float)];

			for (int i = 0; i < sizeof(float); i++)
			{
				result[i] = BitConverter.GetBytes(value)[i];
			}

			return BitConverter.ToSingle(result, 0);
		}

		public static double ReadBytesDouble(byte[] src, uint pointer)
		{
			UInt64 value = (UInt64)ReadBytesInt64(src, pointer);

			byte[] result = new byte[sizeof(double)];

			for (int i = 0; i < sizeof(double); i++)
			{
				result[i] = BitConverter.GetBytes(value)[i];
			}

			return BitConverter.ToDouble(result, 0);
		}

		public static string ReadBytesString(byte[] src, uint pointer)
		{
			string value = "";

			ushort size = (ushort)ReadBytesShort(src, pointer);

			for (uint i = pointer + 2; i < pointer + size + 2; i++)
			{
				value += ReadBytesChar(src, i);
			}

			return value;
		}
	};
}