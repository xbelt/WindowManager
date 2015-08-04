using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using WindowSelector.Configuration;

namespace WindowSelector
{
    public partial class LayoutSettings : Form
    {
        List<Position> _currentList = new List<Position>();
        private int currentIndex;

        public LayoutSettings()
        {
            InitializeComponent();

            panel1.Click += ChangeColor;
            listView1.Items[0].Selected = true;
            listView1.Select();

            checkBox2.Checked = rkApp.GetValue("WindowSelector") != null;
        }

        private void ChangeColor(object sender, EventArgs e)
        {
            var dialog = new ColorDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                panel1.BackColor = dialog.Color;
                Config.Settings["Settings"]["Color"].intValue = dialog.Color.ToArgb();
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var lv = (ListView)sender;
            if (lv.SelectedIndices.Count == 0)
                return;
            currentIndex = lv.SelectedIndices[0] + 1;
            var positionList = ReadPositions();
            _currentList = positionList;
            listView2.Clear();
            foreach (var position in positionList)
            {
                listView2.Items.Add(position.Pretty());
            }
        }

        private List<Position> ReadPositions()
        {
            var positionList =
                JsonConvert.DeserializeObject<List<Position>>(
                    Config.Settings["Positions"]["N" + currentIndex].Value);
            return positionList;
        }

        private void WritePositions(List<Position> input)
        {
            var json = JsonConvert.SerializeObject(input, Formatting.None, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
            
            Config.Settings["Positions"]["N" + currentIndex].Value = json;
            Config.Commit();
        }

        private void moveUpButton_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count == 0)
            {
                return;
            }
            var selectedIndex = listView2.SelectedIndices[0];
            var targetIndex = Math.Max(0, selectedIndex - 1);

            MoveElement(selectedIndex, targetIndex);

            listView2.Items[targetIndex].Selected = true;
            listView2.Select();

            WritePositions(_currentList);
        }

        private void moveDownButton_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count == 0)
            {
                return;
            }
            var selectedIndex = listView2.SelectedIndices[0];
            var targetIndex = Math.Min(_currentList.Count - 1, selectedIndex + 1);

            MoveElement(selectedIndex, targetIndex);

            listView2.Items[targetIndex].Selected = true;
            listView2.Select();

            WritePositions(_currentList);
        }

        private void MoveElement(int selectedIndex, int targetIndex)
        {
            var toBeMoved = _currentList[selectedIndex];
            _currentList.RemoveAt(selectedIndex);

            _currentList.Insert(targetIndex, toBeMoved);

            listView2.Items.Clear();
            _currentList.ForEach(x => listView2.Items.Add(x.Pretty()));
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var targetIndex = _currentList.Count;
            if (listView2.SelectedIndices.Count != 0)
            {
                targetIndex = listView2.SelectedIndices[0] + 1;
            }

            var dialog = new RuleManager();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _currentList.Insert(targetIndex, dialog.Result);
                listView2.Items.Clear();
                _currentList.ForEach(x => listView2.Items.Add(x.Pretty()));

                listView2.Items[targetIndex].Selected = true;
                listView2.Select();

                WritePositions(_currentList);
            }

        }

        private void editButton_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count == 0)
            {
                MessageBox.Show("Please select a rule to edit", "Invalid selection", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var targetIndex = listView2.SelectedIndices[0];
            var dialog = new RuleManager(_currentList[targetIndex]);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _currentList[targetIndex] = dialog.Result;
                listView2.Items.Clear();
                _currentList.ForEach(x => listView2.Items.Add(x.Pretty()));

                listView2.Items[targetIndex].Selected = true;
                listView2.Select();

                WritePositions(_currentList);
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count == 0)
            {
                MessageBox.Show("Please select a rule to delete", "Invalid selection", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            if (
                MessageBox.Show("Are you sure you want to delete the selected rule?", "Delete rule",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                _currentList.RemoveAt(listView2.SelectedIndices[0]);
                listView2.Items.Clear();
                _currentList.ForEach(x => listView2.Items.Add(x.Pretty()));

                listView2.Select();

                WritePositions(_currentList);
            }
        }

        RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                // Add the value in the registry so that the application runs at startup
                rkApp.SetValue("WindowSelector", Application.ExecutablePath);
            }
            else
            {
                // Remove the value from the registry so that the application doesn't start
                rkApp.DeleteValue("WindowSelector", false);
            }
        }
    }
}
