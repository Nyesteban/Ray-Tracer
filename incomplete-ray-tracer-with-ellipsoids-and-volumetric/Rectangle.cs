using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rt
{
    public class Rectangle
    {
        private double _xmin;
        private double _xmax;
        private double _ymin;
        private double _ymax;

        public Rectangle(double xmin, double xmax, double ymin, double ymax)
        {
            _xmin = xmin;
            _xmax = xmax;
            _ymin = ymin;
            _ymax = ymax;
        }

        public bool Contains(double x, double y)
        {
            return x - _xmin > 0.0001 && x - _xmax < 0.0001 && y - _ymin > 0.0001 && y - _ymax < 0.0001;
        }
    }
}
