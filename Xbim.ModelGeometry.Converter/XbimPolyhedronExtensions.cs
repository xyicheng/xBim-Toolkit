using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;

namespace Xbim.ModelGeometry.Converter
{
    public static class XbimPolyhedronExtensions
    {
        public static void WriteJson(this IXbimPolyhedron poly, JsonWriter writer)
        {
           
            writer.WritePropertyName("positions");
            writer.WriteStartArray();
            for (int i = 0; i < poly.VertexCount; i++)
            {
                XbimPoint3D vertex = poly.Vertex(i);
                writer.WriteValue(vertex.X);
                writer.WriteValue(vertex.Y);
                writer.WriteValue(vertex.Z);
            }
            writer.WriteEndArray();
            writer.WritePropertyName("indices");
            writer.WriteStartArray();
            foreach (var idx in poly.Triangulation(1e-5))
            {
                writer.WriteValue(idx);
            }
            writer.WriteEndArray();
        }

    }
}
