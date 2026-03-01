using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Take4_at_rendering;

namespace Take4_at_rendering
{
    class Program
    {
        public static bool isCameraPerspective = true;
        public static float orthoScaler = 0.05f;


        private static IWindow window;
        private static IKeyboard primaryKeyboard;

        private static GL Gl;
        private static Shader Shader;
        private static List<ModelInstance> GeometryInstances = new();
        private static Skybox Skybox;

        //Setup the camera's location, directions, and movement speed
        private static Vector3 CameraPosition = new Vector3(0.0f, 0.0f, 3.0f);
        private static Vector3 CameraFront = new Vector3(0.0f, 0.0f, -1.0f);
        private static Vector3 CameraUp = Vector3.UnitY;
        private static Vector3 CameraDirection = Vector3.Zero;
        private static float CameraYaw = -90f;
        private static float CameraPitch = 0f;
        private static float CameraZoom = 45f;

        //Used to track change in mouse movement to allow for moving of the Camera
        private static Vector2 LastMousePosition;

        private static void Main(string[] args) {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1920, 1080);
            options.Title = "Cardinal renderer";


            window = Window.Create(options);

            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Render += OnRender;
            window.FramebufferResize += OnFramebufferResize;
            window.Closing += OnClose;
            
            window.Run();

            window.Dispose();
        }

        private static void OnLoad() {
            IInputContext input = window.CreateInput();
            primaryKeyboard = input.Keyboards.FirstOrDefault();
            if (primaryKeyboard != null) {
                primaryKeyboard.KeyDown += KeyDown;
            }
            for (int i = 0; i < input.Mice.Count; i++) {
                input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
                input.Mice[i].MouseMove += OnMouseMove;
            }

            Gl = GL.GetApi(window);

            Shader = new Shader(Gl, "shaders/shader.vert", "shaders/shader.frag");
            GenerateGeometry();

            Skybox = new Skybox(Gl, new[] {
                "skybox/right.png", "skybox/left.png",
                "skybox/top.png",   "skybox/bottom.png",
                "skybox/front.png", "skybox/back.png"
            });
        }

        private static unsafe void OnUpdate(double deltaTime) {
            var moveSpeed = 2.5f * (float)deltaTime;

            if (primaryKeyboard.IsKeyPressed(Key.W)) {
                //Move forwards
                CameraPosition += moveSpeed * CameraFront;
            }
            if (primaryKeyboard.IsKeyPressed(Key.S)) {
                //Move backwards
                CameraPosition -= moveSpeed * CameraFront;
            }
            if (primaryKeyboard.IsKeyPressed(Key.A)) {
                //Move left
                CameraPosition -= Vector3.Normalize(Vector3.Cross(CameraFront, CameraUp)) * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.D)) {
                //Move right
                CameraPosition += Vector3.Normalize(Vector3.Cross(CameraFront, CameraUp)) * moveSpeed;
            }


            Console.WriteLine(1.0 / deltaTime);
        }

        private static unsafe void OnRender(double deltaTime) {
            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.CullFace);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var size = window.FramebufferSize;
            var view = Matrix4x4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
            
            var projection = Matrix4x4.Identity;
            if (isCameraPerspective) {
                projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CameraZoom), (float)size.X / size.Y, 0.1f, 100.0f);

            } else {
                projection = Matrix4x4.CreateOrthographic(size.X * orthoScaler, size.Y * orthoScaler, 0.1f, 100.0f);
            }

            foreach (var instance in GeometryInstances) { 
                foreach (var mesh in instance.Model.Meshes) {
                    mesh.Bind();
                    Shader.Use();
                    instance.Texture.Bind();
                    Shader.SetUniform("uTexture0", 0);
                    Shader.SetUniform("uModel", instance.Transform.ViewMatrix);
                    Shader.SetUniform("uView", view);
                    Shader.SetUniform("uProjection", projection);
                    Shader.SetUniform("uLightDir", new Vector3(1.0f, -1.0f, 0.5f)); 
                    Shader.SetUniform("uAmbient", 0.15f);

                    Gl.DrawElements(PrimitiveType.Triangles, (uint)mesh.Indices.Length, DrawElementsType.UnsignedInt, null);
                }
            }

            Skybox.Render(view, projection, window.FramebufferSize);
        }

        private static void OnFramebufferResize(Vector2D<int> newSize) {
            Gl.Viewport(newSize);
        }

        private static unsafe void OnMouseMove(IMouse mouse, Vector2 position) {
            var lookSensitivity = 0.1f;
            if (LastMousePosition == default) {
                LastMousePosition = position;
            } else {
                var xOffset = (position.X - LastMousePosition.X) * lookSensitivity;
                var yOffset = (position.Y - LastMousePosition.Y) * lookSensitivity;
                LastMousePosition = position;

                CameraYaw += xOffset;
                CameraPitch -= yOffset;

                //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
                CameraPitch = Math.Clamp(CameraPitch, -89.0f, 89.0f);

                CameraDirection.X = MathF.Cos(MathHelper.DegreesToRadians(CameraYaw)) * MathF.Cos(MathHelper.DegreesToRadians(CameraPitch));
                CameraDirection.Y = MathF.Sin(MathHelper.DegreesToRadians(CameraPitch));
                CameraDirection.Z = MathF.Sin(MathHelper.DegreesToRadians(CameraYaw)) * MathF.Cos(MathHelper.DegreesToRadians(CameraPitch));
                CameraFront = Vector3.Normalize(CameraDirection);
            }
        }

        private static void OnClose() {
            foreach (var model3d in GeometryInstances) {
                model3d.Model.Dispose();
                model3d.Texture.Dispose();

            }

            Shader.Dispose();

            Skybox.Dispose();
        }

        private static void KeyDown(IKeyboard keyboard, Key key, int arg3) {
            if (key == Key.Escape) {
                window.Close();
            }
        }



        private static void GenerateGeometry() {

            for (int i = 0; i < 50; i++) {
                var t1 = new Transform { Position = new Vector3(i, 0, 0)};
                GeometryInstances.Add(new ModelInstance(
                new Model(Gl, "models/cube.model"),
                t1,
                new Texture(Gl, "textures/testTex.png")
                ));
                
            }

            var t2 = new Transform { };
            GeometryInstances.Add(new ModelInstance(
                new Model(Gl, "models/groundPlane.obj"),
                t2,
                new Texture(Gl, "textures/testTex.png")
                ));
        }
    }
}
