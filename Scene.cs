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
        public event Action<Scene> OnPopulate;

        private GL _gl;
        private Skybox _skybox;

        public SceneNode Root { get; private set; } = new SceneNode("Root");

        private List<Node> _deleteQueue = new();

        public void Load(GL gl) {
            _gl = gl;
            LoadSkybox();
            LoadInstances();
        }

        private void LoadInstances() {
            OnPopulate?.Invoke(this);
        }

        private void LoadSkybox() {
            _skybox = new Skybox(_gl, new[] {
                "skybox/right.png", "skybox/left.png",
                "skybox/top.png",   "skybox/bottom.png",
                "skybox/front.png", "skybox/back.png"
            });
        }

        public void Update(double time, double deltaTime) {
            foreach (var node in _deleteQueue)
                node.QueueFree();
            _deleteQueue.Clear();

            Root.Update(time, deltaTime);
        }

        public unsafe void Render(Shader shader, Matrix4x4 view, Matrix4x4 projection, Vector2D<int> framebufferSize) {
            
            Root.Render(shader, view, projection);
            _skybox.Render(view, projection, framebufferSize);
        }

        public void AddToRoot(Node node) {
            Root.AddChild(node);
        }

        public void QueueFree(Node node) {
            _deleteQueue.Add(node);
        }

        public void Dispose() {
            Root.Dispose();
            _skybox.Dispose();
        }
    }
}