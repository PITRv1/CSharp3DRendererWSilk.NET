using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PETRenderer
{
    public class ModelInstance {
        public Model Model { get; set; }
        public Transform Transform {get; set;}
        public Texture Texture { get; set; }

        public ModelInstance(Model model, Transform transform, Texture texture) {
            Model = model;
            Transform = transform;
            Texture = texture;
        }
    }
}
