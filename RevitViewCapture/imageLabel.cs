using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BimImageNet
{
    public partial class imageSegmentation : System.Windows.Forms.Form
    {
        public bool useMiddlePoint;
        public string output;
        //public string imageList;
        public string postprocessingFunction;
        public bool run = false;
        public double[] distortion;

        public List<string> panopticSeg_semanticList = new List<string>();

        public imageSegmentation()
        {
            InitializeComponent();
            this.distortion = new double[4];
            this.distortion[0] = double.Parse(textBox_k1.Text);
            this.distortion[1] = double.Parse(textBox_k2.Text);
            this.distortion[2] = double.Parse(textBox_p1.Text);
            this.distortion[3] = double.Parse(textBox_p2.Text);

            this.useMiddlePoint = this.radioButton_middlepoint.Checked;
            this.output = this.textBox_outputPath.Text;
            //this.imageList =  this.textBox_imageList.Text;
            this.postprocessingFunction = this.textBox_postprocessing.Text;

            for (int x = 0; x < this.checkedListBox_panopticSeg_semanticList.CheckedItems.Count; x++)
            {
                panopticSeg_semanticList.Add(this.checkedListBox_panopticSeg_semanticList.CheckedItems[x].ToString());
            }
        }

        //private void button_inputImageList_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialog fileBrowser = new OpenFileDialog();
        //    if (fileBrowser.ShowDialog() == DialogResult.OK)
        //    {
        //        //Get the path of specified file
        //        this.textBox_imageList.Text = fileBrowser.FileName;
        //        this.imageList = fileBrowser.FileName;
        //    }
        //}

        private void button_segment_Click(object sender, EventArgs e)
        {
            if (radioButton_middlepoint.Checked)
                this.useMiddlePoint = true;
            this.output = textBox_outputPath.Text;
            this.distortion = new double[4];
            this.distortion[0] = double.Parse(textBox_k1.Text);
            this.distortion[1] = double.Parse(textBox_k2.Text);
            this.distortion[2] = double.Parse(textBox_p1.Text);
            this.distortion[3] = double.Parse(textBox_p2.Text);

            for (int x = 0; x < this.checkedListBox_panopticSeg_semanticList.CheckedItems.Count; x++)
            {
                panopticSeg_semanticList.Add(this.checkedListBox_panopticSeg_semanticList.CheckedItems[x].ToString());
            }
            this.run = true;
            this.Close();
        }

        private void button_output_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                this.output = folderBrowser.SelectedPath;
                textBox_outputPath.Text = this.output;
            }
        }

        private void button_inputPostprocessingFunction_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileBrowser = new OpenFileDialog();
            if (fileBrowser.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                this.textBox_postprocessing.Text = fileBrowser.FileName;
                this.postprocessingFunction = fileBrowser.FileName;
            }
        }
    }
}
