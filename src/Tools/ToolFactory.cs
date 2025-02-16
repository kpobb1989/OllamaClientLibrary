using OllamaClientLibrary.Dto.ChatCompletion.Tools.Request;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace OllamaClientLibrary.Tools
{
    public static class ToolFactory
    {
        public static object? Invoke(Tool tool, Dictionary<string, object?>? arguments)
        {
            if (tool.MethodInfo == null)
            {
                return default;
            }

            var parameters = new List<object>();
            var methodParams = tool.MethodInfo.GetParameters();
            foreach (var param in methodParams)
            {
                if (arguments != null && arguments.TryGetValue(param.Name, out var value) && value != null)
                {
                    if (param.ParameterType.IsEnum)
                    {
                        parameters.Add(Enum.Parse(param.ParameterType, value.ToString(), true));
                    }
                    else
                    {
                        parameters.Add(Convert.ChangeType(value, param.ParameterType));
                    }
                }
            }

            return tool.MethodInfo.Invoke(tool.Instance, parameters.ToArray());
        }

        public static Tool Create(object instance, string methodName)
        {
            var methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return CreateInternal(methodInfo, instance);
        }

        public static Tool Create<TClass>(string methodName)
        {
            var methodInfo = typeof(TClass).GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return CreateInternal(methodInfo);
        }

        private static Tool CreateInternal(MethodInfo methodInfo, object? instance = null)
        {
            if (!methodInfo.IsStatic && instance == null)
            {
                var constructor = methodInfo.DeclaringType.GetConstructor(Type.EmptyTypes);

                if (constructor == null)
                {
                    throw new InvalidOperationException("The class must have a parameterless constructor.");
                }

                instance = Activator.CreateInstance(methodInfo.DeclaringType);
            }

            var tool = new Tool
            {
                MethodInfo = methodInfo,
                Instance = instance,
                Function = new Function
                {
                    Name = methodInfo?.Name,
                    Description = GetMethodDescription(methodInfo),
                    Parameters = new Parameter()
                }
            };

            if (methodInfo != null)
            {
                foreach (var parameter in methodInfo.GetParameters())
                {
                    var parameterType = parameter.ParameterType;
                    var parameterName = parameter.Name ?? string.Empty;
                    var parameterDescription = GetParameterDescription(parameter);

                    var property = new Property
                    {
                        Type = GetTypeString(parameterType),
                        Description = parameterDescription
                    };

                    if (parameterType.IsEnum)
                    {
                        property.Enum = Enum.GetNames(parameterType).ToList();
                    }

                    tool.Function.Parameters.Properties[parameterName] = property;
                    tool.Function.Parameters.Required.Add(parameterName);
                }
            }

            return tool;

        }

        private static string GetTypeString(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type.IsEnum)
                return "string"; // Enums are represented as strings

            return "object"; // Default to object for complex types
        }

        private static string GetMethodDescription(MethodInfo? methodInfo)
            => methodInfo?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? $"Description for {methodInfo?.Name}";

        private static string GetParameterDescription(ParameterInfo parameterInfo)
            => parameterInfo?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? $"Description for {parameterInfo?.Name}";
    }
}
