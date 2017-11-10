using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Rules.Utils.RTree;

namespace DBLint.Rules.OutlierDetection
{
    public abstract class NumberVector
    {
        public virtual float[] GetVector(Dimensions dimensions, decimal value)
        {
            if (dimensions == Dimensions.Two)
                return this.Get2DVector(value);
            else if (dimensions == Dimensions.Three)
                return this.Get3DVector(value);
            return null;
        }

        public abstract float[] Get2DVector(decimal value);

        public abstract float[] Get3DVector(decimal value);
    }

    public class OneDimVector : NumberVector
    {
        public override float[] Get2DVector(decimal value)
        {
            return new float[] { this.getX(value), this.getY(value) };
        }

        public override float[] Get3DVector(decimal value)
        {
            return new float[] { this.getX(value), this.getY(value), this.getZ(value) };
        }

        private float getX(decimal value)
        {
            return 0;
        }

        private float getY(decimal value)
        {
            return (float)value;
        }

        private float getZ(decimal value)
        {
            return 0;
        }
    }
}
