using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Nice3point.Revit.Toolkit.External;
using System.Diagnostics;
using System.Windows;
using Dimension = Autodesk.Revit.DB.Dimension;

namespace DoEverything.Commands
{
    /// <summary>
    ///     External command entry point invoked from the Revit interface
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class StartupCommand : ExternalCommand
    {
        public override void Execute()
        {
            try
            {
                //var gf = NtsGeometryServices.Instance.CreateGeometryFactory(4326);

                //IList<Reference> select = UiDocument.Selection.PickObjects(ObjectType.Element);


                //IList<Polygon> polygons = [];
                //foreach (Reference r in select)
                //{
                //    FilledRegion filledRegion = Document.GetElement(r) as FilledRegion;
                //    IList<XYZ> points = [];
                //    foreach (CurveLoop r2 in filledRegion.GetBoundaries())
                //    {
                //        foreach (Line item in r2.Cast<Line>())
                //        {
                //            XYZ point = item.GetEndPoint(0);
                //            points.Add(point);
                //        }
                //    }
                //    Polygon polygon = CreatePolygonFromRevitPoints([.. points, points[0]]);
                //    polygons.Add(polygon);
                //}

                //Geometry touches = polygons[0].Intersection(polygons[1]);
                //var res = CalculateDimensions(touches.Length, touches.Area);
                //Debug.WriteLine(res);

                IList<Reference> select = UiDocument.Selection.PickObjects(ObjectType.Element, new DimensionSelectionFilter());
                //using Transaction tran = new(Document, "test");
                //tran.Start();
                //foreach (Reference reference in select)
                //{
                //    Dimension dimension = Document.GetElement(reference) as Dimension;
                //    //ElementType type = EditDim(dimension, GetTextNoteType());
                //    //Document.Delete(type.Id);
                //    CreateLine(dimension);

                //}
                //tran.Commit();

                //var pickedObjReference = UiDocument.Selection.PickObject(ObjectType.Element, new DimensionSelectionFilter());
                //Debug.WriteLine(pickedObjReference.UVPoint);

            }
            catch (Exception)
            {
                return;
            }
        }
        private ElementType EditDim(Dimension dim, TextNoteType textNoteType)
        {
            XYZ dir1;
            XYZ dir2;
            if (IsParallel((dim.Curve as Line).Direction, XYZ.BasisX))
            {
                dir1 = -XYZ.BasisX;
                dir2 = XYZ.BasisY;
            }
            else
            {
                dir1 = -XYZ.BasisY;
                dir2 = -XYZ.BasisX;
            }
            ElementType tempType = textNoteType.Duplicate("TempType");
            DimensionType dimensionType = dim.DimensionType;
            tempType.FindParameter(BuiltInParameter.TEXT_SIZE).Set(dimensionType.FindParameter(BuiltInParameter.TEXT_SIZE).AsDouble());
            tempType.FindParameter(BuiltInParameter.TEXT_FONT).Set(dimensionType.FindParameter(BuiltInParameter.TEXT_FONT).AsString());
            tempType.FindParameter(BuiltInParameter.TEXT_WIDTH_SCALE).Set(dimensionType.FindParameter(BuiltInParameter.TEXT_WIDTH_SCALE).AsDouble());

            double heightText = 0;
            double widthText = 0;
            int num = 0;
            IList<IList<int>> listParent = [];
            IList<int> listChild = [];
            DimensionSegmentArrayIterator dimensionSegmentArrayIterator = dim.Segments.ForwardIterator();
            checked
            {
                while (dimensionSegmentArrayIterator.MoveNext())
                {
                    DimensionSegment dimensionSegment = (DimensionSegment)dimensionSegmentArrayIterator.Current;
                    TextNote textNote = TextNote.Create(Document, ActiveView.Id, new XYZ(0, 0, 0), dimensionSegment.ValueString, tempType.Id);
                    Document.Regenerate();
                    heightText = textNote.Height * ActiveView.Scale;
                    widthText = textNote.Width * ActiveView.Scale;
                    double? value = dimensionSegment.Value;

                    if (widthText > value)
                    {
                        listChild.Add(num);
                    }
                    else
                    {
                        if (listChild.Any())
                        {
                            listParent.Add(listChild);
                            listChild = [];
                        }
                    }
                    num++;
                    if (num == dim.Segments.Size && listChild.Any())
                        listParent.Add(listChild);
                }
                DimensionSegmentArray dimensionSegmentArray = dim.Segments;
                foreach (var _listChild in listParent)
                {
                    XYZ _dir = dir1;
                    int mid = _listChild.Count / 2;
                    for (int i = 0; i < _listChild.Count; i++)
                    {
                        if (i < mid)
                        {
                            DimensionSegment segment = dimensionSegmentArray.get_Item(_listChild[i]);
                            segment.TextPosition = segment.TextPosition.Add(_dir * widthText * 9 / 10)
                                                                        .Add(dir2 * heightText * (i + 1));
                        }
                        else
                        {
                            _dir = -dir1;
                            DimensionSegment segment = dimensionSegmentArray.get_Item(_listChild[i]);
                            segment.TextPosition = segment.TextPosition.Add(_dir * widthText * 9 / 10)
                                                                        .Add(dir2 * heightText * (_listChild.Count - i));
                        }
                    }
                }
            }
            return tempType;
        }
        private TextNoteType GetTextNoteType()
        {
            return new FilteredElementCollector(Document)
                        .OfClass(typeof(TextNoteType))
                        .WhereElementIsElementType()
                        .OfType<TextNoteType>()
                        .FirstOrDefault();
        }
        public bool IsParallel(XYZ p, XYZ q)
        {
            return p.CrossProduct(q).GetLength() < 0.01;
        }
        private Coordinate RevitPointToNTSCoordinate(XYZ point)
        {
            return new Coordinate(point.X.ToMeters(), point.Y.ToMeters());
        }
        private Polygon CreatePolygonFromRevitPoints(List<XYZ> revitPoints)
        {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
            var coordinates = revitPoints.Select(RevitPointToNTSCoordinate).ToList();

            // Đảm bảo polygon được đóng
            if (!coordinates.First().Equals2D(coordinates.Last()))
            {
                coordinates.Add(coordinates.First());
            }

            return geometryFactory.CreatePolygon(coordinates.ToArray());
        }
        public (double length, double width) CalculateDimensions(double perimeter, double area)
        {
            double S = perimeter / 2;
            double discriminant = S * S - 4 * area;

            if (discriminant < 0)
            {
                throw new ArgumentException("Không có nghiệm thực cho các giá trị chu vi và diện tích đã cho.");
            }

            double sqrtDiscriminant = Math.Sqrt(discriminant);
            double length = (S + sqrtDiscriminant) / 2;
            double width = (S - sqrtDiscriminant) / 2;

            return (length, width);
        }
        private void CreateLine(Dimension dimension)
        {
            BoundingBoxXYZ bounding = dimension.get_BoundingBox(ActiveView);
            XYZ max = bounding.Max;
            XYZ min = bounding.Min;

            Document.Create.NewDetailCurve(ActiveView, Line.CreateBound(min, new XYZ(min.X, max.Y, 0)));
            Document.Create.NewDetailCurve(ActiveView, Line.CreateBound(new XYZ(min.X, max.Y, 0), max));
            Document.Create.NewDetailCurve(ActiveView, Line.CreateBound(max, new XYZ(max.X, min.Y, 0)));
            Document.Create.NewDetailCurve(ActiveView, Line.CreateBound(new XYZ(max.X, min.Y, 0), min));
        }
    }

    public class DimensionSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Dimension;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}