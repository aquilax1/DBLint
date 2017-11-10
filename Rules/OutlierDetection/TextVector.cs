using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DBLint.Rules.Utils.RTree;

namespace DBLint.Rules.OutlierDetection
{
    public abstract class TextVector
    {
        public virtual float[] GetVector(Dimensions dimensions, string value)
        {
            if (dimensions == Dimensions.Two)
                return this.Get2DVector(value);
            else if (dimensions == Dimensions.Three)
                return this.Get3DVector(value);
            return null;
        }

        public abstract float[] Get2DVector(string value);
        
        public abstract float[] Get3DVector(string value);
    }

    public class TextSizes : TextVector
    {
        public override float[] Get2DVector(string value)
        {
            return new float[] { this.getX(value), this.getY(value) };
        }

        public override float[] Get3DVector(string value)
        {
            return new float[] { this.getX(value), this.getY(value), this.getZ(value) };
        }

        public float getX(string value)
        {
            // Number of words
            return (float)value.Split(' ').Length;
        }

        public float getY(string value)
        {
            // Length of value
            return (float)value.Length;
        }

        public float getZ(string value)
        {
            // Average length of words
            return (float)value.Split(' ').ToList().Average(n => n.Length);
        }
    }

    public class TextType : TextVector
    {
        public override float[] Get2DVector(string value)
        {
            return new float[] { this.getX(value), this.getY(value) };
        }

        public override float[] Get3DVector(string value)
        {
            return new float[] { this.getX(value), this.getY(value), this.getZ(value) };
        }

        private float getX(string value)
        {
            // Average length of words
            return (float)value.Split(' ').ToList().Average(n => n.Length);
        }

        Regex numeric = new Regex("([0-9])", RegexOptions.Compiled);

        private float getY(string value)
        {
            // Number of numeric values in string
            return numeric.Matches(value).Count;
        }

        private float getZ(string value)
        {
            // Length of value
            return (float)value.Length;
        }
    }
}
