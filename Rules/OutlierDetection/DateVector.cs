using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.Rules.Utils.RTree;

namespace DBLint.Rules.OutlierDetection
{
    public abstract class DateVector
    {
        public virtual float[] GetVector(Dimensions dimensions, DateTime value)
        {
            if (dimensions == Dimensions.Two)
                return this.Get2DVector(value);
            else if (dimensions == Dimensions.Three)
                return this.Get3DVector(value);
            return null;
        }

        public abstract float[] Get2DVector(DateTime value);

        public abstract float[] Get3DVector(DateTime value);
    }

    public class DayVector : DateVector
    {
        public override float[] Get2DVector(DateTime value)
        {
            return new float[] { this.getX(value), this.getY(value) };
        }

        public override float[] Get3DVector(DateTime value)
        {
            return new float[] { this.getX(value), this.getY(value), this.getZ(value) };
        }

        public float getX(DateTime value)
        {
            return (int)value.DayOfWeek;
        }

        public float getY(DateTime value)
        {
            return value.DayOfYear;
        }

        public float getZ(DateTime value)
        {
            return value.Year;
        }
    }
}
