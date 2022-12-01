using System;
using System.Linq;
using System.Windows.Forms;

namespace SNSS_Reader
{
	public partial class SnssForm : Form
	{
		Snss File;

		public SnssForm()
		{
			InitializeComponent();
			File = null;
		}

		void aboutToolStripMenuItem_Click(object sender, EventArgs e) => MessageBox.Show(
			"Simplest reader of Snss format. Version 19-11-30\n\n" +
			"By phacox.cll\n\n" +
			"Based on:\n" +
			"  https://digitalinvestigation.wordpress.com/2012/09/03/chrome-session-and-tabs-files-and-the-puzzle-of-the-pickle/",
			"About", MessageBoxButtons.OK, MessageBoxIcon.None);

		void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			openFileDialog.FileName = "";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				var fileName = openFileDialog.FileName;
				Open(fileName);
			}
		}

		void Open(string fileName)
		{
			File = new Snss(fileName);

			richTextBox.Clear();
			treeView.Nodes.Clear();

			var root = new TreeNode("Snss")
			{
				Name = "Snss"
			};
			treeView.Nodes.Add(root);

			for (var i = 0; i < File.Commands.Count; i++)
			{
				var node = new TreeNode("[" + i + "] Id: " + File.Commands[i].Id);
				root.Nodes.Add(node);
			}
		}

		void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (File == null || treeView.SelectedNode == null) return;
			try
			{
				if (treeView.SelectedNode.Name == "Snss")
				{
					richTextBox.Clear();
					richTextBox.AppendText(File + "\n");
					if (File.Version != 0)
					{
						richTextBox.AppendText("URLs:\n");
						for (var i = 0; i < File.Commands.Count; i++)
						{
							if (File.Commands[i].Content is Snss.Tab)
								richTextBox.AppendText(((Snss.Tab) File.Commands[i].Content).URL + "\n");
						}
					}
				}
				else
				{
					richTextBox.Clear();
					richTextBox.AppendText(File.Commands[treeView.SelectedNode.Index].ToString());
				}
			}
			catch
			{
				/**/
			}
		}

		void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

		void treeView_DragDrop(object sender, DragEventArgs e)
		{
			var files = e.Data.GetData(DataFormats.FileDrop) as string[]; // get all files droppeds  
			if (files != null && files.Any())
				Open(files.First());
		}

		void treeView_DragEnter(object sender, DragEventArgs e)
		{
		}

		void treeView_DragOver(object sender, DragEventArgs e) => e.Effect =
			e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;

		#region TextBox Events

        void Undo()
		{
			if (richTextBox.CanUndo)
				richTextBox.Undo();
		}

		void Redo()
		{
			if (richTextBox.CanRedo)
				richTextBox.Redo();
		}

		void Cut()
		{
			if (richTextBox.SelectionLength > 0)
				richTextBox.Cut();
		}

		void Copy()
		{
			if (richTextBox.SelectionLength > 0)
				richTextBox.Copy();
		}

		void Paste()
		{
			if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Text))
				richTextBox.Paste();
		}

		void Delete()
		{
			if (richTextBox.SelectionLength > 0)
				richTextBox.SelectedText = "";
		}

		void SelectAll()
		{
			richTextBox.Select();
			richTextBox.SelectAll();
		}

		void undoToolStripMenuItem_Click(object sender, EventArgs e) => Undo();

		void redoToolStripMenuItem_Click(object sender, EventArgs e) => Redo();

		void cutToolStripMenuItem_Click(object sender, EventArgs e) => Cut();

		void copyToolStripMenuItem_Click(object sender, EventArgs e) => Copy();

		void pasteToolStripMenuItem_Click(object sender, EventArgs e) => Paste();

		void deleteToolStripMenuItem_Click(object sender, EventArgs e) => Delete();

		void selectAllToolStripMenuItem_Click(object sender, EventArgs e) => SelectAll();

		void cutToolStripMenuItem1_Click(object sender, EventArgs e) => Cut();

		void copyToolStripMenuItem1_Click(object sender, EventArgs e) => Copy();

		void pasteToolStripMenuItem1_Click(object sender, EventArgs e) => Paste();

		void deleteToolStripMenuItem1_Click(object sender, EventArgs e) => Delete();

		void selectAllToolStripMenuItem1_Click(object sender, EventArgs e) => SelectAll();

		#endregion TextBox Events
	}
}