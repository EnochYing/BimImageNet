
namespace BimImageNet
{
    partial class imageCapture_form
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_xGridSize = new System.Windows.Forms.TextBox();
            this.button_captureView = new System.Windows.Forms.Button();
            this.textBox_yawRotate = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox_HFOV = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_VHOV = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Width = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Height = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.button_getOutputLocation = new System.Windows.Forms.Button();
            this.textBox_outputLocation = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.textBox_similarity = new System.Windows.Forms.TextBox();
            this.button_inputInvalidCheck = new System.Windows.Forms.Button();
            this.label13 = new System.Windows.Forms.Label();
            this.textBox_invalidCheck = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_zGridSize = new System.Windows.Forms.TextBox();
            this.textBox_pitchRotate = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox_rollRotate = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.button_distortImage = new System.Windows.Forms.Button();
            this.button_select = new System.Windows.Forms.Button();
            this.treeView_spaces = new System.Windows.Forms.TreeView();
            this.listBox_selectedSpaces = new System.Windows.Forms.ListBox();
            this.button_del = new System.Windows.Forms.Button();
            this.textBox_p2 = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textBox_p1 = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.textBox_k2 = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox_k1 = new System.Windows.Forms.TextBox();
            this.cameraParameters = new System.Windows.Forms.GroupBox();
            this.label21 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_yGridSize = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.textBox_edgeThreshold = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBox_pred_locations = new System.Windows.Forms.TextBox();
            this.radioButton_pred_location = new System.Windows.Forms.RadioButton();
            this.cameraParameters.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "xGridSize: ";
            // 
            // textBox_xGridSize
            // 
            this.textBox_xGridSize.Location = new System.Drawing.Point(80, 21);
            this.textBox_xGridSize.Name = "textBox_xGridSize";
            this.textBox_xGridSize.Size = new System.Drawing.Size(60, 20);
            this.textBox_xGridSize.TabIndex = 1;
            this.textBox_xGridSize.Text = "4000";
            // 
            // button_captureView
            // 
            this.button_captureView.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.button_captureView.Location = new System.Drawing.Point(211, 334);
            this.button_captureView.Name = "button_captureView";
            this.button_captureView.Size = new System.Drawing.Size(101, 34);
            this.button_captureView.TabIndex = 2;
            this.button_captureView.Text = "Capture views";
            this.button_captureView.UseVisualStyleBackColor = true;
            this.button_captureView.Click += new System.EventHandler(this.button_capture_Click);
            // 
            // textBox_yawRotate
            // 
            this.textBox_yawRotate.Location = new System.Drawing.Point(80, 93);
            this.textBox_yawRotate.Name = "textBox_yawRotate";
            this.textBox_yawRotate.Size = new System.Drawing.Size(60, 20);
            this.textBox_yawRotate.TabIndex = 6;
            this.textBox_yawRotate.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 96);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Yaw_rotate: ";
            // 
            // textBox_HFOV
            // 
            this.textBox_HFOV.Location = new System.Drawing.Point(54, 28);
            this.textBox_HFOV.Name = "textBox_HFOV";
            this.textBox_HFOV.Size = new System.Drawing.Size(60, 20);
            this.textBox_HFOV.TabIndex = 8;
            this.textBox_HFOV.Text = "70";
            this.textBox_HFOV.TextChanged += new System.EventHandler(this.textBox_HFOV_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 31);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "HFOV: ";
            // 
            // textBox_VHOV
            // 
            this.textBox_VHOV.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_VHOV.Enabled = false;
            this.textBox_VHOV.ForeColor = System.Drawing.Color.Black;
            this.textBox_VHOV.Location = new System.Drawing.Point(54, 54);
            this.textBox_VHOV.Name = "textBox_VHOV";
            this.textBox_VHOV.Size = new System.Drawing.Size(60, 20);
            this.textBox_VHOV.TabIndex = 10;
            this.textBox_VHOV.Text = "70";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(10, 57);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "VFOV: ";
            // 
            // textBox_Width
            // 
            this.textBox_Width.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_Width.ForeColor = System.Drawing.Color.Black;
            this.textBox_Width.Location = new System.Drawing.Point(54, 80);
            this.textBox_Width.Name = "textBox_Width";
            this.textBox_Width.Size = new System.Drawing.Size(60, 20);
            this.textBox_Width.TabIndex = 12;
            this.textBox_Width.Text = "1024";
            this.textBox_Width.TextChanged += new System.EventHandler(this.textBox_Width_TextChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 83);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Width: ";
            // 
            // textBox_Height
            // 
            this.textBox_Height.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_Height.ForeColor = System.Drawing.Color.Black;
            this.textBox_Height.Location = new System.Drawing.Point(54, 106);
            this.textBox_Height.Name = "textBox_Height";
            this.textBox_Height.Size = new System.Drawing.Size(60, 20);
            this.textBox_Height.TabIndex = 14;
            this.textBox_Height.Text = "1024";
            this.textBox_Height.TextChanged += new System.EventHandler(this.textBox_Height_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 109);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(44, 13);
            this.label7.TabIndex = 13;
            this.label7.Text = "Height: ";
            // 
            // button_getOutputLocation
            // 
            this.button_getOutputLocation.Location = new System.Drawing.Point(744, 298);
            this.button_getOutputLocation.Name = "button_getOutputLocation";
            this.button_getOutputLocation.Size = new System.Drawing.Size(65, 25);
            this.button_getOutputLocation.TabIndex = 17;
            this.button_getOutputLocation.Text = "Output...";
            this.button_getOutputLocation.UseVisualStyleBackColor = true;
            this.button_getOutputLocation.Click += new System.EventHandler(this.button_getOutputLocation_Click);
            // 
            // textBox_outputLocation
            // 
            this.textBox_outputLocation.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_outputLocation.ForeColor = System.Drawing.Color.Black;
            this.textBox_outputLocation.Location = new System.Drawing.Point(107, 303);
            this.textBox_outputLocation.Name = "textBox_outputLocation";
            this.textBox_outputLocation.Size = new System.Drawing.Size(631, 20);
            this.textBox_outputLocation.TabIndex = 18;
            this.textBox_outputLocation.Text = "C:\\Users\\enochying\\Desktop\\testBuilding";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(153, 207);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(85, 13);
            this.label12.TabIndex = 29;
            this.label12.Text = "Similarity thresh: ";
            // 
            // textBox_similarity
            // 
            this.textBox_similarity.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_similarity.ForeColor = System.Drawing.Color.Black;
            this.textBox_similarity.Location = new System.Drawing.Point(241, 204);
            this.textBox_similarity.Name = "textBox_similarity";
            this.textBox_similarity.Size = new System.Drawing.Size(47, 20);
            this.textBox_similarity.TabIndex = 30;
            this.textBox_similarity.Text = "0.99";
            // 
            // button_inputInvalidCheck
            // 
            this.button_inputInvalidCheck.Location = new System.Drawing.Point(744, 267);
            this.button_inputInvalidCheck.Name = "button_inputInvalidCheck";
            this.button_inputInvalidCheck.Size = new System.Drawing.Size(65, 25);
            this.button_inputInvalidCheck.TabIndex = 31;
            this.button_inputInvalidCheck.Text = "Input...";
            this.button_inputInvalidCheck.UseVisualStyleBackColor = true;
            this.button_inputInvalidCheck.Click += new System.EventHandler(this.button_inputInvalidCheck_Click);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(10, 273);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(79, 13);
            this.label13.TabIndex = 33;
            this.label13.Text = "Validity check: ";
            // 
            // textBox_invalidCheck
            // 
            this.textBox_invalidCheck.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_invalidCheck.ForeColor = System.Drawing.Color.Black;
            this.textBox_invalidCheck.Location = new System.Drawing.Point(107, 270);
            this.textBox_invalidCheck.Name = "textBox_invalidCheck";
            this.textBox_invalidCheck.Size = new System.Drawing.Size(631, 20);
            this.textBox_invalidCheck.TabIndex = 34;
            this.textBox_invalidCheck.Text = "D:\\Huaquan\\BimImageNet\\App_postProcessing\\dist\\invalidImageChecking.exe";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(10, 306);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(89, 13);
            this.label14.TabIndex = 35;
            this.label14.Text = "Images_building: ";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(20, 71);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(57, 13);
            this.label8.TabIndex = 36;
            this.label8.Text = "zGridSize: ";
            // 
            // textBox_zGridSize
            // 
            this.textBox_zGridSize.Location = new System.Drawing.Point(80, 69);
            this.textBox_zGridSize.Name = "textBox_zGridSize";
            this.textBox_zGridSize.Size = new System.Drawing.Size(60, 20);
            this.textBox_zGridSize.TabIndex = 37;
            this.textBox_zGridSize.Text = "1700";
            // 
            // textBox_pitchRotate
            // 
            this.textBox_pitchRotate.Location = new System.Drawing.Point(80, 117);
            this.textBox_pitchRotate.Name = "textBox_pitchRotate";
            this.textBox_pitchRotate.Size = new System.Drawing.Size(60, 20);
            this.textBox_pitchRotate.TabIndex = 41;
            this.textBox_pitchRotate.Text = "45";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 120);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(70, 13);
            this.label9.TabIndex = 40;
            this.label9.Text = "Pitch_rotate: ";
            // 
            // textBox_rollRotate
            // 
            this.textBox_rollRotate.Location = new System.Drawing.Point(80, 141);
            this.textBox_rollRotate.Name = "textBox_rollRotate";
            this.textBox_rollRotate.Size = new System.Drawing.Size(60, 20);
            this.textBox_rollRotate.TabIndex = 45;
            this.textBox_rollRotate.Text = "0";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(12, 143);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(64, 13);
            this.label11.TabIndex = 44;
            this.label11.Text = "Roll_rotate: ";
            // 
            // button_distortImage
            // 
            this.button_distortImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.button_distortImage.Location = new System.Drawing.Point(351, 334);
            this.button_distortImage.Name = "button_distortImage";
            this.button_distortImage.Size = new System.Drawing.Size(99, 34);
            this.button_distortImage.TabIndex = 48;
            this.button_distortImage.Text = "Create images";
            this.button_distortImage.UseVisualStyleBackColor = true;
            this.button_distortImage.Click += new System.EventHandler(this.button_distortImage_Click);
            // 
            // button_select
            // 
            this.button_select.Location = new System.Drawing.Point(476, 105);
            this.button_select.Margin = new System.Windows.Forms.Padding(2);
            this.button_select.Name = "button_select";
            this.button_select.Size = new System.Drawing.Size(32, 21);
            this.button_select.TabIndex = 24;
            this.button_select.Text = "=>";
            this.button_select.UseVisualStyleBackColor = true;
            this.button_select.Click += new System.EventHandler(this.button_select_Click);
            // 
            // treeView_spaces
            // 
            this.treeView_spaces.BackColor = System.Drawing.SystemColors.Window;
            this.treeView_spaces.Location = new System.Drawing.Point(308, 33);
            this.treeView_spaces.Margin = new System.Windows.Forms.Padding(2);
            this.treeView_spaces.Name = "treeView_spaces";
            this.treeView_spaces.Size = new System.Drawing.Size(163, 223);
            this.treeView_spaces.TabIndex = 26;
            this.treeView_spaces.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_spaces_AfterSelect);
            // 
            // listBox_selectedSpaces
            // 
            this.listBox_selectedSpaces.FormattingEnabled = true;
            this.listBox_selectedSpaces.Location = new System.Drawing.Point(514, 32);
            this.listBox_selectedSpaces.Margin = new System.Windows.Forms.Padding(2);
            this.listBox_selectedSpaces.Name = "listBox_selectedSpaces";
            this.listBox_selectedSpaces.ScrollAlwaysVisible = true;
            this.listBox_selectedSpaces.Size = new System.Drawing.Size(134, 225);
            this.listBox_selectedSpaces.TabIndex = 27;
            // 
            // button_del
            // 
            this.button_del.Location = new System.Drawing.Point(476, 154);
            this.button_del.Margin = new System.Windows.Forms.Padding(2);
            this.button_del.Name = "button_del";
            this.button_del.Size = new System.Drawing.Size(32, 21);
            this.button_del.TabIndex = 28;
            this.button_del.Text = "<=";
            this.button_del.UseVisualStyleBackColor = true;
            this.button_del.Click += new System.EventHandler(this.button_del_Click);
            // 
            // textBox_p2
            // 
            this.textBox_p2.Location = new System.Drawing.Point(54, 212);
            this.textBox_p2.Name = "textBox_p2";
            this.textBox_p2.Size = new System.Drawing.Size(60, 20);
            this.textBox_p2.TabIndex = 56;
            this.textBox_p2.Text = "0";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(10, 214);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(22, 13);
            this.label10.TabIndex = 55;
            this.label10.Text = "p2:";
            // 
            // textBox_p1
            // 
            this.textBox_p1.Location = new System.Drawing.Point(54, 186);
            this.textBox_p1.Name = "textBox_p1";
            this.textBox_p1.Size = new System.Drawing.Size(60, 20);
            this.textBox_p1.TabIndex = 54;
            this.textBox_p1.Text = "0";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(10, 188);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(22, 13);
            this.label15.TabIndex = 53;
            this.label15.Text = "p1:";
            // 
            // textBox_k2
            // 
            this.textBox_k2.Location = new System.Drawing.Point(54, 160);
            this.textBox_k2.Name = "textBox_k2";
            this.textBox_k2.Size = new System.Drawing.Size(60, 20);
            this.textBox_k2.TabIndex = 52;
            this.textBox_k2.Text = "0";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(11, 162);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(22, 13);
            this.label16.TabIndex = 51;
            this.label16.Text = "k2:";
            // 
            // textBox_k1
            // 
            this.textBox_k1.Location = new System.Drawing.Point(54, 133);
            this.textBox_k1.Name = "textBox_k1";
            this.textBox_k1.Size = new System.Drawing.Size(60, 20);
            this.textBox_k1.TabIndex = 50;
            this.textBox_k1.Text = "0";
            // 
            // cameraParameters
            // 
            this.cameraParameters.Controls.Add(this.label21);
            this.cameraParameters.Controls.Add(this.label4);
            this.cameraParameters.Controls.Add(this.textBox_p2);
            this.cameraParameters.Controls.Add(this.textBox_HFOV);
            this.cameraParameters.Controls.Add(this.label10);
            this.cameraParameters.Controls.Add(this.label5);
            this.cameraParameters.Controls.Add(this.textBox_p1);
            this.cameraParameters.Controls.Add(this.textBox_VHOV);
            this.cameraParameters.Controls.Add(this.label15);
            this.cameraParameters.Controls.Add(this.label6);
            this.cameraParameters.Controls.Add(this.textBox_k2);
            this.cameraParameters.Controls.Add(this.textBox_Width);
            this.cameraParameters.Controls.Add(this.label16);
            this.cameraParameters.Controls.Add(this.label7);
            this.cameraParameters.Controls.Add(this.textBox_k1);
            this.cameraParameters.Controls.Add(this.textBox_Height);
            this.cameraParameters.Location = new System.Drawing.Point(12, 10);
            this.cameraParameters.Name = "cameraParameters";
            this.cameraParameters.Size = new System.Drawing.Size(126, 246);
            this.cameraParameters.TabIndex = 57;
            this.cameraParameters.TabStop = false;
            this.cameraParameters.Text = "Camera parameters";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(11, 136);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(22, 13);
            this.label21.TabIndex = 57;
            this.label21.Text = "k1:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.textBox_yGridSize);
            this.groupBox1.Controls.Add(this.textBox_xGridSize);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.textBox_yawRotate);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.textBox_zGridSize);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.textBox_pitchRotate);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Controls.Add(this.textBox_rollRotate);
            this.groupBox1.Location = new System.Drawing.Point(148, 10);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(148, 165);
            this.groupBox1.TabIndex = 58;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "View capture parameters";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 46;
            this.label2.Text = "yGridSize: ";
            // 
            // textBox_yGridSize
            // 
            this.textBox_yGridSize.Location = new System.Drawing.Point(80, 45);
            this.textBox_yGridSize.Name = "textBox_yGridSize";
            this.textBox_yGridSize.Size = new System.Drawing.Size(60, 20);
            this.textBox_yGridSize.TabIndex = 47;
            this.textBox_yGridSize.Text = "3000";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(305, 11);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(106, 13);
            this.label18.TabIndex = 59;
            this.label18.Text = "Spaces in the model:";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(511, 11);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(89, 13);
            this.label19.TabIndex = 60;
            this.label19.Text = "Selected spaces:";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(168, 234);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(70, 13);
            this.label20.TabIndex = 61;
            this.label20.Text = "Edge thresh: ";
            // 
            // textBox_edgeThreshold
            // 
            this.textBox_edgeThreshold.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.textBox_edgeThreshold.ForeColor = System.Drawing.Color.Black;
            this.textBox_edgeThreshold.Location = new System.Drawing.Point(241, 230);
            this.textBox_edgeThreshold.Name = "textBox_edgeThreshold";
            this.textBox_edgeThreshold.Size = new System.Drawing.Size(47, 20);
            this.textBox_edgeThreshold.TabIndex = 62;
            this.textBox_edgeThreshold.Text = "2";
            // 
            // groupBox2
            // 
            this.groupBox2.Location = new System.Drawing.Point(148, 181);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(148, 75);
            this.groupBox2.TabIndex = 63;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Valid check parameters";
            // 
            // textBox_pred_locations
            // 
            this.textBox_pred_locations.Location = new System.Drawing.Point(668, 34);
            this.textBox_pred_locations.Multiline = true;
            this.textBox_pred_locations.Name = "textBox_pred_locations";
            this.textBox_pred_locations.Size = new System.Drawing.Size(143, 222);
            this.textBox_pred_locations.TabIndex = 64;
            // 
            // radioButton_pred_location
            // 
            this.radioButton_pred_location.AutoSize = true;
            this.radioButton_pred_location.Location = new System.Drawing.Point(668, 11);
            this.radioButton_pred_location.Name = "radioButton_pred_location";
            this.radioButton_pred_location.Size = new System.Drawing.Size(114, 17);
            this.radioButton_pred_location.TabIndex = 65;
            this.radioButton_pred_location.TabStop = true;
            this.radioButton_pred_location.Text = "predfined locations";
            this.radioButton_pred_location.UseVisualStyleBackColor = true;
            // 
            // imageCapture_form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(826, 378);
            this.Controls.Add(this.radioButton_pred_location);
            this.Controls.Add(this.textBox_pred_locations);
            this.Controls.Add(this.textBox_edgeThreshold);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button_del);
            this.Controls.Add(this.button_distortImage);
            this.Controls.Add(this.listBox_selectedSpaces);
            this.Controls.Add(this.treeView_spaces);
            this.Controls.Add(this.button_select);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.textBox_invalidCheck);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.button_inputInvalidCheck);
            this.Controls.Add(this.textBox_similarity);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.textBox_outputLocation);
            this.Controls.Add(this.button_getOutputLocation);
            this.Controls.Add(this.button_captureView);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.cameraParameters);
            this.Name = "imageCapture_form";
            this.Text = "imageCapture";
            this.cameraParameters.ResumeLayout(false);
            this.cameraParameters.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_xGridSize;
        private System.Windows.Forms.Button button_captureView;
        private System.Windows.Forms.TextBox textBox_yawRotate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox_HFOV;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_VHOV;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Width;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Height;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button button_getOutputLocation;
        private System.Windows.Forms.TextBox textBox_outputLocation;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textBox_similarity;
        private System.Windows.Forms.Button button_inputInvalidCheck;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox textBox_invalidCheck;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_zGridSize;
        private System.Windows.Forms.TextBox textBox_pitchRotate;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBox_rollRotate;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button button_distortImage;
        private System.Windows.Forms.Button button_select;
        private System.Windows.Forms.TreeView treeView_spaces;
        private System.Windows.Forms.ListBox listBox_selectedSpaces;
        private System.Windows.Forms.Button button_del;
        private System.Windows.Forms.TextBox textBox_p2;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBox_p1;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox textBox_k2;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox_k1;
        private System.Windows.Forms.GroupBox cameraParameters;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox textBox_edgeThreshold;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_yGridSize;
        private System.Windows.Forms.TextBox textBox_pred_locations;
        private System.Windows.Forms.RadioButton radioButton_pred_location;
    }
}