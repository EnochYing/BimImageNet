
namespace BimImageNet
{
    partial class imageSegmentation
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_outputPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButton_SAH = new System.Windows.Forms.RadioButton();
            this.radioButton_middlepoint = new System.Windows.Forms.RadioButton();
            this.button_segment = new System.Windows.Forms.Button();
            this.button_output = new System.Windows.Forms.Button();
            this.button_inputPostprocessingFunction = new System.Windows.Forms.Button();
            this.textBox_postprocessing = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.checkedListBox_panopticSeg_semanticList = new System.Windows.Forms.CheckedListBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox_p2 = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textBox_p1 = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.textBox_k2 = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_k1 = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox_outputPath
            // 
            this.textBox_outputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_outputPath.Location = new System.Drawing.Point(83, 44);
            this.textBox_outputPath.Name = "textBox_outputPath";
            this.textBox_outputPath.Size = new System.Drawing.Size(299, 20);
            this.textBox_outputPath.TabIndex = 4;
            this.textBox_outputPath.Text = "C:\\Users\\enochying\\Desktop\\testBuilding";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Labels_build:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton_SAH);
            this.groupBox1.Controls.Add(this.radioButton_middlepoint);
            this.groupBox1.Location = new System.Drawing.Point(11, 177);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(192, 57);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "BVH split strategy";
            // 
            // radioButton_SAH
            // 
            this.radioButton_SAH.AutoSize = true;
            this.radioButton_SAH.Location = new System.Drawing.Point(121, 25);
            this.radioButton_SAH.Name = "radioButton_SAH";
            this.radioButton_SAH.Size = new System.Drawing.Size(47, 17);
            this.radioButton_SAH.TabIndex = 19;
            this.radioButton_SAH.Text = "SAH";
            this.radioButton_SAH.UseVisualStyleBackColor = true;
            // 
            // radioButton_middlepoint
            // 
            this.radioButton_middlepoint.AutoSize = true;
            this.radioButton_middlepoint.Checked = true;
            this.radioButton_middlepoint.Location = new System.Drawing.Point(10, 25);
            this.radioButton_middlepoint.Name = "radioButton_middlepoint";
            this.radioButton_middlepoint.Size = new System.Drawing.Size(79, 17);
            this.radioButton_middlepoint.TabIndex = 19;
            this.radioButton_middlepoint.TabStop = true;
            this.radioButton_middlepoint.Text = "Middlepoint";
            this.radioButton_middlepoint.UseVisualStyleBackColor = true;
            // 
            // button_segment
            // 
            this.button_segment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_segment.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.button_segment.Location = new System.Drawing.Point(76, 240);
            this.button_segment.Name = "button_segment";
            this.button_segment.Size = new System.Drawing.Size(60, 29);
            this.button_segment.TabIndex = 19;
            this.button_segment.Text = "Label";
            this.button_segment.UseVisualStyleBackColor = true;
            this.button_segment.Click += new System.EventHandler(this.button_segment_Click);
            // 
            // button_output
            // 
            this.button_output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_output.Location = new System.Drawing.Point(388, 42);
            this.button_output.Name = "button_output";
            this.button_output.Size = new System.Drawing.Size(61, 23);
            this.button_output.TabIndex = 20;
            this.button_output.Text = "Output...";
            this.button_output.UseVisualStyleBackColor = true;
            this.button_output.Click += new System.EventHandler(this.button_output_Click);
            // 
            // button_inputPostprocessingFunction
            // 
            this.button_inputPostprocessingFunction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_inputPostprocessingFunction.Location = new System.Drawing.Point(388, 13);
            this.button_inputPostprocessingFunction.Name = "button_inputPostprocessingFunction";
            this.button_inputPostprocessingFunction.Size = new System.Drawing.Size(61, 23);
            this.button_inputPostprocessingFunction.TabIndex = 23;
            this.button_inputPostprocessingFunction.Text = "Input...";
            this.button_inputPostprocessingFunction.UseVisualStyleBackColor = true;
            this.button_inputPostprocessingFunction.Click += new System.EventHandler(this.button_inputPostprocessingFunction_Click);
            // 
            // textBox_postprocessing
            // 
            this.textBox_postprocessing.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_postprocessing.Location = new System.Drawing.Point(83, 15);
            this.textBox_postprocessing.Name = "textBox_postprocessing";
            this.textBox_postprocessing.Size = new System.Drawing.Size(299, 20);
            this.textBox_postprocessing.TabIndex = 22;
            this.textBox_postprocessing.Text = "C:\\Users\\enochying\\Desktop\\App_postProcessing\\dist\\postprocessing\\postprocessing_" +
    "instSeg.exe";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 18);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(68, 13);
            this.label9.TabIndex = 21;
            this.label9.Text = "Postprocess:";
            // 
            // checkedListBox_panopticSeg_semanticList
            // 
            this.checkedListBox_panopticSeg_semanticList.AllowDrop = true;
            this.checkedListBox_panopticSeg_semanticList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBox_panopticSeg_semanticList.FormattingEnabled = true;
            this.checkedListBox_panopticSeg_semanticList.Items.AddRange(new object[] {
            "Wall",
            "Curtain Wall",
            "Floor",
            "Roof",
            "Door",
            "Window",
            "Column",
            "Beam",
            "Chair",
            "Desk",
            "Pipe"});
            this.checkedListBox_panopticSeg_semanticList.Location = new System.Drawing.Point(233, 112);
            this.checkedListBox_panopticSeg_semanticList.Name = "checkedListBox_panopticSeg_semanticList";
            this.checkedListBox_panopticSeg_semanticList.Size = new System.Drawing.Size(218, 154);
            this.checkedListBox_panopticSeg_semanticList.TabIndex = 24;
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Location = new System.Drawing.Point(233, 77);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(215, 35);
            this.textBox1.TabIndex = 26;
            this.textBox1.Text = "Choose object types for semantic labelling for panotic sgementation:";
            // 
            // textBox_p2
            // 
            this.textBox_p2.Location = new System.Drawing.Point(130, 58);
            this.textBox_p2.Name = "textBox_p2";
            this.textBox_p2.Size = new System.Drawing.Size(53, 20);
            this.textBox_p2.TabIndex = 64;
            this.textBox_p2.Text = "0";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(103, 61);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(22, 13);
            this.label10.TabIndex = 63;
            this.label10.Text = "p2:";
            // 
            // textBox_p1
            // 
            this.textBox_p1.Location = new System.Drawing.Point(35, 58);
            this.textBox_p1.Name = "textBox_p1";
            this.textBox_p1.Size = new System.Drawing.Size(53, 20);
            this.textBox_p1.TabIndex = 62;
            this.textBox_p1.Text = "0";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(7, 61);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(22, 13);
            this.label15.TabIndex = 61;
            this.label15.Text = "p1:";
            // 
            // textBox_k2
            // 
            this.textBox_k2.Location = new System.Drawing.Point(130, 27);
            this.textBox_k2.Name = "textBox_k2";
            this.textBox_k2.Size = new System.Drawing.Size(53, 20);
            this.textBox_k2.TabIndex = 60;
            this.textBox_k2.Text = "0";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(103, 30);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(22, 13);
            this.label16.TabIndex = 59;
            this.label16.Text = "k2:";
            // 
            // textBox_k1
            // 
            this.textBox_k1.Location = new System.Drawing.Point(35, 27);
            this.textBox_k1.Name = "textBox_k1";
            this.textBox_k1.Size = new System.Drawing.Size(53, 20);
            this.textBox_k1.TabIndex = 58;
            this.textBox_k1.Text = "0";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(7, 29);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(22, 13);
            this.label17.TabIndex = 57;
            this.label17.Text = "k1:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBox_p2);
            this.groupBox2.Controls.Add(this.textBox_k2);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.label17);
            this.groupBox2.Controls.Add(this.textBox_p1);
            this.groupBox2.Controls.Add(this.textBox_k1);
            this.groupBox2.Controls.Add(this.label15);
            this.groupBox2.Controls.Add(this.label16);
            this.groupBox2.Location = new System.Drawing.Point(12, 77);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(191, 91);
            this.groupBox2.TabIndex = 65;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Distortion coefficients";
            // 
            // imageSegmentation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(463, 277);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.checkedListBox_panopticSeg_semanticList);
            this.Controls.Add(this.button_inputPostprocessingFunction);
            this.Controls.Add(this.textBox_postprocessing);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.button_output);
            this.Controls.Add(this.button_segment);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox_outputPath);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox2);
            this.Name = "imageSegmentation";
            this.Text = "imageLabel";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textBox_outputPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButton_SAH;
        private System.Windows.Forms.RadioButton radioButton_middlepoint;
        private System.Windows.Forms.Button button_segment;
        private System.Windows.Forms.Button button_output;
        private System.Windows.Forms.Button button_inputPostprocessingFunction;
        private System.Windows.Forms.TextBox textBox_postprocessing;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckedListBox checkedListBox_panopticSeg_semanticList;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox_p2;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBox_p1;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox textBox_k2;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_k1;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}