using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ProxyGenerator.Core
{
    public class ProxyMaker
    {
        private readonly Type _interfaceType;
        private readonly Type[] _interceptor;
        private readonly bool _useServiceProvider;
        protected readonly Type _typeToImplement;
        protected readonly TypeBuilder _typeBuilder;
        protected readonly FieldBuilder _fieldBuilder;
        private readonly FieldBuilder _serviceProviderField;
        protected readonly GenericTypeParameterBuilder[] _defineGenericParameters;
        protected bool IsBaseTypeInterface { get; }

        public ProxyMaker(Type interfaceType, Type[] interceptor = null, bool useServiceProvider = false)
        {
            this._interfaceType = interfaceType;
            this._interceptor = interceptor;
            _useServiceProvider = useServiceProvider;
            

            AssemblyBuilder defineDynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("salam"), AssemblyBuilderAccess.Run);
            ModuleBuilder defineDynamicModule = defineDynamicAssembly.DefineDynamicModule("MyModule");

            

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
            if (useServiceProvider)
            {
                _serviceProviderField = _typeBuilder.DefineField("___Iserviceprovider", typeof(IServiceProvider), FieldAttributes.Private);
            }
            IsBaseTypeInterface = _typeToImplement.IsInterface;
            if (IsBaseTypeInterface)
            {
                _typeBuilder.AddInterfaceImplementation(_typeToImplement);
                
            }
            else
            {
                _typeBuilder.SetParent(_typeToImplement);
            }
            

            InternalCreateType();
        }

        private void GenerateNewObj(ILGenerator ilGenerator,Type type)
        {
            
            if (!_useServiceProvider)
                ilGenerator.Emit(OpCodes.Newobj, type.GetConstructors()[0]);
            else
            {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld,_serviceProviderField);
                ilGenerator.Emit(OpCodes.Ldtoken,type);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) }));
                ilGenerator.Emit(OpCodes.Callvirt,typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService)));
                ilGenerator.Emit(OpCodes.Isinst,type);
            }
        }
        public Type CreateProxy()
        {
            CreateConstructor();


            return _typeBuilder.CreateType();
        }

        protected virtual void CreateConstructor()
        {
            ConstructorBuilder constructorBuilder = _typeBuilder.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, new[]
                {
                    _serviceProviderField == null ? _typeToImplement : typeof(IServiceProvider)
                });
            ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call,
                typeof(object).GetConstructor(Type.EmptyTypes)!);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            if (_serviceProviderField != null)
            {
                ilGenerator.Emit(OpCodes.Stfld, _serviceProviderField);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                GenerateNewObj(ilGenerator, _typeToImplement);
            }

            ilGenerator.Emit(OpCodes.Stfld, _fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static IEnumerable<MethodInfo> GetMethods(Type t)
        {
            return t.GetMethods().Concat(t.GetInterfaces().SelectMany(x=>x.GetMethods()));
        }
        private void InternalCreateType()
        {
            IEnumerable<MethodInfo> methodInfos = IsBaseTypeInterface switch
            {
                true => GetMethods(_interfaceType)
                    .Concat(_interfaceType.GetProperties().SelectMany(x => new[] {x.GetMethod, x.SetMethod})),
                false => _interfaceType.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Where(x=>x.IsAbstract || x.IsVirtual )
            };
            

            foreach (MethodInfo methodInfo in methodInfos)
            {
                ParameterInfo[] methodParameters = methodInfo.GetParameters();
                MethodBuilder methodBuilder = _typeBuilder.DefineMethod(methodInfo.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual);
                
                    
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
                if (!IsBaseTypeInterface)
                    _typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
                ILGenerator generator = methodBuilder.GetILGenerator();
                generator.DeclareLocal(typeof(IInvocation));
                generator.DeclareLocal(typeof(IInterceptor[]));
                if (methodInfoReturnType != typeof(void))
                {
                    generator.DeclareLocal(methodInfoReturnType);
                }

                if (_interceptor != null && _interceptor.Length>0)
                {
                    ConstructorInfo constructorInfo = typeof(Invocation).GetConstructors()[0];

                    generator.Emit(OpCodes.Newobj, constructorInfo);
                    generator.Emit(OpCodes.Stloc_0);
                    generator.Emit(OpCodes.Ldloc_0);


                    //Set Arguments
                    generator.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                    generator.Emit(OpCodes.Newarr, typeof(object));
                    if (parameterTypes.Length > 0)
                    {
                        
                        
                        generator.Emit(OpCodes.Dup);


                        for (var i = 1; i <= methodParameters.Length; i++)
                        {
                            generator.Emit(OpCodes.Ldc_I4_S, i - 1);
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

                    generator.Emit(OpCodes.Callvirt, typeof(Invocation).GetProperty(nameof(Invocation.Arguments))!.SetMethod!);


                    //Set Original
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, _fieldBuilder);
                    generator.Emit(OpCodes.Callvirt, typeof(Invocation).GetProperty(nameof(Invocation.Original))!.SetMethod!);

                    //Set Method
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Dup);
                    generator.Emit(OpCodes.Ldtoken,methodInfo);
                    generator.Emit(OpCodes.Call,typeof(MethodBase).GetMethods().FirstOrDefault(x => x.GetParameters().Length == 1 && x.Name == nameof(MethodInfo.GetMethodFromHandle)));
                    generator.Emit(OpCodes.Castclass,typeof(MethodInfo));
                    generator.Emit(OpCodes.Callvirt, typeof(Invocation).GetProperty(nameof(Invocation.Method))!.SetMethod!);
                    
                    //Set Other Field
                    generator.Emit(OpCodes.Call,typeof(ProxyHelperMethods).GetMethod(nameof(ProxyHelperMethods.FillInvocationProperties)));

                    generator.Emit(OpCodes.Ldc_I4_S, _interceptor.Length);
                    generator.Emit(OpCodes.Newarr, typeof(IInterceptor));
                    generator.Emit(OpCodes.Stloc_1);

                    for (var index = 0; index < _interceptor.Length; index++)
                    {
                        Type type = _interceptor[index];
                        generator.Emit(OpCodes.Ldloc_1);
                        generator.Emit(OpCodes.Ldc_I4_S, index);
                        GenerateNewObj(generator,type);
                        generator.Emit(OpCodes.Stelem_Ref);
                    }
                    CreateNestedType(methodParameters, methodInfo, out var fbs, out var ctor);


                    generator.Emit(OpCodes.Newobj, ctor);
                    generator.Emit(OpCodes.Dup);

                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, _fieldBuilder);
                    generator.Emit(OpCodes.Stfld, fbs[0]);
                    if (methodParameters.Length > 0)
                    {
                        generator.Emit(OpCodes.Dup);

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

                            generator.Emit(OpCodes.Stfld, fbs[i]);
                            if (methodParameters.Length != i)
                                generator.Emit(OpCodes.Dup);
                        }
                    }

                    // generator.Emit(OpCodes.Ldnull);
                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Newobj, typeof(InterceptorHelper).GetConstructors()[0]);
                    generator.Emit(OpCodes.Call, typeof(InterceptorHelper).GetMethods()[0]);
                    if (methodInfo.ReturnType == typeof(void))
                        generator.Emit(OpCodes.Pop);
                }
                else
                {
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
                }



                if (methodInfoReturnType != typeof(void))
                {
                    generator.Emit(OpCodes.Stloc_2);
                    generator.Emit(OpCodes.Ldloc_2);
                }

                generator.Emit(OpCodes.Ret);
            }
        }

        private void CreateNestedType(ParameterInfo[] methodParameters, MethodInfo methodInfo, out FieldBuilder[] fb, out ConstructorBuilder defineDefaultConstructor)
        {
            TypeBuilder defineNestedType = _typeBuilder.DefineNestedType($"MM__{methodInfo.Name}_{Guid.NewGuid()}", TypeAttributes.NestedPublic | TypeAttributes.Public);
            defineNestedType.AddInterfaceImplementation(typeof(IDefaultInvocation));

            defineDefaultConstructor = defineNestedType.DefineDefaultConstructor(MethodAttributes.Public);

            fb = new FieldBuilder[methodParameters.Length + 1];
            fb[0] = defineNestedType.DefineField("_target", _interfaceType, FieldAttributes.Public);
            for (var i = 1; i <= methodParameters.Length; i++)
            {
                FieldBuilder fieldBuilder = defineNestedType.DefineField("__fl" + i, methodParameters[i - 1].ParameterType,
                    FieldAttributes.Public);
                fb[i] = fieldBuilder;
            }

            MethodBuilder defineMethod =
                defineNestedType.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual);
            defineMethod.SetReturnType(typeof(object));

            ILGenerator ilGenerator = defineMethod.GetILGenerator();
            ilGenerator.DeclareLocal(typeof(object));

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fb[0]);

            for (var i = 1; i <= methodParameters.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, fb[i]);
            }

            ilGenerator.Emit(OpCodes.Callvirt, methodInfo);
            
            if (methodInfo.ReturnType == typeof(void))
                ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Ret);
            defineNestedType.CreateType();

        }

        

    }
}