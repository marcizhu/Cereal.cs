using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Cereal;

namespace Cereal_CSharp
{
	class Program
	{
		static void Main(string[] args)
		{
			Cereal.Buffer buff = new Cereal.Buffer(64);
			buff.ReadFile("dictionaries-english.db");

			Cereal.Database db = new Cereal.Database();
			db.Read(ref buff);

			List<string> words = db.GetObject("English").GetArray("words").GetArray();

			foreach(string s in words)
			{
				Console.WriteLine(s);
			}

			db = null;

			buff.Clear();

			Cereal.Object obj = new Cereal.Object("Object");
			Cereal.Field field = new Cereal.Field("test", 3.14159265);

			obj.Fields.Add(field);

			field.Write(ref buff);

			buff.Position = 0;
			Cereal.Field f = new Cereal.Field();
			f.Read(ref buff);

			double val = f.GetDouble();

			Console.WriteLine("Value: {0}", f.GetDouble());

			Console.Write("\nDone.");
			Console.ReadKey();
		}
	}
}
