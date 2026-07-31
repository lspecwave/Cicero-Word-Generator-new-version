using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DataStructures;

namespace WordGenerator.Controls
{
    public partial class SequenceDifferencesForm : Form
    {
        public SequenceDifferencesForm()
        {
            InitializeComponent();
        }

        public SequenceDifferencesForm(List<SequenceComparer.SequenceDifference> differences) : this()
        {
            PopulateReport(differences, null, null);
        }

        public SequenceDifferencesForm(List<SequenceComparer.SequenceDifference> differences, string basePath, string comparedPath) : this()
        {
            PopulateReport(differences, basePath, comparedPath);
        }

        private void PopulateReport(List<SequenceComparer.SequenceDifference> differences, string basePath, string comparedPath)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Difference report");
            report.AppendLine("--------------------");

            if (basePath != null)
                report.AppendLine("Base: " + basePath);
            if (comparedPath != null)
                report.AppendLine("Compared: " + comparedPath);

            if (differences.Count != 0)
            {
                foreach (SequenceComparer.SequenceDifference diff in differences)
                {
                    report.AppendLine();
                    report.AppendLine(diff.Description);
                }
            }
            else
            {
                report.AppendLine("No differences to report!");
            }

            textBox1.Text = report.ToString();
        }
    }
}
