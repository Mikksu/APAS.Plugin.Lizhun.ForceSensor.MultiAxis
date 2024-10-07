using System;
using System.ComponentModel;

namespace APAS.Plugin.KEYTHLEY._2600.Extensions
{
    public static class GenericTypeConverter
    {
        public static T ConvertTo<T>(this object value)
        {
            if (value is T variable) return variable;

            try
            {
                //Handling Nullable types i.e, int?, double?, bool? .. etc
                if (Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value);
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
