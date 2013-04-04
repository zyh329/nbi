﻿using System;
using System.Linq;
using System.Windows.Forms;
using NBi.UI.Genbi.Interface.Generator.Events;

namespace NBi.UI.Genbi.View.Generator
{
    public partial class OpenTemplateView : Form
    {
        protected CsvGeneratorView Origin { get; set; }
        public string EmbeddedName { get; set; }
        public string FullPath { get; set; }

        public OpenTemplateView(CsvGeneratorView origin)
        {
            Origin = origin;
            InitializeComponent();
            DeclareBindings();
        }

        private void DeclareBindings()
        {
            predefinedTemplateName.DataSource = BindingEmbeddedTemplates;
        }

        private void OpenTemplateView_Load(object sender, EventArgs e)
        {
            TemplateChoice_CheckedChanged(this, EventArgs.Empty);
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            TemplateSelectEventArgs eventArgs = null;
            if (isUserTemplate.Checked)
            {
                eventArgs = new TemplateSelectEventArgs(
                    TemplateSelectEventArgs.TemplateType.External, 
                    userTemplateFullPath.Text);
            }
            else
            {
                eventArgs = new TemplateSelectEventArgs(
                    TemplateSelectEventArgs.TemplateType.Embedded,
                    predefinedTemplateName.SelectedValue.ToString().Replace(" ",""));
            }
            Origin.InvokeTemplateSelect(eventArgs);
            this.Hide();
        }

        private void TemplateChoice_CheckedChanged(object sender, EventArgs e)
        {
            userTemplateFullPath.Enabled = isUserTemplate.Checked;
            openUserTemplate.Enabled = isUserTemplate.Checked;
            predefinedTemplateName.Enabled = isPredefinedTemplate.Checked;
        }

        private void UserTemplateFileSelection_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
                userTemplateFullPath.Text = openFileDialog.FileName;
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

    }
}