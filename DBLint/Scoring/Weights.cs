using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;

namespace DBLint
{
    public interface IIssuePenalty
    {
        float GetColumnIssuePenalty(Severity severity);
        float GetTableIssuePenalty(Severity severity);
        float GetSchemaIssuePenalty(Severity severity);
    }

    public class IssuePenalties : IIssuePenalty
    {
        private static Dictionary<Severity, float> columnPenalties = new Dictionary<Severity, float>();
        private static Dictionary<Severity, float> tablePenalties = new Dictionary<Severity, float>();
        private static Dictionary<Severity, float> schemaPenalties = new Dictionary<Severity, float>();


        public IssuePenalties()
        {
            //Columns
            if (columnPenalties.Count == 0)
            {
                columnPenalties.Add(Severity.Critical, 0.25f);
                columnPenalties.Add(Severity.High, 0.30f);
                columnPenalties.Add(Severity.Medium, 0.35f);
                columnPenalties.Add(Severity.Low, 0.40f);
            }

            //Tables
            if (tablePenalties.Count == 0)
            {
                tablePenalties.Add(Severity.Critical, 0.35f);
                tablePenalties.Add(Severity.High, 0.45f);
                tablePenalties.Add(Severity.Medium, 0.55f);
                tablePenalties.Add(Severity.Low, 0.65f);
            }

            //Schemas
            if (schemaPenalties.Count == 0)
            {
                schemaPenalties.Add(Severity.Critical, 0.75f);
                schemaPenalties.Add(Severity.High, 0.80f);
                schemaPenalties.Add(Severity.Medium, 0.85f);
                schemaPenalties.Add(Severity.Low, 0.90f);
            }
        }

        #region IIssuePenalty Members

        public float GetColumnIssuePenalty(Severity severity)
        {
            return columnPenalties[severity];
        }

        public float GetTableIssuePenalty(Severity severity)
        {
            return tablePenalties[severity];
        }

        public float GetSchemaIssuePenalty(Severity severity)
        {
            return schemaPenalties[severity];
        }

        #endregion
    }
}
