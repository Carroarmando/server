using System;
using SFML.Graphics;
using SFML.System;

namespace LatoServer
{
    internal class Casa
    {
        IntRect rect;
        Texture texture;
        public Sprite sprite;

        int origine;
        public Casa(int origine)
        {
            this.origine = origine;
        }

        public static implicit operator Casa(int origine) => new Casa(origine);
        public static explicit operator int(Casa c) => c.origine;
    }
}
