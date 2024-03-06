using System;
using System.Runtime.InteropServices;

namespace rt
{
    class RayTracer
    {
        private Geometry[] geometries;
        private Light[] lights;

        public RayTracer(Geometry[] geometries, Light[] lights)
        {
            this.geometries = geometries;
            this.lights = lights;
        }

        private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
        {
            return -n * viewPlaneSize / imgSize + viewPlaneSize / 2;
        }

        private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
        {
            var intersection = Intersection.NONE;

            foreach (var geometry in geometries)
            {
                var intr = geometry.GetIntersection(ray, minDist, maxDist);

                if (!intr.Valid || !intr.Visible) continue;

                if (!intersection.Valid || !intersection.Visible)
                {
                    intersection = intr;
                }
                else if (intr.T < intersection.T)
                {
                    intersection = intr;
                }
            }

            return intersection;
        }

        private bool IsLit(Vector point, Light light, Ellipsoid ellipsoid)
        {
            Line line = new Line(point, light.Position);
            foreach (var geometry in geometries)
            {
                if (!(geometry is RawCtMask))
                {
                    Ellipsoid ellipsoid2 = (Ellipsoid)geometry;
                    // skip the sphere the point is on
                    if ((ellipsoid2.Center - ellipsoid.Center).Length() < 0.001)
                    {
                        continue;
                    }

                    // other spheres:
                    Intersection intersection = ellipsoid2.GetIntersection(line, 0, 1000);
                    if (intersection.T > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void Render(Camera camera, int width, int height, string filename)
        {
            var background = new Color(0.2, 0.2, 0.2, 1.0);

            var image = new Image(width, height);

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    // TODO: ADD CODE HERE
                    var point = camera.Position + camera.Direction * camera.ViewPlaneDistance +
                                camera.Up * ImageToViewPlane(j, height, camera.ViewPlaneHeight) +
                                (camera.Up ^ camera.Direction) * ImageToViewPlane(i, width, camera.ViewPlaneWidth);
                    Line ray = new Line(camera.Position, point);
                    Intersection intersection = FindFirstIntersection(ray, camera.FrontPlaneDistance, camera.BackPlaneDistance);
                    if (intersection.Valid && intersection.Visible)
                    {
                        var color = new Color();
                        foreach (var light in lights)
                        {
                            var colorFromLight = new Color();
                            if(intersection.Geometry is RawCtMask)
                            {
                                colorFromLight = intersection.Material.Ambient * light.Ambient;
                                var v = intersection.Position;
                                var e = (camera.Position - v).Normalize();
                                var n = intersection.Normal;
                                var t = (light.Position - v).Normalize();
                                var r = (n * (n * t) * 2 - t).Normalize();
                                if (n * t > 0)
                                    colorFromLight += intersection.Material.Diffuse * light.Diffuse * (n * t);
                                if (e * r > 0)
                                    colorFromLight += intersection.Material.Specular * light.Specular * Math.Pow(e * r, intersection.Material.Shininess);
                                color *= light.Intensity;
                            }
                            else if (IsLit(intersection.Position, light, (Ellipsoid) intersection.Geometry))
                            {
                                colorFromLight = intersection.Geometry.Material.Ambient * light.Ambient;
                                var v = intersection.Position;
                                var e = (camera.Position - v).Normalize();
                                var n = intersection.Normal;
                                var t = (light.Position - v).Normalize();
                                var r = (n * (n * t) * 2 - t).Normalize();
                                if (n * t > 0)
                                    colorFromLight += intersection.Geometry.Material.Diffuse * light.Diffuse * (n * t);
                                if (e * r > 0)
                                    colorFromLight += intersection.Geometry.Material.Specular * light.Specular * Math.Pow(e * r, intersection.Geometry.Material.Shininess);
                                color *= light.Intensity;
                            }
                            else if(!IsLit(intersection.Position, light, (Ellipsoid) intersection.Geometry))
                                colorFromLight = intersection.Geometry.Material.Ambient * light.Ambient;
                            color += colorFromLight;
                        }
                        image.SetPixel(i, j, color);
                    }
                    else
                        image.SetPixel(i, j, background);
                }
            }

            image.Store(filename);
        }
    }
}