using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Reflection;

namespace BimImageNet
{
    public class BimImageNet: IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // create ribbon tab
            string tab = "BimImageNet_instSeg";
            application.CreateRibbonTab(tab);

            // create ribbon panel
            RibbonPanel Panel_BimImageNet = application.CreateRibbonPanel(tab, "BimImageNet_instSeg");

            // create pull-down push buttons
            //PulldownButton imageCapturingAndLabelling = Panel_BimImageNet.AddItem(
            //    new PulldownButtonData("BimImageNet", "BimImageNet")) as PulldownButton;
            //imageCapturingAndLabelling.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Users\enochying\Desktop\App_Revit_rayTracing_v4.1\RevitViewCapture\resources\BimImageNet.png"));

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData pbd_captureImage = new PushButtonData(
                "captureImages", "captureImages", thisAssemblyPath, "BimImageNet.imageCapture");
            PushButton pb_captureImage = Panel_BimImageNet.AddItem(pbd_captureImage) as PushButton;
            pb_captureImage.LongDescription = "Capture BIM Views, output corresponding images, check similar images, and finalize BIM views and images to be lablled";
            pb_captureImage.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"D:\Huaquan\BimImageNet\App_Revit_rayTracing_v4.2_instSeg\RevitViewCapture\resources\captureImages.png"));

            Panel_BimImageNet.AddSeparator();

            PushButtonData pbd_labelImage = new PushButtonData(
                "labelImages", "labelImages", thisAssemblyPath, "BimImageNet.imageSeg");
            PushButton pb_labelImage = Panel_BimImageNet.AddItem(pbd_labelImage) as PushButton;
            pb_labelImage.LongDescription = "Label all finalized BIM images by using a ray-tracing approach on a two-tier BVH tree of BIM scenes";
            pb_labelImage.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"D:\Huaquan\BimImageNet\App_Revit_rayTracing_v4.2_instSeg\RevitViewCapture\resources\labelImages.png"));

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // nothing to clean up in this simple case
            return Result.Succeeded;
        }
    }
}
