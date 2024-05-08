using GShark.Geometry;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;

namespace RevitScript.Commands;

[Transaction(TransactionMode.Manual)]
public class GSharkCmd : ExternalCommand
{
    public override void Execute()
    {
        var points = new List<Point3> { new(0, 3, 0), new(10, 20, 0), new(10, 10, 50), new(1, 10, 0) };
        var verts = new List<Point3>();
        var tris = new List<int>();
        var normals = new List<Vector3>();
        
        var convexHull = new ConvexHull();
        convexHull.GenerateHull(points, true, ref verts, ref tris, ref normals);
        Debug.WriteLine(convexHull);
    }
}