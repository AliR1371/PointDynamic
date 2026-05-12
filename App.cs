using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace PointDynamic
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            const string tabName = "PointDynamic";

            try { app.CreateRibbonTab(tabName); }
            catch { /* tab already exists */ }

            RibbonPanel panel = app.CreateRibbonPanel(tabName, "Point Cloud");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            var btnData = new PushButtonData(
                "IsolateRegion",
                "Isolate\nRegion",
                assemblyPath,
                "PointDynamic.LauncherCommand")
            {
                ToolTip = "Open PointDynamic tools — isolate a region or clear a filter.",
                LargeImage = LoadIcon("icon_32.png"),
                Image = LoadIcon("icon_16.png")
            };

            panel.AddItem(btnData);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;

        private static BitmapImage LoadIcon(string filename)
        {
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"PointDynamic.Resources.{filename}");
            if (stream == null) return null;

            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = stream;
            img.EndInit();
            return img;
        }
    }
}
