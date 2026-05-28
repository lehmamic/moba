using MOBA.Engine.Graphics;
using Silk.NET.Maths;

namespace MOBA.Game.Client;

/// <summary>
/// UV-sphere generator. Default 16 longitude × 8 latitude segments gives ~144
/// vertices and 256 triangles - cheap enough for placeholder markers. Triangles
/// are wound CCW from the outside, matching the front-face setting in
/// <c>OpenGLBackend</c>.
/// </summary>
public static class SphereMesh
{
    public static IMesh Create(IGraphicsBackend backend, float radius = 0.5f, int lonSegments = 16, int latSegments = 8)
    {
        var ringSize = lonSegments + 1;
        var vertexCount = ringSize * (latSegments + 1);
        var vertices = new Vertex[vertexCount];

        for (var lat = 0; lat <= latSegments; lat++)
        {
            var theta = MathF.PI * lat / latSegments;
            var sinTheta = MathF.Sin(theta);
            var cosTheta = MathF.Cos(theta);

            for (var lon = 0; lon <= lonSegments; lon++)
            {
                var phi = 2f * MathF.PI * lon / lonSegments;
                var sinPhi = MathF.Sin(phi);
                var cosPhi = MathF.Cos(phi);

                var x = radius * sinTheta * cosPhi;
                var y = radius * cosTheta;
                var z = radius * sinTheta * sinPhi;

                var u = (float)lon / lonSegments;
                var v = (float)lat / latSegments;

                vertices[(lat * ringSize) + lon] = new Vertex(
                    new Vector3D<float>(x, y, z),
                    new Vector2D<float>(u, v));
            }
        }

        var indices = new uint[lonSegments * latSegments * 6];
        var idx = 0;
        for (var lat = 0; lat < latSegments; lat++)
        {
            for (var lon = 0; lon < lonSegments; lon++)
            {
                var topLeft = (uint)((lat * ringSize) + lon);
                var topRight = topLeft + 1;
                var bottomLeft = topLeft + (uint)ringSize;
                var bottomRight = bottomLeft + 1;

                // CCW when viewed from outside (outward normal = position / radius).
                indices[idx++] = topLeft;
                indices[idx++] = bottomLeft;
                indices[idx++] = topRight;

                indices[idx++] = topRight;
                indices[idx++] = bottomLeft;
                indices[idx++] = bottomRight;
            }
        }

        return backend.CreateMesh(vertices, indices);
    }
}
