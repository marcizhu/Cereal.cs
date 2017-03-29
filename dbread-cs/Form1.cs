using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Cereal;

namespace dbread_cs
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void addItem(string text, string parent = null)
		{
			if(parent == null)
			{
				TreeNode node = new TreeNode();
				node.Name = text;
				node.Text = text;
				treeView1.Nodes.Add(node);
			}
			else
			{
				TreeNode node = treeView1.Nodes.Find(parent, true)[0];
				TreeNode newNode = new TreeNode();
				newNode.Name = text;
				newNode.Text = text;

				node.Nodes.Add(newNode);
			}
		}

		public void load(string[] args)
		{
			addItem(args[0]);

			Cereal.Buffer buff = new Cereal.Buffer(0);
			buff.readFile(args[0]);

			if(buff.Data[0] == 0x52 && buff.Data[1] == 0x4D)
			{
				Header header = new Header();
				header.read(ref buff);

				foreach(Database db in header.Databases)
				{
					addItem(db.Name, args[0]);

					foreach(Cereal.Object obj in db.Objects)
					{
						addItem(obj.Name, db.Name);

						foreach(Cereal.Array arr in obj.Arrays)
						{
							addItem("Array: " + arr.Name, obj.Name);
						}

						foreach(Field f in obj.Fields)
						{
							addItem("Field: " + f.Name, obj.Name);
						}
					}
				}
			}
			else
			{
				Database db = new Database();
				db.read(ref buff);

				addItem(db.Name, args[0]);

				foreach (Cereal.Object obj in db.Objects)
				{
					addItem(obj.Name, db.Name);

					foreach (Cereal.Array arr in obj.Arrays)
					{
						addItem("Array: " + arr.Name, obj.Name);
					}

					foreach (Field f in obj.Fields)
					{
						addItem("Field: " + f.Name, obj.Name);
					}
				}
			}

			treeView1.ExpandAll();
		}
	}
}
