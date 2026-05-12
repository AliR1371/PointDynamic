using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace PointDynamic
{
    [Transaction(TransactionMode.Manual)]
    public class IsolatePointCloudRegionCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            
            try
            {
                // 1. Pick the point cloud instance
                Reference pcRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a point cloud instance");

                PointCloudInstance pcInstance = doc.GetElement(pcRef) as PointCloudInstance;
                if (pcInstance == null)
                {
                    TaskDialog.Show("Error", "Selected element is not a point cloud.");
                    return Result.Failed;
                }

                // 2. Pick a rectangular region on screen
                PickedBox pickedBox = uidoc.Selection.PickBox(
                    PickBoxStyle.Enclosing,
                    "Drag a box around the points to KEEP visible");

                XYZ p1 = pickedBox.Min;
                XYZ p2 = pickedBox.Max;

                // 3. Build axis-aligned planes spanning the full Z extent of the cloud
                double minX = Math.Min(p1.X, p2.X);
                double maxX = Math.Max(p1.X, p2.X);
                double minY = Math.Min(p1.Y, p2.Y);
                double maxY = Math.Max(p1.Y, p2.Y);

                BoundingBoxXYZ pcBox = pcInstance.get_BoundingBox(null);
                double minZ = pcBox != null ? pcBox.Min.Z - 1.0 : -1000.0;
                double maxZ = pcBox != null ? pcBox.Max.Z + 1.0 :  1000.0;

                List<Plane> planes = new List<Plane>
                {
                    Plane.CreateByNormalAndOrigin( XYZ.BasisX,  new XYZ(minX, 0,    0   )),
                    Plane.CreateByNormalAndOrigin(-XYZ.BasisX,  new XYZ(maxX, 0,    0   )),
                    Plane.CreateByNormalAndOrigin( XYZ.BasisY,  new XYZ(0,    minY, 0   )),
                    Plane.CreateByNormalAndOrigin(-XYZ.BasisY,  new XYZ(0,    maxY, 0   )),
                    Plane.CreateByNormalAndOrigin( XYZ.BasisZ,  new XYZ(0,    0,    minZ)),
                    Plane.CreateByNormalAndOrigin(-XYZ.BasisZ,  new XYZ(0,    0,    maxZ)),
                };

                PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(planes);

                // 4. Apply filter — points outside the box are hidden
                using (Transaction t = new Transaction(doc, "Isolate Point Cloud Region"))
                {
                    t.Start();
                    pcInstance.SetSelectionFilter(filter);
                    pcInstance.FilterAction = SelectionFilterAction.Isolate;
                    t.Commit();
                }

                uidoc.RefreshActiveView();

                TaskDialog.Show("Done",
                    "Points outside the selected region are now hidden.\n\n" +
                    "To restore: set FilterAction = None, or remove the filter.");

                return Result.Succeeded;
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
    }
}
