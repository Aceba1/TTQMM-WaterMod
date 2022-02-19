using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace WaterMod
{
    internal static class ReflectionHelper
    {
        /// <summary>
        /// Gets the value of the requested private field, using reflection, from the instance object.
        /// </summary>
        /// <typeparam name="T">The instance class type.</typeparam>
        /// <param name="instance">The instance.</param>
        /// <param name="fieldName">Name of the private field.</param>
        /// <param name="bindingFlags">The additional binding flags you wish to set.
        /// <see cref="BindingFlags.NonPublic" /> and <see cref="BindingFlags.Instance" /> are already included.</param>
        /// <returns>
        /// The value of the requested field as an <see cref="object" />.
        /// </returns>
        public static object GetInstanceField<T>(this T instance, string fieldName, BindingFlags bindingFlags = BindingFlags.Default) where T : class
            => typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | bindingFlags).GetValue(instance);

        /// <summary>
        /// Does a deep copy of all field values from the original instance onto the copied instance.
        /// </summary>
        /// <typeparam name="T">The class type of both objects.</typeparam>
        /// <param name="original">The original instance.</param>
        /// <param name="copy">The instance receiving the copied values.</param>
        /// <param name="bindingFlags">The additional binding flags you wish to set.
        /// <see cref="BindingFlags.Instance" /> is already included.</param>
        public static void CopyFields<T>(this T original, T copy, BindingFlags bindingFlags = BindingFlags.Default) where T : class
        {
            FieldInfo[] fieldsInfo = typeof(T).GetFields(BindingFlags.Instance | bindingFlags);

            foreach (FieldInfo fieldInfo in fieldsInfo)
            {
                if (fieldInfo.GetType().IsClass)
                {
                    var origValue = fieldInfo.GetValue(original);
                    var copyValue = fieldInfo.GetValue(copy);

                    origValue.CopyFields(copyValue);
                }
                else
                {
                    var value = fieldInfo.GetValue(original);
                    fieldInfo.SetValue(copy, value);
                }
            }
        }
    }
}
