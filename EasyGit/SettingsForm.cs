using System;
using System.Windows.Forms;

namespace EasyGit
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }


        private void SettingsForm_Load(object sender, EventArgs e)
        {
            checkBoxOpenFolder.Checked = Properties.Settings.Default.OpenFolderAfterDownload;
        }


        private void btnSave_Click(object sender, EventArgs e)
        {

            Properties.Settings.Default.OpenFolderAfterDownload = checkBoxOpenFolder.Checked;
            Properties.Settings.Default.Save(); 

            this.DialogResult = DialogResult.OK; 
            this.Close(); 
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Pashamin/EasyGit");
        }
    }
}