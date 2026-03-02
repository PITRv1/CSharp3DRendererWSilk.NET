using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System.Linq;
using System.Numerics;

namespace PETRenderer
{
    class Program
    {
        private static IWindow _window;
        private static IKeyboard _keyboard;
        private static Camera _camera;
        private static Scene _scene;
        private static Renderer _renderer;

        private static void Main(string[] args) {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1920, 1080);
            options.Title = "Cardinal Renderer";

            _window = Window.Create(options);
            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.FramebufferResize += OnResize;
            _window.Closing += OnClose;

            _window.Run();
            _window.Dispose();
        }

        private static void OnLoad() {
            var input = _window.CreateInput();
            _keyboard = input.Keyboards.FirstOrDefault();
            if (_keyboard != null)
                _keyboard.KeyDown += OnKeyDown;

            _camera = new Camera();
            _scene = new Scene();
            _renderer = new Renderer();

            _renderer.Initialize(_window);
            _scene.Load(_renderer.Gl);

            for (int i = 0; i < input.Mice.Count; i++) {
                input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
                input.Mice[i].MouseMove += (mouse, pos) => _camera.ProcessMouseMove(pos);
            }
        }

        private static void OnUpdate(double deltaTime) {
            _camera.ProcessKeyboard(_keyboard, (float)deltaTime);
            _scene.Update(_window.Time);
        }

        private static void OnRender(double deltaTime) {
            _renderer.Render(_scene, _camera, _window.FramebufferSize);
        }

        private static void OnResize(Vector2D<int> newSize) {
            _renderer.Resize(newSize);
        }

        private static void OnClose() {
            _scene.Dispose();
            _renderer.Dispose();
        }

        private static void OnKeyDown(IKeyboard keyboard, Key key, int arg) {
            if (key == Key.Escape)
                _window.Close();
        }
    }
}