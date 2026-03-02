using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PETRenderer
{
    public class Scene : IDisposable
    {
        private GL _gl;
        private List<ModelInstance> _instances = new();
        private Skybox _skybox;

        public IReadOnlyList<ModelInstance> Instances => _instances;

        public void Load(GL gl) {
            _gl = gl;
            LoadInstances();
            LoadSkybox();
        }

        private void LoadInstances() {
            _instances.Add(new ModelInstance(
                new Model(_gl, "models/groundPlane.obj"),
                new Transform(),
                new Texture(_gl, "textures/testTex.png")
            ));

            _instances.Add(new ModelInstance(
                new Model(_gl, "models/cineball.obj"),
                new Transform(),
                new Texture(_gl, "textures/absolute.png")
            ));

            _instances.Add(new ModelInstance(
                new Model(_gl, "models/cineball.obj"),
                new Transform { Position = new Vector3(0, 0, -2f) },
                new Texture(_gl, "textures/testTex.png")
            ));

            _instances.Add(new ModelInstance(
                new Model(_gl, "models/cineball.obj"),
                new Transform { Position = new Vector3(0, 0, 2f) },
                new Texture(_gl, "textures/silk.png")
            ));
        }

        private void LoadSkybox() {
            _skybox = new Skybox(_gl, new[] {
                "skybox/right.png", "skybox/left.png",
                "skybox/top.png",   "skybox/bottom.png",
                "skybox/front.png", "skybox/back.png"
            });
        }

        public void Update(double time) {
            // Rotate last instance over time as an example
            _instances[_instances.Count - 1].Transform.Rotation =
                Quaternion.CreateFromAxisAngle(Vector3.UnitY,
                    MathHelper.DegreesToRadians((float)time * 45f));
        }

        public unsafe void Render(Shader shader, Matrix4x4 view, Matrix4x4 projection, Vector2D<int> framebufferSize) {
            foreach (var instance in _instances) {
                foreach (var mesh in instance.Model.Meshes) {
                    mesh.Bind();
                    shader.Use();
                    instance.Texture.Bind();
                    shader.SetUniform("uTexture0", 0);
                    shader.SetUniform("uModel", instance.Transform.ViewMatrix);
                    shader.SetUniform("uView", view);
                    shader.SetUniform("uProjection", projection);
                    shader.SetUniform("uLightDir", new Vector3(-1.0f, -1.0f, -0.5f));
                    shader.SetUniform("uAmbient", 0.15f);

                    // gl is needed here - expose it or pass it in
                    _gl.DrawElements(PrimitiveType.Triangles,
                        (uint)mesh.Indices.Length,
                        DrawElementsType.UnsignedInt, null);
                }
            }

            _skybox.Render(view, projection, framebufferSize);
        }

        public void Dispose() {
            foreach (var instance in _instances) {
                instance.Model.Dispose();
                instance.Texture.Dispose();
            }
            _skybox.Dispose();
        }
    }
}