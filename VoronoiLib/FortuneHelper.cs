using System;
using System.Collections.Generic;
using System.Linq;

namespace Voronoi
{

    internal class VoronoiEdge
    {
        public Point Start;
        public Point End;
        public Point Direction;
        public Point Left;
        public Point Right;

        public double F;
        public double G;

        public VoronoiEdge Neighbour;

        public VoronoiEdge(Point s, Point a, Point b)
        {
            Start = s;
            Left = a;
            Right = b;
            Neighbour = null;
            End = null;

            F = (b.X - a.X) / (a.Y - b.Y);
            G = s.Y - (F*s.X);
            Direction = new Point(b.Y - a.Y, - b.X - a.X);
        }
    }

    /// <summary>
    /// a class that stores information about an item in a beachline sequence.
    /// it can represent an parabola or an intersection between 2 archs ( an edge)
    /// this implementation uses parabolas to create a binary tree
    /// </summary>
    internal class VoronoiParabola
    {
        public bool IsLeaf; //flag whether the node is Leaf or Internal node
        public Point FocusPoint; //focus point of the parabola when it is a parabola
        public VoronoiEdge Edge; //the edge when it is an edge
        public VoronoiEvent CircleEvent; //the event when the parabola dissapears(Circle event)
        
        //binary tree stuff
        #region Binary Tree Operations
       
        public VoronoiParabola Parent; //parent node in the binary tree

        private VoronoiParabola _left = null;
        public VoronoiParabola Left
        {
            get { return _left; }
            set
            {
                _left = value;
                _left.Parent = this;
            }
        }

        private VoronoiParabola _right = null;
        public VoronoiParabola Right
        {
            get { return _right; }
            set
            {
                _right = value;
                _right.Parent = this;
            }
        }

        public static VoronoiParabola GetLeft(VoronoiParabola p)
        {
            return GetLeftChild(GetLeftParent(p));
        }

        public static VoronoiParabola GetRight(VoronoiParabola p)
        {
            return GetRightChild(GetRightParent(p));
        }

        public static VoronoiParabola GetLeftParent(VoronoiParabola p)
        {
            var parent = p.Parent;
            var last = p;
            while (p.Left == last)
            {
                if(parent.Parent == null) return null;
                last = parent;
                parent = parent.Parent;
            }

            return parent;
        }

        public static VoronoiParabola GetRightParent(VoronoiParabola p)
        {
            var parent = p.Parent;
            var last = p;
            while (p.Right == last)
            {
                if (parent.Parent == null) return null;
                last = parent;
                parent = parent.Parent;
            }

            return parent;
        }

        public static VoronoiParabola GetLeftChild(VoronoiParabola p)
        {
            if (p == null) return null;
            var par = p.Left;
            while (par.IsLeaf == false)
                par = par.Right;

            return par;
        }
        
        public static VoronoiParabola GetRightChild(VoronoiParabola p)
        {
            if (p == null) return null;
            var par = p.Left;
            while (par.IsLeaf == false)
                par = par.Left;

            return par;
        }

        #endregion

        //Constructors
        //when is is an edge
        public VoronoiParabola()
        {
            FocusPoint = null;
            IsLeaf = false;
            Edge = null;
            Parent = null;
            CircleEvent = null;
        }

        //when it is a parabola
        public VoronoiParabola(Point p)
        {
            FocusPoint = p;
            IsLeaf = true;
            Edge = null;
            Parent = null;
            CircleEvent = null;
        }      
    }

    /// <summary>
    /// the class for storing Place / Circle Events in the event queue
    /// </summary>
    internal class VoronoiEvent
    {
        public Point Point; //the point for wich the current event occurs ( top circle point for circle event, focus point for place event)
        public bool PlaceEvent; //flag to see if it is a place event or a circle event
        public double Y; //y coordinate of the point, events will be sorted by this y
        public VoronoiParabola Arch; // if it is a place Event, it is an arch above which the event occurs

        public VoronoiEvent(Point point, bool bPlaceEvent)
        {
            Point = point;
            PlaceEvent = bPlaceEvent;
            Y = Point.Y;
            Arch = null;
        }

        public static bool operator >(VoronoiEvent e1, VoronoiEvent e2)
        {
            return (e1.Y > e2.Y);
        }

        public static bool operator <(VoronoiEvent e1, VoronoiEvent e2)
        {
            return (e1.Y < e2.Y);
        }
    }

    /// <summary>
    /// Helper class for generating a Voronoi Diagram using the Fortune Algoritm
    /// </summary>
    internal class FortuneHelper
    {
        private int _width;
        private int _height;
        private VoronoiParabola _root = null; //root of the tree of parabolas
        private double _currentY; //current y positon of the line

        private List<Point> _points; 
        private List<VoronoiEvent> _sortedEvents; //Events sorted by Y position
        private List<VoronoiEvent> _deletedEvents; //deleted events
        private List<VoronoiEdge> _edges; //deleted events

        private VoronoiDiagram _voronoi = new VoronoiDiagram();


        public FortuneHelper(int width, int height)
        {
            _sortedEvents = new List<VoronoiEvent>();
            _deletedEvents = new List<VoronoiEvent>();
            _edges = new List<VoronoiEdge>();
            _currentY = 0.0;
            _root = null;
            _width = width;
            _height = height;
        }


        public VoronoiDiagram GetVoronoi(List<Point> points)
        {
            _points = points;

            //1. Create Place Events for every point
            foreach (var point in _points)
                InsertEvent(new VoronoiEvent(point,true));

            foreach (var e in _sortedEvents)
            {
                _currentY = e.Y;

                //look if event has been deleted
                if (_deletedEvents.Contains(e))
                {
                    _deletedEvents.Remove(e);
                    continue;
                }

                if (e.PlaceEvent) //place event
                    AddParabola(e.Point);
                else //circle event
                    RemoveParabola(e);
            }

            FinishEdge(_root); 

            foreach (var edge in _edges.Where(edge => edge.Direction != null))
            {
                edge.Start = edge.Neighbour.End;
            }

            foreach (var edge in _edges)
            {
                _voronoi.Lines.Add(new Line(edge.Start,edge.End));
            }

            return _voronoi;
        }

        private void FinishEdge(VoronoiParabola p)
        {
            if (p.IsLeaf)
                return;

            var mx = 0.0;
            if (p.Edge.Direction.X > 0.0)
                mx = Math.Max(_width, p.Edge.Start.X + 10);
            else
                mx = Math.Min(0, p.Edge.Start.X - 10);

            var end = new Point(mx, mx * p.Edge.F + p.Edge.G);
            p.Edge.End = end;
            _points.Add(end);

            FinishEdge(p.Left);
            FinishEdge(p.Right);
        }

        private void InsertEvent(VoronoiEvent e)
        {
            var index = _sortedEvents.Count(voronoiEvent => !(e.Y < voronoiEvent.Y));
            _sortedEvents.Insert(index, e);
        }

        private void AddParabola(Point point)
        {
            if (_root == null)
            {
                _root = new VoronoiParabola(point);
                return;
            }

            if (_root.IsLeaf && (_root.FocusPoint.Y - point.Y) < 1)
            {
                var fp = _root.FocusPoint;
                _root.IsLeaf = false;
                _root.Left = new VoronoiParabola(fp);
                _root.Right = new VoronoiParabola(point);

                var s = new Point((point.X + fp.X)/2,_height);
                _points.Add(s);

                _root.Edge = point.X > fp.X ? new VoronoiEdge(s,fp,point) : new VoronoiEdge(s,point,fp);

                _edges.Add(_root.Edge);
                return;
            }

            var par = GetParabolaByX(point.X);
            if (par.CircleEvent != null)
            {
                _deletedEvents.Add(par.CircleEvent);
            }

            var start = new Point(point.X,GetY(par.FocusPoint,point.X));
            _points.Add(start);

            var edgeLeft = new VoronoiEdge(start,par.FocusPoint,point);
            var edgeRight = new VoronoiEdge(start, point,par.FocusPoint);
            _edges.Add(edgeLeft);

            par.Edge = edgeRight;
            par.IsLeaf = false;

            var p0 = new VoronoiParabola(par.FocusPoint);
            var p1 = new VoronoiParabola(point);
            var p2 = new VoronoiParabola(par.FocusPoint);

            par.Right = p2;
            par.Left = new VoronoiParabola();
            par.Left.Edge = edgeLeft;
            par.Left.Left = p0;
            par.Left.Right = p1;

            CheckCircle(p0);
            CheckCircle(p2);


        }

        private void RemoveParabola(VoronoiEvent e)
        {
            var p1 = e.Arch;

            var xl = VoronoiParabola.GetLeftParent(p1);
            var xr = VoronoiParabola.GetRightParent(p1);

            var p0 = VoronoiParabola.GetLeftChild(xl);
            var p2 = VoronoiParabola.GetRightChild(xr);

            if(p0.CircleEvent != null)
                _deletedEvents.Add(p0.CircleEvent);

            if (p2.CircleEvent != null)
                _deletedEvents.Add(p2.CircleEvent);

            var p = new Point(e.Point.X,GetY(p1.FocusPoint,e.Point.X));
            _points.Add(p);

            xl.Edge.End = p;
            xr.Edge.End = p;

            VoronoiParabola higher = null;
            var par = p1;
            while (par != _root)
            {
                par = par.Parent;
                if (par == xl) higher = xl;
                if (par == xr) higher = xr;
            }

            higher.Edge = new VoronoiEdge(p,p0.FocusPoint,p2.FocusPoint);
            _edges.Add(higher.Edge);

            var gPar = p1.Parent.Parent;
            if (p1.Parent.Left == p1)
            {
                if (gPar.Left == p1.Parent) gPar.Left = p1.Parent.Right;
                if (gPar.Right == p1.Parent) gPar.Right = p1.Parent.Right;
            }
            else
            {
                if (gPar.Left == p1.Parent) gPar.Left = p1.Parent.Left;
                if (gPar.Right == p1.Parent) gPar.Right = p1.Parent.Left;
            }

            CheckCircle(p0);
            CheckCircle(p2);
        }

        private void CheckCircle(VoronoiParabola b)
        {
            var lp = VoronoiParabola.GetLeftParent(b);
            var rp = VoronoiParabola.GetRightParent(b);

            var a = VoronoiParabola.GetLeftChild(lp);
            var c = VoronoiParabola.GetRightChild(rp);

            if( a == null || b == null || a.FocusPoint == c.FocusPoint)
                return;

            var s = Point.Zero;
            s = GetEdgeIntersection(lp.Edge, rp.Edge);
            if(s == null) 
                return;


            var dx = a.FocusPoint.X - s.X;
            var dy = a.FocusPoint.Y - s.Y;

            var d = Math.Sqrt((dx*dx) + (dy*dy));
            if (s.Y - d >= _currentY)
                return;

            //create new Event
            var e = new VoronoiEvent(new Point(s.X,s.Y - d),false);
            _points.Add(e.Point);
            b.CircleEvent = e;
            e.Arch = b;
            InsertEvent(e);
        }

        private Point GetEdgeIntersection(VoronoiEdge a, VoronoiEdge b)
        {
            double x = (b.G - a.G) / (a.F - b.F);
            double y = a.F*x + a.G;

            if ((x - a.Start.X) / a.Direction.X < 0) return null;
            if ((y - a.Start.Y) / a.Direction.Y < 0) return null;

            if ((x - b.Start.X) / b.Direction.X < 0) return null;
            if ((y - b.Start.Y) / b.Direction.Y < 0) return null;

            var p = new Point(x, y);
            _points.Add(p);
            return p;
        }

        private VoronoiParabola GetParabolaByX(double d)
        {
            var par = _root;
            var x = 0.0;

            while (!par.IsLeaf)
            {
                x = GetXOfEdge(par, _currentY);
                par = x > d ? par.Left : par.Right;
            }

            return par;
        }

        private double GetXOfEdge(VoronoiParabola par, double y)
        {
            var left = VoronoiParabola.GetLeftChild(par);
            var right = VoronoiParabola.GetRightChild(par);

            var p = left.FocusPoint;
            var r = right.FocusPoint;

            double dp = 2.0 * (p.Y - y);
            double a1 = 1.0 / dp;
            double b1 = -2.0 * p.X / dp;
            double c1 = y + dp / 4 + p.X * p.X / dp;

            dp = 2.0 * (r.Y - y);
            double a2 = 1.0 / dp;
            double b2 = -2.0 * r.X / dp;
            double c2 = _currentY + dp / 4 + r.X * r.X / dp;

            double a = a1 - a2;
            double b = b1 - b2;
            double c = c1 - c2;

            double disc = b * b - 4 * a * c;
            double x1 = (-b + Math.Sqrt(disc)) / (2 * a);
            double x2 = (-b - Math.Sqrt(disc)) / (2 * a);

            var ry = p.Y < r.Y ? Math.Max(x1, x2) : Math.Min(x1, x2);

            return ry;
        }

        private double GetY(Point p, double x)
        {
            double dp = 2 * (p.Y - _currentY);
            double a1 = 1 / dp;
            double b1 = -2 * p.X / dp;
            double c1 = _currentY + dp / 4 + p.X * p.X / dp;

            return (a1 * x * x + b1 * x + c1);
        }
    }
}
