using System;
using System.Collections.Generic;

namespace SharpFontManaged
{
    public class TextLayout
    {
        public List<Data> Stuff = new List<Data>();

        internal void SetCount(int count)
        {
            Stuff.Clear();
            Stuff.Capacity = count;
        }

        internal void AddGlyph(int destX, int destY, int sourceX, int sourceY, int width, int height)
        {
            Stuff.Add(new Data
            {
                DestX = destX,
                DestY = destY,
                SourceX = sourceX,
                SourceY = sourceY,
                Width = width,
                Height = height
            });
        }
    }
    public struct Data : IEquatable<Data>
    {
        public int DestX, DestY;
        public int SourceX, SourceY;
        public int Width, Height;

        public override bool Equals(object obj) => obj is Data d
            && d.DestX.Equals(DestX) && d.DestY.Equals(DestY)
            && d.SourceX.Equals(SourceX) && d.SourceY.Equals(SourceY)
            && d.Width.Equals(Width) && d.Height.Equals(Height);

        public override int GetHashCode() => HashCode.Combine(
            DestX.GetHashCode(), DestY.GetHashCode(),
            SourceX.GetHashCode(), SourceY.GetHashCode(),
            Width.GetHashCode(), Height.GetHashCode());


        public bool Equals(Data other) => Equals(other);

        public static bool operator ==(Data left, Data right) => left.Equals(right);

        public static bool operator !=(Data left, Data right) => !(left == right);
    }
}
