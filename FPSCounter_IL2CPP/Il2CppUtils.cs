using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace FPSCounter
{
    internal static class Il2CppUtils
    {
        /// <summary>
        /// Create a dynamic proxy type that inherits from TClass and has the TAttribute applied to it.
        /// Every call creates a completely new type, so cache the result if you need to use it multiple times.
        /// </summary>
        /// <typeparam name="TClass">Any non-sealed and non-static class. It may need to be public.</typeparam>
        /// <typeparam name="TAttribute">An Il2CppSystem.Attribute</typeparam>
        /// <param name="attributeCtorTypes">Types of the desired TAttribute constructor</param>
        /// <param name="attributeCtorParams">Parameters to plug into the desired constructor, must have the same size and parameter types as defined in the type array</param>
        /// <param name="registerToIl2Cpp">Automatically use ClassInjector.RegisterTypeInIl2Cpp on the generated type and on TClass if necessary</param>
        /// <returns>A dynamically created type that inherits TClass and has the TAttribute applied to it by using the specified constructor.</returns>
        public static Type AddIl2CppAttributeToClass<TClass, TAttribute>(Type[] attributeCtorTypes, object[] attributeCtorParams, bool registerToIl2Cpp = true) where TAttribute : Il2CppSystem.Attribute where TClass : class
        {
            if (attributeCtorTypes == null) throw new ArgumentNullException(nameof(attributeCtorTypes));
            if (attributeCtorParams == null) throw new ArgumentNullException(nameof(attributeCtorParams));
            if (attributeCtorParams.Length != attributeCtorTypes.Length) throw new ArgumentException($"{nameof(attributeCtorParams)} and {nameof(attributeCtorTypes)} must have the same length");

            var type = typeof(TClass);

            if (registerToIl2Cpp && !ClassInjector.IsTypeRegisteredInIl2Cpp(type))
                ClassInjector.RegisterTypeInIl2Cpp(type);

            var assName = type.Name + "_Proxy_" + Guid.NewGuid().ToString("N");
            var assNameObj = new AssemblyName(assName);

            var dynamicAss = AssemblyBuilder.DefineDynamicAssembly(assNameObj, AssemblyBuilderAccess.Run);
            var dynamicModule = dynamicAss.DefineDynamicModule(assName);
            var definedType = dynamicModule.DefineType(type.Name + "_Proxy", TypeAttributes.Class, type);

            var attribConstructor = typeof(TAttribute).GetConstructor(AccessTools.all, attributeCtorTypes);
            if (attribConstructor == null) throw new InvalidOperationException("GetConstructor returned null");
            var attributeBuilder = new CustomAttributeBuilder(attribConstructor, attributeCtorParams);
            definedType.SetCustomAttribute(attributeBuilder);

            var newType = definedType.CreateType();
            if (newType == null) throw new InvalidOperationException("CreateType returned null");

            if (registerToIl2Cpp)
                ClassInjector.RegisterTypeInIl2Cpp(newType);

            return newType;
        }
    }
}
