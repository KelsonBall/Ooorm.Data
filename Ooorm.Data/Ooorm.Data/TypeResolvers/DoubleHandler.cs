﻿using Ooorm.Data.Attributes;
using Ooorm.Data.Reflection;
using System.Data;

namespace Ooorm.Data.TypeResolvers
{
    public class DoubleHandler : TypeHandler<double, double>
    {
        public override DbType GetDbType(Column column)
        {
            if (column.Info.TryGetAttribute(out CurrencyAttribute currency))
                return DbType.Currency;
            else
                return DbType.Decimal;
        }

        public override string GetDbTypeString(Column column)
        {
            if (column.Info.TryGetAttribute(out DecimalPrecisionAttribute precision))
                return $"DECIMAL({precision.Total}, {precision.Fractional})";
            else if (column.Info.TryGetAttribute(out CurrencyAttribute currency))
                return $"DECIMAL({currency.Total}, {currency.Fractional})";
            else
                return "REAL";
        }

        public override double Deserialize(double value) => value;

        public override double Serialize(double value) => value;
    }
}
