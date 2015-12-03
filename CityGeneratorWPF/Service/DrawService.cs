using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CityGenerator;
using CityGeneratorWPF.Extensions;
using Helpers;
using Voronoi;
using Line = System.Windows.Shapes.Line;
using Point = Voronoi.Point;
using Rectangle = Voronoi.Rectangle;

namespace CityGeneratorWPF.Service
{
    /// <summary>
    /// Handles Drawing to a canvas
    /// </summary>
    public class DrawService
    {
        /// <summary>
        /// Event that is called when the user clicks on the canvas
        /// </summary>
        public delegate void ClickOnCanvas(Point p);
        public ClickOnCanvas OnClick;

        /// <summary>
        /// The canvas used to draw
        /// </summary>
        private readonly Canvas _drawCanvas;

        public Canvas Canvas => _drawCanvas;

        /// <summary>
        /// Create a new DrawService and hook up the events
        /// </summary>
        public DrawService(Canvas drawCanvas)
        {
            _drawCanvas = drawCanvas;
            _drawCanvas.MouseDown += OnCanvasMouseDown;
        }

        /// <summary>
        /// On mouse Down event
        /// </summary>
        private void OnCanvasMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //get point
            var p = e.GetPosition(_drawCanvas);

            if (OnClick != null)
                OnClick(new Point(p.X,p.Y));
        }

        /// <summary>
        /// Clear the entire canvas
        /// </summary>
        public void ClearCanvas()
        {
            _drawCanvas.Children.Clear();
        }

        /// <summary>
        /// Add a point to the canvas
        /// </summary>
        public void DrawPoint(Point p, double radius, Color c)
        {
            //Create the point
            var point = new Ellipse
            {
                Fill = new SolidColorBrush(c),
                Width = radius,
                Height = radius,
            };

            //position it on the canvas
            Canvas.SetLeft(point, p.X - (radius / 2));
            Canvas.SetTop(point, p.Y - (radius / 2));

            //draw it
            _drawCanvas.Children.Add(point);
        }

        /// <summary>
        /// Draw a line from point 1 to point 2
        /// </summary>
        public void DrawLine(Point p1, Point p2, Color c,int thick = 1)
        {
            var thickness = new Thickness(0, 0, 0, 0);
            var line = new Line
            {
                Margin = thickness,
                Visibility = Visibility.Visible,
                StrokeThickness = thick,
                Stroke = new SolidColorBrush(c),
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y
            };

            _drawCanvas.Children.Add(line);
        }

        public void DrawLine(Voronoi.Line line, Color c,int thick = 1)
        {
            DrawLine(line.Start,line.End,c,thick);
        }

        /// <summary>
        /// Draw Rectangle from a point and width and height
        /// </summary>
        public void DrawRectangle(Point p,int width, int height,Color c)
        {
            //Create 3 other points
            var pWidth =       new Point(p.X + width, p.Y);
            var pHeight =      new Point(p.X, p.Y + height);
            var pWidthHeight = new Point(p.X + width, p.Y + height);

            //Draw
            DrawLine(p,pWidth,c);
            DrawLine(p, pHeight, c);
            DrawLine(pHeight, pWidthHeight, c);
            DrawLine(pWidth, pWidthHeight, c);
            
        }

        public void DrawTriangle(Triangle t, Color c)
        {
            DrawTriangle(t.Point1, t.Point2, t.Point3, c);
        }

        public void DrawTriangle(Point p1, Point p2, Point p3, Color c)
        {
            DrawLine(p1, p2, c);
            DrawLine(p3, p2, c);
            DrawLine(p1, p3, c);

        }

        public void DrawCircle(Circle cr, Color c)
        {
            DrawCircle(cr.Center,cr.Radius,c);
        }

        public void DrawCircle(Point p, double radius, Color c)
        {
            //Create the point
            var point = new Ellipse
            {
                //Fill = new SolidColorBrush(c),
                Stroke = new SolidColorBrush(c),
                StrokeThickness = 1,
                Width = radius,
                Height = radius,
            };

            //position it on the canvas
            Canvas.SetLeft(point, p.X - (radius / 2));
            Canvas.SetTop(point, p.Y - (radius / 2));

            //draw it
            _drawCanvas.Children.Add(point);
        }

        public void DrawRectangle(Rectangle rect, Color c)
        {
            var p1 = new Point(rect.Left, rect.Top);
            var p2 = new Point(rect.Left, rect.Bottom);
            var p3 = new Point(rect.Right, rect.Bottom);
            var p4 = new Point(rect.Right, rect.Top);


            DrawLine(p1, p2, c);
            DrawLine(p2, p3, c);
            DrawLine(p3, p4, c);
            DrawLine(p4, p1, c);
        }

        public void DrawPolygon(IList<Point> points, Color c)
        {
            var polygon = new Polygon()
            {
                Stroke = new SolidColorBrush(c),
                Fill = new SolidColorBrush(c),
                
            };

            var pc = new PointCollection();
            foreach (var point in points)
                pc.Add(new System.Windows.Point(point.X,point.Y));

            polygon.Points = pc;

            _drawCanvas.Children.Add(polygon);
        }

        public void DrawPolygon(Triangle t, Color c)
        {
            var plist = new List<Point>()
            {
                t.Point1,
                t.Point2,
                t.Point3
            };

            DrawPolygon(plist,c);
        }

        public void DrawCell(Cell cell, Color c, bool bFill = true,bool bBorder = false)
        {
            if (bFill)
            {
                //Create polygon
                var polygon = new Polygon()
                {
                    Stroke = new SolidColorBrush(Colors.Red),
                    Fill = new SolidColorBrush(c),
                    FillRule = FillRule.EvenOdd,
                    StrokeThickness = 1
                };

                polygon.Stroke = (bBorder) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(c);

                //Create a point list for the polygon
                var pc = new PointCollection();
                foreach (var point in cell.Points)
                {
                    pc.Add(new System.Windows.Point(point.X, point.Y));
                }

                polygon.Points = pc;


                //Draw
                _drawCanvas.Children.Add(polygon);
            }
            else
            {
                foreach (var edge in cell.Edges)
                {
                    DrawLine(edge,c);
                }
            }

            //Info about the Cell
            //var areaOfCell = cell.Area();
            //var centerOfcell = cell.Center();
            //DrawText($"{areaOfCell}",Colors.Black, centerOfcell);

        }

        public void DrawText(string text, Color c, Point position)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(c)
            };

            Canvas.SetLeft(textBlock, position.X);
            Canvas.SetTop(textBlock, position.Y);

            _drawCanvas.Children.Add(textBlock);
        }

        public void DrawDistrict(District district, Color c, bool bDrawRoads, bool bDrawCells)
        {
            //Fill the cells 
            foreach (var cell in district.Cells)
            {
                if(bDrawCells)
                     DrawCell(cell.Cell, c.GetRandomColorOffset(0.07));
                else
                {
                    var cellColor = Color.FromRgb(200, 200, 200);
                    //DrawCell(cell.Cell, cellColor);
                }

                if (bDrawRoads && cell.Roads != null)
                {
                    var roadColor = Color.FromRgb(75,75,75);
                    

                    foreach (var road in cell.Roads)
                    {
                        //roadColor = Extensions.Extensions.RandomColor(false,225);
                        DrawRoad(road, roadColor, Colors.Red, Colors.Aqua, false,1);
                    }
                }

              

            };
        }

        public void DrawRoad(Road road, Color linecolor, Color startColor, Color endColor, bool drawStartEnd = true, int width = 1)
        {
                //width = line.Intersected ? width + 1 : width;
                DrawLine(road.RoadLine,linecolor, width);

            //DrawPoint(road.RoadLine.Start,10,Colors.Red);
            //DrawPoint(road.RoadLine.End, 5, Colors.CornflowerBlue);


            var pc = Color.FromRgb(50,50,50);
            foreach (var p in road.BuildSites)
            {
                DrawPoint(p, 5, pc);
            }
         }
    }
}
