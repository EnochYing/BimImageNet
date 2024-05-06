using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Linq;
using System.IO;


namespace BimImageNet
{
    public partial class imageCapture_form : System.Windows.Forms.Form
    {
        public double xgridInterval;     // (f) 
        public double ygridInterval;
        public double zgridInterval;     // (f) 
        public double yawRotate; // degree
        public double pitchRotate;
        public double rollRotate;
        public double overlapRate;

        public int imagWidth;
        public int imagHeight;
        public double HFOV_d;
        public double VFOV_d;  // 38.724 degree

        public double[] distortion; // radial +  tangital 
        public double similarity_threshold;
        public double edge_threshold;

        public string invalidCheckingFunction;
        public bool useSpaces;
        public List<string> selectedSpaceIds = new List<string>();
        public bool run_checkAndDistortImage = false;

        public string[] pred_locations;

        public string output;
        public bool run_captureView = false;

        private Document doc;

        private TreeNode selectedTreeNode = null;


        public imageCapture_form(Document Doc)
        {
            InitializeComponent();
            this.doc = Doc;
            this.xgridInterval = Convert.ToDouble(this.textBox_xGridSize.Text) / 304.8; // transform mm into feet
            this.ygridInterval = Convert.ToDouble(this.textBox_yGridSize.Text) / 304.8; // transform mm into feet
            this.zgridInterval = Convert.ToDouble(this.textBox_zGridSize.Text) / 304.8; // transform mm into feet
            this.yawRotate = Convert.ToDouble(this.textBox_yawRotate.Text);
            this.pitchRotate = Convert.ToDouble(this.textBox_pitchRotate.Text);
            this.rollRotate = Convert.ToDouble(this.textBox_rollRotate.Text);
            this.imagWidth = Convert.ToInt32(this.textBox_Width.Text);
            this.imagHeight = Convert.ToInt32(this.textBox_Height.Text);
            this.HFOV_d = Convert.ToDouble(this.textBox_HFOV.Text);
            this.VFOV_d = Convert.ToDouble(this.textBox_VHOV.Text);
            this.similarity_threshold = double.Parse(textBox_similarity.Text);
            this.edge_threshold = double.Parse(textBox_edgeThreshold.Text);
            this.invalidCheckingFunction = textBox_invalidCheck.Text;
            this.output = this.textBox_outputLocation.Text;

            this.distortion = new double[4];
            this.distortion[0] = double.Parse(textBox_k1.Text);
            this.distortion[1] = double.Parse(textBox_k2.Text);
            this.distortion[2] = double.Parse(textBox_p1.Text);
            this.distortion[3] = double.Parse(textBox_p2.Text);

            // initiaze treeView_spaces
            Dictionary<string, List<string>> level_spaceNameID = getLevelSpaces(Doc);
            initializeSpaceTreeView(level_spaceNameID);

            this.useSpaces = !this.radioButton_pred_location.Checked;
        }

        private void initializeSpaceTreeView(Dictionary<string, List<string>> level_spaces)
        {
            int rootNode = 0;
            foreach(var key in level_spaces.Keys)
            {
                this.treeView_spaces.Nodes.Add(key);
                foreach (var value in level_spaces[key])
                    this.treeView_spaces.Nodes[rootNode].Nodes.Add(value);
                rootNode += 1;
            }
        }

        // levelName_levelID - List<spaceName_spaceID>
        private Dictionary<string, List<string>> getLevelSpaces(Document Doc)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            FilteredElementCollector collector = new FilteredElementCollector(Doc);
            IList<Element> levels = collector.OfClass(typeof(Level)).ToElements();

            foreach (var level in levels)
            {
                ElementLevelFilter levelFilter = new ElementLevelFilter(level.Id);
                FilteredElementCollector collector2 = new FilteredElementCollector(Doc);
                IList<Element> rooms = collector2.WherePasses(new RoomFilter()).WherePasses(levelFilter).ToElements();
                if (rooms.Count != 0)
                {
                    string key = level.Name /*+ "_" + level.Id.ToString()*/;
                    List<string> value = new List<string>();
                    foreach (var room in rooms)
                        value.Add(room.Name + "_" + room.Id.ToString());
                    result.Add(key, value);
                }
            }
            return result;
        }

        private void button_capture_Click(object sender, EventArgs e)
        {
            this.xgridInterval = Convert.ToDouble(this.textBox_xGridSize.Text) / 304.8; // transform mm into feet
            this.ygridInterval = Convert.ToDouble(this.textBox_yGridSize.Text) / 304.8; // transform mm into feet
            this.zgridInterval = Convert.ToDouble(this.textBox_zGridSize.Text) / 304.8; // transform mm into feet
            this.yawRotate = double.Parse(this.textBox_yawRotate.Text);
            this.pitchRotate = Convert.ToDouble(this.textBox_pitchRotate.Text);
            this.rollRotate = Convert.ToDouble(this.textBox_rollRotate.Text);
            this.imagWidth = int.Parse(this.textBox_Width.Text);
            this.imagHeight = int.Parse(this.textBox_Height.Text);
            this.HFOV_d = double.Parse(this.textBox_HFOV.Text);
            // automatically update textBox_VHOV using TextChanged event
            this.VFOV_d = double.Parse(this.textBox_VHOV.Text);
            this.useSpaces = !this.radioButton_pred_location.Checked;

            if (this.useSpaces)
                foreach (var item in this.listBox_selectedSpaces.Items)
                    this.selectedSpaceIds.Add(item.ToString().Split('_').Last());
            else 
            {
                string[] stringSeparators = new string[] { "\r\n" };
                this.pred_locations = this.textBox_pred_locations.Text.Split(stringSeparators, StringSplitOptions.None);
            }

            this.output = textBox_outputLocation.Text;
            this.run_captureView = true;
            this.Close();
        }

        private void button_distortImage_Click(object sender, EventArgs e)
        {
            this.distortion = new double[4];
            this.distortion[0] = double.Parse(textBox_k1.Text);
            this.distortion[1] = double.Parse(textBox_k2.Text);
            this.distortion[2] = double.Parse(textBox_p1.Text);
            this.distortion[3] = double.Parse(textBox_p2.Text);

            this.similarity_threshold = double.Parse(textBox_similarity.Text);
            this.edge_threshold = double.Parse(textBox_edgeThreshold.Text);;
            this.run_checkAndDistortImage = true;
            this.output = textBox_outputLocation.Text;
            this.Close();
        }


        // automatically update tetxBox_VFOV using TextChanged event for all relevant textbox
        private void textBox_HFOV_TextChanged(object sender, EventArgs e)
        {
            int h;
            if (this.textBox_Height.Text == string.Empty)
                h = 0;
            else
                h = int.Parse(this.textBox_Height.Text);

            int w;
            if (this.textBox_Width.Text == string.Empty)
                w = 0;
            else
                w = int.Parse(this.textBox_Width.Text);

            double hfov;
            if (this.textBox_HFOV.Text == string.Empty)
                hfov = 0;
            else
                hfov = double.Parse(textBox_HFOV.Text);

            double vfov;
            if (this.textBox_VHOV.Text == string.Empty)
                vfov = 0;
            else
                vfov = double.Parse(textBox_VHOV.Text);

            this.VFOV_d = 360 * Math.Atan(h * Math.Tan(hfov * Math.PI / 360) / w) / Math.PI;
            this.textBox_VHOV.Text = this.VFOV_d.ToString();
        }

        private void textBox_Width_TextChanged(object sender, EventArgs e)
        {
            int h;
            if (this.textBox_Height.Text == string.Empty)
                h = 0;
            else
                h = int.Parse(this.textBox_Height.Text);

            int w;
            if (this.textBox_Width.Text == string.Empty)
                w = 0;
            else
                w = int.Parse(this.textBox_Width.Text);

            double hfov;
            if (this.textBox_HFOV.Text == string.Empty)
                hfov = 0;
            else
                hfov = double.Parse(textBox_HFOV.Text);

            this.VFOV_d = 360 * Math.Atan(h * Math.Tan(hfov * Math.PI / 360) / w) / Math.PI;
            this.textBox_VHOV.Text = this.VFOV_d.ToString();
        }

        private void textBox_Height_TextChanged(object sender, EventArgs e)
        {
            int h;
            if (this.textBox_Height.Text == string.Empty)
                h = 0;
            else
                h = int.Parse(this.textBox_Height.Text);

            int w;
            if (this.textBox_Width.Text == string.Empty)
                w = 0;
            else
                w = int.Parse(this.textBox_Width.Text);

            double hfov;
            if (this.textBox_HFOV.Text == string.Empty)
                hfov = 0;
            else
                hfov = double.Parse(textBox_HFOV.Text);

            this.VFOV_d = 360 * Math.Atan(h * Math.Tan(hfov * Math.PI / 360) / w) / Math.PI;
            this.textBox_VHOV.Text = this.VFOV_d.ToString();
        }


        private void button_getOutputLocation_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {          
                //Get the path of specified file
                this.output = folderBrowser.SelectedPath;
                textBox_outputLocation.Text = this.output;
            }
        }

        private void button_inputInvalidCheck_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileBrowser = new OpenFileDialog();
            if (fileBrowser.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                this.textBox_invalidCheck.Text = fileBrowser.FileName;
                this.invalidCheckingFunction = fileBrowser.FileName;
            }
        }

        private void button_select_Click(object sender, EventArgs e)
        {
            if (this.selectedTreeNode != null)
            {
                if (!this.listBox_selectedSpaces.Items.Contains(this.selectedTreeNode.Text))
                {
                    this.listBox_selectedSpaces.Items.Add(this.selectedTreeNode.Text);

                    // color the added tree node in the tree view
                    this.selectedTreeNode.BackColor = System.Drawing.Color.LightGreen;
                }
                    
            }        
        }

        private void treeView_spaces_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.selectedTreeNode = treeView_spaces.SelectedNode;

            string spaceID = treeView_spaces.SelectedNode.Text.Split('_').Last();
            ElementId id = ElementId.Parse(spaceID);
            // check whether it is a space
            Element eFromId = doc.GetElement(id);
            if (eFromId.GetType() == typeof(Room))
            {
                UIDocument uiDoc = new UIDocument(eFromId.Document);
                ICollection<ElementId> ids = new List<ElementId>();
                ids.Add(eFromId.Id);
                uiDoc.Selection.SetElementIds(ids);
                uiDoc.ShowElements(ids);
            }    
        }

        private void button_del_Click(object sender, EventArgs e)
        {
            for (int x = listBox_selectedSpaces.SelectedIndices.Count - 1; x >= 0; x--)
            {
                int idx = listBox_selectedSpaces.SelectedIndices[x];

                // uncolor the removed tree nodes in the tree view
                string spaceName = listBox_selectedSpaces.Items[idx].ToString();
                foreach (TreeNode n in treeView_spaces.Nodes)
                {
                   bool done = uncolorRemoveSpace(n, spaceName);
                    if (done)
                        break;
                }
                                         
                // remove the selected item
                listBox_selectedSpaces.Items.RemoveAt(idx);
            }
        }

        private bool uncolorRemoveSpace(TreeNode node, string spaceName)
        {
            if (node.Text == spaceName)
            {
                node.BackColor = System.Drawing.Color.White;
                return true;

            } else
            {
                foreach (TreeNode n in node.Nodes)
                    uncolorRemoveSpace(n, spaceName);
            }
            return false;
        }

        private void textBox_overlapRate_TextChanged(object sender, EventArgs e)
        {
            double hfov;
            if (this.textBox_HFOV.Text == string.Empty)
                hfov = 0;
            else
                hfov = double.Parse(textBox_HFOV.Text);

            double vfov;
            if (this.textBox_VHOV.Text == string.Empty)
                vfov = 0;
            else
                vfov = double.Parse(textBox_VHOV.Text);
        }
    }
}
