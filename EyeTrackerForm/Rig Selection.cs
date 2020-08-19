using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace EyeTrackerForm
{
    public partial class RigSelection : Form
    {
        public string SelectedRig
        {
            get { return selectRig.SelectedItem.ToString(); }
        }

        public string CohortNumber
        {
            get { return textBox1.Text.ToString(); }
        }

        public RigSelection()
        {
            InitializeComponent();
            var rig_list = new List<string>(ConfigurationManager.AppSettings["rig_list"].Split(new char[] { ';' }));

            for (int i = 0; i < rig_list.Count; i++)
            {
                this.selectRig.Items.Add(rig_list[i]);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void confirmButton_Click(object sender, EventArgs e)
        {
                Close();
        }

        private void selectRig_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && !int.TryParse(textBox1.Text, out int parsedValue))
            {
                MessageBox.Show("Cohort Number must be an integer.");
                textBox1.Text = "";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                if (textBox1.Text == "")
                {
                    MessageBox.Show("you must enter a cohort number");
                    e.Cancel = true;
                }
                if (selectRig.SelectedIndex == -1)
                {
                    MessageBox.Show("you must choose a rig");
                    e.Cancel = true;
                }
            }
        }
    }
}
