using Microsoft.Azure.WebJobs;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

namespace Pulumi.AzureFunctions.Sdk
{
    public class CallbackFunction
    {
        public CallbackFunction(MethodInfo callback, string name = null)
        {
            Callback = callback;
            Name = string.IsNullOrEmpty(name) ? callback.GetCustomAttribute<FunctionNameAttribute>().Name : name;
        }

        internal MethodInfo Callback { get; private set; }
        internal string Name { get; private set; }

        public static CallbackFunction[] FromAssembly(Assembly assembly)
        {
            return assembly
                .GetTypes()
                .SelectMany(FromClass)
                .ToArray();
        }

        public static CallbackFunction[] FromClass(Type type)
        {
            return type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<FunctionNameAttribute>() != null)
                .Select(m => new CallbackFunction(m))
                .ToArray();
        }

        public static CallbackFunction From<T1>(Func<T1> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2>(Func<T1, T2> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3>(Func<T1, T2, T3> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4>(Func<T1, T2, T3, T4> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1>(Action<T1> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2>(Action<T1, T2> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3>(Action<T1, T2, T3> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4>(Action<T1, T2, T3, T4> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

        public static CallbackFunction From<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> callback, string name = null)
        {
            return new CallbackFunction(callback.Method, name);
        }

    }

}
