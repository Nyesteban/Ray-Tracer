using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace rt;

public class RawCtMask: Geometry
{
    private readonly Vector _position;
    private readonly double _scale;
    private readonly ColorMap _colorMap;
    private readonly byte[] _data;

    private readonly int[] _resolution = new int[3];
    private readonly double[] _thickness = new double[3];
    private readonly Vector _v0;
    private readonly Vector _v1;

    public RawCtMask(string datFile, string rawFile, Vector position, double scale, ColorMap colorMap) : base(Color.NONE)
    {
        _position = position;
        _scale = scale;
        _colorMap = colorMap;

        var lines = File.ReadLines(datFile);
        foreach (var line in lines)
        {
            var kv = Regex.Replace(line, "[:\\t ]+", ":").Split(':');
            if (kv[0] == "Resolution")
            {
                _resolution[0] = Convert.ToInt32(kv[1]);
                _resolution[1] = Convert.ToInt32(kv[2]);
                _resolution[2] = Convert.ToInt32(kv[3]);
            } else if (kv[0] == "SliceThickness")
            {
                _thickness[0] = Convert.ToDouble(kv[1]);
                _thickness[1] = Convert.ToDouble(kv[2]);
                _thickness[2] = Convert.ToDouble(kv[3]);
            }
        }

        _v0 = position;
        _v1 = position + new Vector(_resolution[0]*_thickness[0]*scale, _resolution[1]*_thickness[1]*scale, _resolution[2]*_thickness[2]*scale);

        var len = _resolution[0] * _resolution[1] * _resolution[2];
        _data = new byte[len];
        using FileStream f = new FileStream(rawFile, FileMode.Open, FileAccess.Read);
        if (f.Read(_data, 0, len) != len)
        {
            throw new InvalidDataException($"Failed to read the {len}-byte raw data");
        }
    }
    
    private ushort Value(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= _resolution[0] || y >= _resolution[1] || z >= _resolution[2])
        {
            return 0;
        }

        return _data[z * _resolution[1] * _resolution[0] + y * _resolution[0] + x];
    }

    public override Intersection GetIntersection(Line line, double minDist, double maxDist)
    {
        Rectangle xFaces = new Rectangle(_v0.Y, _v1.Y, _v0.Z, _v1.Z);
        Rectangle yFaces = new Rectangle(_v0.X, _v1.X, _v0.Z, _v1.Z);
        Rectangle zFaces = new Rectangle(_v0.X, _v1.X, _v0.Y, _v1.Y);

        double t1_x = (_v0.X - line.X0.X) / line.Dx.X;
        double t2_x = (_v1.X - line.X0.X) / line.Dx.X;
        double t1_y = (_v0.Y - line.X0.Y) / line.Dx.Y;
        double t2_y = (_v1.Y - line.X0.Y) / line.Dx.Y;
        double t1_z = (_v0.Z - line.X0.Z) / line.Dx.Z;
        double t2_z = (_v1.Z - line.X0.Z) / line.Dx.Z;

        Vector pos_t1_x = line.CoordinateToPosition(t1_x);
        Vector pos_t2_x = line.CoordinateToPosition(t2_x);
        Vector pos_t1_y = line.CoordinateToPosition(t1_y);
        Vector pos_t2_y = line.CoordinateToPosition(t2_y);
        Vector pos_t1_z = line.CoordinateToPosition(t1_z);
        Vector pos_t2_z = line.CoordinateToPosition(t2_z);

        int assigned_ts = 0;
        double t1 = 0;
        double t2 = 0;

        if (xFaces.Contains(pos_t1_x.Y, pos_t1_x.Z) && assigned_ts < 2)
        {
            if (assigned_ts == 0)
            {
                t1 = t1_x;
            }

            if (assigned_ts == 1)
            {
                t2 = t1_x;
            }

            assigned_ts++;
        }

        if (xFaces.Contains(pos_t2_x.Y, pos_t2_x.Z) && assigned_ts < 2)
        {
            if (assigned_ts == 0)
            {
                t1 = t2_x;
            }

            if (assigned_ts == 1)
            {
                t2 = t2_x;
            }

            assigned_ts++;
        }

        if (yFaces.Contains(pos_t1_y.X, pos_t1_y.Z) && assigned_ts < 2)
        {
            if (assigned_ts == 0)
            {
                t1 = t1_y;
            }

            if (assigned_ts == 1)
            {
                t2 = t1_y;
            }

            assigned_ts++;
        }

        if (yFaces.Contains(pos_t2_y.X, pos_t2_y.Z) && assigned_ts < 2)
        {
            if (assigned_ts == 0)
            {
                t1 = t2_y;
            }

            if (assigned_ts == 1)
            {
                t2 = t2_y;
            }

            assigned_ts++;
        }

        if (zFaces.Contains(pos_t1_z.X, pos_t1_z.Y) && assigned_ts < 2)
        {
            if (assigned_ts == 0)
            {
                t1 = t1_z;
            }

            if (assigned_ts == 1)
            {
                t2 = t1_z;
            }

            assigned_ts++;
        }

        if (zFaces.Contains(pos_t2_z.X, pos_t2_z.Y) && assigned_ts < 2)
        {
            if (assigned_ts == 0)
            {
                t1 = t2_z;
            }

            if (assigned_ts == 1)
            {
                t2 = t2_z;
            }

            assigned_ts++;
        }

        if (assigned_ts < 2)
        {
            return Intersection.NONE;
        }

        if (t1 > t2)
        {
            (t1, t2) = (t2, t1);
        }

        double sampleStep = _scale;

        double firstT = t1;
        double lastT = t2;

        while (GetColor(line.CoordinateToPosition(firstT)).Alpha < 0.00001 && firstT < lastT)
        {
            firstT += sampleStep;
        }

        while (GetColor(line.CoordinateToPosition(lastT)).Alpha < 0.00001 && lastT > firstT)
        {
            lastT -= sampleStep;
        }

        Color sampleColor = GetColor(line.CoordinateToPosition(lastT));
        double sampleAlpha = sampleColor.Alpha;
        Color currentColor = new Color(sampleColor.Red * sampleAlpha,
                                        sampleColor.Green * sampleAlpha,
                                        sampleColor.Blue * sampleAlpha,
                                        sampleAlpha);

        for (double sampleT = lastT; sampleT > firstT; sampleT -= sampleStep)
        {
            sampleColor = GetColor(line.CoordinateToPosition(sampleT));
            sampleAlpha = sampleColor.Alpha;

            currentColor *= 1 - sampleAlpha;
            currentColor += new Color(sampleColor.Red * sampleAlpha,
                                        sampleColor.Green * sampleAlpha,
                                        sampleColor.Blue * sampleAlpha,
                                        sampleAlpha);
        }

        if (currentColor.Alpha < 0.00001)
        {
            return Intersection.NONE;
        }

        return new Intersection(
            true,
            true,
            this,
            line,
            firstT,
            GetNormal(line.CoordinateToPosition(firstT)),
            Material.FromColor(currentColor),
            currentColor
        );
    }

    private int[] GetIndexes(Vector v)
    {
        return new []{
            (int)Math.Floor((v.X - _position.X) / _thickness[0] / _scale), 
            (int)Math.Floor((v.Y - _position.Y) / _thickness[1] / _scale),
            (int)Math.Floor((v.Z - _position.Z) / _thickness[2] / _scale)};
    }
    private Color GetColor(Vector v)
    {
        int[] idx = GetIndexes(v);

        ushort value = Value(idx[0], idx[1], idx[2]);
        return _colorMap.GetColor(value);
    }

    private Vector GetNormal(Vector v)
    {
        int[] idx = GetIndexes(v);
        double x0 = Value(idx[0] - 1, idx[1], idx[2]);
        double x1 = Value(idx[0] + 1, idx[1], idx[2]);
        double y0 = Value(idx[0], idx[1] - 1, idx[2]);
        double y1 = Value(idx[0], idx[1] + 1, idx[2]);
        double z0 = Value(idx[0], idx[1], idx[2] - 1);
        double z1 = Value(idx[0], idx[1], idx[2] + 1);

        return new Vector(x1 - x0, y1 - y0, z1 - z0).Normalize();
    }
}