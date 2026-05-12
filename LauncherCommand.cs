using System;
using System.Collections.Generic;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace PointDynamic
{
    [Transaction(TransactionMode.Manual)]
    public class LauncherCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var win = new PointDynamicWindow();
            new WindowInteropHelper(win).Owner = commandData.Application.MainWindowHandle;

            if (win.ShowDialog() != true || win.SelectedAction == ToolAction.None)
                return Result.Cancelled;

            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            try
            {
                return win.SelectedAction == ToolAction.IsolateRegion
                    ? IsolateRegion(uidoc, doc)
                    : ClearFilter(uidoc, doc);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static Result IsolateRegion(UIDocument uidoc, Document doc)
        {
            Reference pcRef = uidoc.Selection.PickObject(
                ObjectType.Element, "Select a point cloud instance");

            if (doc.GetElement(pcRef) is not PointCloudInstance pcInstance)
            {
                TaskDialog.Show("Error", "Selected element is not a point cloud.");
                return Result.Failed;
            }

            PickedBox box = uidoc.Selection.PickBox(
                PickBoxStyle.Enclosing, "Drag a box around the points to KEEP visible");

            double minX = Math.Min(box.Min.X, box.Max.X);
            double maxX = Math.Max(box.Min.X, box.Max.X);
            double minY = Math.Min(box.Min.Y, box.Max.Y);
            double maxY = Math.Max(box.Min.Y, box.Max.Y);

            BoundingBoxXYZ pcBox = pcInstance.get_BoundingBox(null);
            double minZ = pcBox?.Min.Z - 1.0 ?? -1000.0;
            double maxZ = pcBox?.Max.Z + 1.0 ?? 1000.0;

            var planes = new List<Plane>
            {
                Plane.CreateByNormalAndOrigin( XYZ.BasisX,  new XYZ(minX, 0,    0   )),
                Plane.CreateByNormalAndOrigin(-XYZ.BasisX,  new XYZ(maxX, 0,    0   )),
                Plane.CreateByNormalAndOrigin( XYZ.BasisY,  new XYZ(0,    minY, 0   )),
                Plane.CreateByNormalAndOrigin(-XYZ.BasisY,  new XYZ(0,    maxY, 0   )),
                Plane.CreateByNormalAndOrigin( XYZ.BasisZ,  new XYZ(0,    0,    minZ)),
                Plane.CreateByNormalAndOrigin(-XYZ.BasisZ,  new XYZ(0,    0,    maxZ)),
            };

            PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);

            using var t = new Transaction(doc, "Isolate Point Cloud Region");
            t.Start();
            pcInstance.SetSelectionFilter(filter);
            pcInstance.FilterAction = SelectionFilterAction.Isolate;
            t.Commit();

            uidoc.RefreshActiveView();
            return Result.Succeeded;
        }

        private static Result ClearFilter(UIDocument uidoc, Document doc)
        {
            Reference pcRef = uidoc.Selection.PickObject(
                ObjectType.Element, "Select a point cloud to clear its filter");

            if (doc.GetElement(pcRef) is not PointCloudInstance pcInstance)
            {
                TaskDialog.Show("Error", "Selected element is not a point cloud.");
                return Result.Failed;
            }

            using var t = new Transaction(doc, "Clear Point Cloud Filter");
            t.Start();
            pcInstance.FilterAction = SelectionFilterAction.None;
            t.Commit();

            uidoc.RefreshActiveView();
            return Result.Succeeded;
        }
    }
}
