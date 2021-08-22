﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ProxyGenerator.Core
{
    public class ProxyMaker
    {
        private readonly Type _interfaceType;
        private readonly Type _interceptor;
        private readonly Type _typeToImplement;
        private readonly TypeBuilder _typeBuilder;
        private readonly FieldBuilder _fieldBuilder;
        private readonly GenericTypeParameterBuilder[] _defineGenericParameters;


        public ProxyMaker(Type interfaceType, Type interceptor = null)
        {
            this._interfaceType = interfaceType;
            this._interceptor = interceptor;

            AssemblyBuilder defineDynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("salam"), AssemblyBuilderAccess.Run);
            ModuleBuilder defineDynamicModule = defineDynamicAssembly.DefineDynamicModule("MyModule");

            //Type interfaceType = typeof(IInterface);

            this._typeBuilder = defineDynamicModule.DefineType("MyType", TypeAttributes.Public);
            this._typeToImplement = interfaceType;
            if (_typeToImplement.IsGenericType)
            {
                Type[] genericArguments = _typeToImplement.GetGenericArguments();
                this._defineGenericParameters = _typeBuilder.DefineGenericParameters(Enumerable.Range(0, genericArguments.Length).Select(x => "T" + x)
                    .ToArray());
                _typeToImplement = interfaceType.MakeGenericType(_defineGenericParameters);
            }
            this._fieldBuilder = _typeBuilder.DefineField("__backendvariable__", _typeToImplement, FieldAttributes.Private);
            InternalCreateType();
        }
        public Type CreateProxy()
        {

            ConstructorBuilder constructorBuilder = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[]
            {
                _typeToImplement
            });
            ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call,
                typeof(object).GetConstructor(Type.EmptyTypes)!);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, _fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);



            return _typeBuilder.CreateType();

        }

        private void InternalCreateType()
        {
            _typeBuilder.AddInterfaceImplementation(_typeToImplement);

            MethodInfo[] methodInfos = _interfaceType.GetMethods().Concat(_interfaceType.GetProperties().Select(x => x.GetMethod).Concat(_interfaceType.GetProperties().Select(x => x.SetMethod))).ToArray();
            foreach (MethodInfo methodInfo in methodInfos)
            {
                ParameterInfo[] methodParameters = methodInfo.GetParameters();
                MethodBuilder methodBuilder =
                    _typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual);
                Type[] parameterTypes = methodParameters.Select(x => x.ParameterType).ToArray();
                Type methodInfoReturnType = methodInfo.ReturnType;
                if (methodInfo.IsGenericMethod)
                {
                    Type[] genericArguments = methodInfo.GetGenericArguments();
                    GenericTypeParameterBuilder[] genericTypeParameterBuilders = methodBuilder.DefineGenericParameters(
                        Enumerable.Range(0, genericArguments.Length)
                            .Select(x => "M" + x)
                            .ToArray());
                    int indexOf = Array.IndexOf(genericArguments, methodInfoReturnType);
                    if (indexOf >= 0)
                    {
                        methodInfoReturnType = genericTypeParameterBuilders[indexOf];
                    }

                    for (var index = 0; index < parameterTypes.Length; index++)
                    {
                        Type parameterType = parameterTypes[index];
                        int paramIndex = Array.IndexOf(genericArguments, parameterType);
                        if (paramIndex >= 0)
                        {
                            parameterTypes[index] = genericArguments[paramIndex];
                        }
                    }
                }

                methodBuilder.SetParameters(parameterTypes);
                methodBuilder.SetReturnType(methodInfoReturnType);
                ILGenerator generator = methodBuilder.GetILGenerator();
                generator.DeclareLocal(typeof(IInvocation));
                generator.DeclareLocal(typeof(IInterceptor));
                if (methodInfoReturnType != typeof(void))
                {
                    generator.DeclareLocal(methodInfoReturnType);
                }

                if (_interceptor != null)
                {
                    //
                    ConstructorInfo constructorInfo = typeof(Invocation).GetConstructors()[0];

                    generator.Emit(OpCodes.Newobj, constructorInfo);
                    generator.Emit(OpCodes.Stloc_0);
                    generator.Emit(OpCodes.Ldloc_0);
                    //generator.Emit(OpCodes.Call,typeof(Console).GetMethod("WriteLine",new Type[]{typeof(object)}));


                    if (parameterTypes.Length > 0)
                    {
                        //Fill Object array
                        generator.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                        generator.Emit(OpCodes.Newarr, typeof(object));
                        generator.Emit(OpCodes.Dup);


                        for (var i = 1; i <= methodParameters.Length; i++)
                        {
                            generator.Emit(OpCodes.Ldc_I4_S, i - 1);
                            //generator.Emit(OpCodes.Ldarg, i);
                            switch (i)
                            {
                                case 1:
                                    generator.Emit(OpCodes.Ldarg_1);
                                    break;
                                case 2:
                                    generator.Emit(OpCodes.Ldarg_2);
                                    break;
                                case 3:
                                    generator.Emit(OpCodes.Ldarg_3);
                                    break;
                                default:
                                    generator.Emit(OpCodes.Ldarg, i);
                                    break;
                            }

                            Type parameterType = methodParameters[i - 1].ParameterType;
                            if (parameterType.IsValueType)
                            {
                                generator.Emit(OpCodes.Box, parameterType);
                            }

                            generator.Emit(OpCodes.Stelem_Ref);
                            if (methodParameters.Length != i)
                                generator.Emit(OpCodes.Dup);
                        }
                    }

                    generator.Emit(OpCodes.Callvirt, typeof(IInvocation).GetProperty("Arguments").SetMethod);

                    generator.Emit(OpCodes.Newobj, _interceptor.GetConstructors()[0]);
                    generator.Emit(OpCodes.Stloc_1);

                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Callvirt, typeof(IInterceptor).GetMethods()[0]);
                }


                generator.Emit(OpCodes.Ldarg_0);

                generator.Emit(OpCodes.Ldfld, _fieldBuilder);

                for (var i = 1; i <= methodParameters.Length; i++)
                {
                    if (i <= 3)
                    {
                        generator.Emit(i switch
                        {
                            1 => OpCodes.Ldarg_1,
                            2 => OpCodes.Ldarg_2,
                            3 => OpCodes.Ldarg_3
                        });
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldarg, i);
                    }
                }

                generator.Emit(OpCodes.Callvirt, methodInfo);
                if (methodInfoReturnType != typeof(void))
                {
                    generator.Emit(OpCodes.Stloc_2);
                    generator.Emit(OpCodes.Ldloc_2);
                }

                generator.Emit(OpCodes.Ret);
            }
        }

       

    }
}