using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace ProxyGenerator.Core
{
    public class ProxyMaker
    {
        private readonly Type _typeToProxy;
        private readonly Type[] _interceptorTypes;
        private readonly bool _useServiceProvider;
        protected readonly Type _typeToImplement;
        protected readonly TypeBuilder _typeBuilder;
        protected readonly FieldBuilder _fieldBuilder;
        protected readonly FieldBuilder _interceptorsField;
        private readonly FieldBuilder _serviceProviderField;
        protected readonly GenericTypeParameterBuilder[] _defineGenericParameters;
        private Type[] _genericArguments;
        private ModuleBuilder _dynamicModule;
        private AssemblyBuilder _defineDynamicAssembly;
        
        protected bool IsBaseTypeInterface { get; }

        public ProxyMaker(Type typeToProxy, Type[] interceptorTypes ):this(typeToProxy,interceptorTypes,true)
        {

        }
        public ProxyMaker(Type typeToProxy) : this(typeToProxy,null,  false)
        {

        }
        protected ProxyMaker(Type typeToProxy, Type[] interceptorTypes = null, bool useServiceProvider = false)
        {
            this._typeToProxy = typeToProxy;
            this._interceptorTypes = interceptorTypes;
            _useServiceProvider = useServiceProvider;
            

            _defineDynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("salam"), AssemblyBuilderAccess.Run);
            ModuleBuilder defineDynamicModule = _defineDynamicAssembly.DefineDynamicModule("MyModule");


            _dynamicModule = defineDynamicModule;
            this._typeBuilder = _dynamicModule.DefineType("MyType", TypeAttributes.Public);
            this._typeToImplement = typeToProxy;
            if (_typeToImplement.IsGenericType)
            {
                _genericArguments = _typeToImplement.GetGenericArguments();
                this._defineGenericParameters = _typeBuilder.DefineGenericParameters(Enumerable.Range(0, _genericArguments.Length).Select(x => "T" + x)
                    .ToArray());
                _typeToImplement = typeToProxy.MakeGenericType(_defineGenericParameters);
            }
            else
            {
                this._defineGenericParameters = new GenericTypeParameterBuilder[0];
            }

            
            this._fieldBuilder = _typeBuilder.DefineField("__backendvariable__", _typeToImplement, FieldAttributes.Private);
            this._interceptorsField = _typeBuilder.DefineField("__backendvariableinterceptors__", ReflectionStaticValue.TypeArrayOfIInterceptor, FieldAttributes.Private);
            if (useServiceProvider)
            {
                _serviceProviderField = _typeBuilder.DefineField("___Iserviceprovider", ReflectionStaticValue.TypeIServiceProvider, FieldAttributes.Private);
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
            // if (!_useServiceProvider)
            //     ilGenerator.Emit(OpCodes.Newobj, type.GetConstructors()[0]);
            // else
            {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld,_serviceProviderField);
                ilGenerator.Emit(OpCodes.Ldtoken,type);
                ilGenerator.Emit(OpCodes.Call,ReflectionStaticValue.Type_GetTypeFromHandle);
                ilGenerator.Emit(OpCodes.Callvirt,ReflectionStaticValue.IServiceProvider_GetService);
                ilGenerator.Emit(OpCodes.Isinst,type);
            }
        }
        public Type CreateProxy()
        {
            CreateConstructor();
            Type proxy = _typeBuilder.CreateType();
            
            var gr = new Lokad.ILPack.AssemblyGenerator();
            
            gr.GenerateAssembly(_defineDynamicAssembly,new Assembly[]{ _defineDynamicAssembly1 }, "a.dll");
            // gr.GenerateAssembly(_defineDynamicAssembly1,new Assembly[]{typeof(IDefaultInvocation).Assembly}, "b.dll");

            
            return proxy;
        }

        protected virtual void CreateConstructor()
        {
            if (_serviceProviderField != null)
            {
                ConstructorBuilder constructorBuilder = _typeBuilder.DefineConstructor(MethodAttributes.Public,
                    CallingConventions.Standard, new[]
                    {
                        ReflectionStaticValue.TypeIServiceProvider
                    });
                ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, ReflectionStaticValue.Object_Constructor);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Stfld, _serviceProviderField);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                GenerateNewObj(ilGenerator, _typeToImplement);
                ilGenerator.Emit(OpCodes.Stfld, _fieldBuilder);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldc_I4,_interceptorTypes.Length);
                ilGenerator.Emit(OpCodes.Newarr,ReflectionStaticValue.TypeIInterceptor);
                ilGenerator.Emit(OpCodes.Dup);
                for (var index = 0; index < _interceptorTypes.Length; index++)
                {
                    Type type = _interceptorTypes[index];
                    ilGenerator.Emit(OpCodes.Ldc_I4,index);
                    GenerateNewObj(ilGenerator,type);
                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                    if(index!= _interceptorTypes.Length-1)
                        ilGenerator.Emit(OpCodes.Dup);
                }
                
                ilGenerator.Emit(OpCodes.Stfld, _interceptorsField);

                ilGenerator.Emit(OpCodes.Ret);
            }
            else
            {
                ConstructorBuilder constructorBuilder = _typeBuilder.DefineConstructor(MethodAttributes.Public,
                    CallingConventions.Standard, new[]
                    {
                        _typeToImplement, ReflectionStaticValue.TypeArrayOfIInterceptor
                    });
                
                ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, ReflectionStaticValue.Object_Constructor);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Stfld, _fieldBuilder);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Stfld, _interceptorsField);

                ilGenerator.Emit(OpCodes.Ret);

                
            }
        }

        private static IEnumerable<MethodInfo> GetMethods(Type t)
        {
            return t.GetMethods().Concat(t.GetInterfaces().SelectMany(x=>x.GetMethods()));
        }
        private void InternalCreateType()
        {
            IEnumerable<MethodInfo> methodInfos = IsBaseTypeInterface switch
            {
                true => GetMethods(_typeToProxy)
                    .Concat(_typeToProxy.GetProperties().SelectMany(x => new[] {x.GetMethod, x.SetMethod})),
                false => _typeToProxy.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Where(x=>x.IsAbstract || x.IsVirtual )
            };


            foreach (MethodInfo methodInfo in methodInfos)
            {
                
                ParameterInfo[] methodParameters = methodInfo.GetParameters();
                MethodBuilder methodBuilder = _typeBuilder.DefineMethod(methodInfo.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual);


                Type[] newMethodParameterTypes = methodParameters.Select(x => x.ParameterType).ToArray();
                Type methodInfoReturnType = methodInfo.ReturnType;
                GenericTypeParameterBuilder[] methodDefinedGenericArguments = new GenericTypeParameterBuilder[0];
                if (methodInfo.IsGenericMethod)
                {
                    var methodGenericArgumentsType = methodInfo.GetGenericArguments();
                    methodDefinedGenericArguments = methodBuilder.DefineGenericParameters(
                        Enumerable.Range(0, methodGenericArgumentsType.Length)
                            .Select(x => "M" + x)
                            .ToArray());
                    int indexOf = Array.IndexOf(methodGenericArgumentsType, methodInfoReturnType);
                    if (indexOf >= 0)
                    {
                        methodInfoReturnType = methodDefinedGenericArguments[indexOf];
                    }

                    for (var index = 0; index < newMethodParameterTypes.Length; index++)
                    {
                        Type parameterType = newMethodParameterTypes[index];
                        int paramIndex = Array.IndexOf(methodGenericArgumentsType, parameterType);
                        if (paramIndex >= 0)
                        {
                            newMethodParameterTypes[index] = methodGenericArgumentsType[paramIndex];
                        }
                    }

                    // methodInfo=methodInfo.MakeGenericMethod(genericTypeParameterBuilders);

                }

                methodBuilder.SetParameters(newMethodParameterTypes);
                methodBuilder.SetReturnType(methodInfoReturnType);

                ILGenerator generator = methodBuilder.GetILGenerator();
                // generator.EmitWriteLine("inja 0");
                generator.DeclareLocal(ReflectionStaticValue.TypeIInvocation);
                Label withoutInterceptor = generator.DefineLabel();

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, _interceptorsField);
                generator.Emit(OpCodes.Ldlen);
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Cgt_Un);
                generator.Emit(OpCodes.Brfalse, withoutInterceptor);

                // generator.EmitWriteLine("Interceptor");
                ConstructorInfo constructorInfo = ReflectionStaticValue.Invocation_Constructor;

                generator.Emit(OpCodes.Newobj, constructorInfo);
                generator.Emit(OpCodes.Stloc_0);
                generator.Emit(OpCodes.Ldloc_0);

                // generator.EmitWriteLine("Interceptor 0");
                //Set Arguments
                generator.Emit(OpCodes.Ldc_I4, newMethodParameterTypes.Length);
                generator.Emit(OpCodes.Newarr, ReflectionStaticValue.TypeObject);
                if (newMethodParameterTypes.Length > 0)
                {
                    generator.Emit(OpCodes.Dup);
                    for (var i = 1; i <= newMethodParameterTypes.Length; i++)
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

                        Type parameterType = newMethodParameterTypes[i - 1];
                        if (parameterType.IsValueType )
                        {
                            generator.Emit(OpCodes.Box, parameterType);
                        }
                        if (parameterType.IsGenericParameter)
                        {
                            generator.Emit(OpCodes.Box, parameterType);
                        }
                        generator.Emit(OpCodes.Stelem_Ref);
                        if (methodParameters.Length != i)
                            generator.Emit(OpCodes.Dup);
                    }
                }

                generator.Emit(OpCodes.Callvirt, ReflectionStaticValue.Invocation_Arguments_Set);
                // generator.EmitWriteLine("Interceptor -1");

                //Set Original
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, _fieldBuilder);
                generator.Emit(OpCodes.Callvirt, ReflectionStaticValue.Invocation_Original_Set);

                //Set Method
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Ldtoken, methodInfo);
                generator.Emit(OpCodes.Ldtoken, _typeToImplement);
                generator.Emit(OpCodes.Call, ReflectionStaticValue.MethodBase_GetMethodFromHandle);
                generator.Emit(OpCodes.Castclass, ReflectionStaticValue.MethodInfoType);
                generator.Emit(OpCodes.Callvirt, ReflectionStaticValue.Invocation_Method_Set);
                // generator.Emit(OpCodes.Pop);
                //Set Other Field
                //TODO:Bottleneck
                generator.Emit(OpCodes.Call, ReflectionStaticValue.ProxyHelperMethods_FillInvocationProperties);


                Type? nestedType = CreateNestedType(methodParameters, methodInfo, out var fbs, out var ctor);
                Type makeGenericType = nestedType;
                 Type[] typeArguments = _defineGenericParameters.Concat(methodDefinedGenericArguments).ToArray();
                 if(typeArguments.Length>0)
                     makeGenericType = nestedType.MakeGenericType(typeArguments);
                // ConstructorInfo[] constructorInfos = makeGenericType.GetConstructors();
                 // generator.EmitWriteLine("CreateNestedType");

                if (typeArguments.Length > 0)
                    generator.Emit(OpCodes.Newobj, TypeBuilder.GetConstructor(makeGenericType, ctor));
                else
                    generator.Emit(OpCodes.Newobj, ctor);
                
                generator.Emit(OpCodes.Dup);
                //                generator.Emit(OpCodes.Call, typeof(ProxyHelperMethods).GetMethod("GGG"));
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, _fieldBuilder);
                if (typeArguments.Length > 0)
                    generator.Emit(OpCodes.Stfld, TypeBuilder.GetField(makeGenericType, fbs[0]));
                else
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
                        if (typeArguments.Length > 0)
                            generator.Emit(OpCodes.Stfld, TypeBuilder.GetField(makeGenericType, fbs[i]));
                        else
                            generator.Emit(OpCodes.Stfld, fbs[i]);
                        if (methodParameters.Length != i)
                            generator.Emit(OpCodes.Dup);
                    }
                }

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld,_interceptorsField);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Newobj, ReflectionStaticValue.InterceptorHelper_Constructor);
                generator.Emit(OpCodes.Call, ReflectionStaticValue.InterceptorHelper_Intercept);
                
                if (methodInfo.ReturnType == ReflectionStaticValue.TypeVoid)
                    generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Ret);
                


                generator.MarkLabel(withoutInterceptor);
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
                

                generator.Emit(OpCodes.Ret);

            }
        }

        private AssemblyBuilder _defineDynamicAssembly1;
        private ModuleBuilder defineDynamicModule;
        private Type? CreateNestedType(ParameterInfo[] methodParameters2, MethodInfo methodInfo, out FieldBuilder[] fb, out ConstructorBuilder defineDefaultConstructor)
        {
            if (_defineDynamicAssembly1 == null)
            {
                _defineDynamicAssembly1 =
                    AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("salam2123" ),
                        AssemblyBuilderAccess.Run);
                defineDynamicModule = _defineDynamicAssembly1.DefineDynamicModule("MyModule12323" + Guid.NewGuid());
            }

            
            TypeBuilder defineNestedType = defineDynamicModule.DefineType($"MT__{methodInfo.Name}_{Guid.NewGuid()}",  TypeAttributes.Public);

            Type targetFieldType = _typeToProxy;

            Type[] parametersType = methodParameters2.Select(x=>x.ParameterType).ToArray();
            List<Type> genericArgument = new List<Type>();
            if (_genericArguments != null && _genericArguments.Length > 0)
            {
                genericArgument.AddRange(_genericArguments);
                
            }
            
            if (methodInfo.IsGenericMethod)
            {
                Type[] array2 = methodInfo.GetGenericArguments();
                //genericArgument.AddRange(parametersType.Where(x=>Array.IndexOf(array,x)>-1));
                genericArgument.AddRange(array2);
            
            }

            if (genericArgument.Count > 0)
            {
                var genericTypeParameterBuilders = defineNestedType.DefineGenericParameters(genericArgument.Select((x, i) => "K" + i).ToArray());

                Type GetNewType(Type oldType)
                {
                    
                    var indexOf = genericArgument.FindIndex(x => x == oldType);
                    if (indexOf >= 0)
                        return genericTypeParameterBuilders[indexOf];
                    return oldType;
                }

                
                for (var i = 0; i < parametersType.Length; i++)
                {
                    parametersType[i] = GetNewType(parametersType[i]);
                }
            
                
                if (methodInfo.IsGenericMethod)
                {
                    Type[] array = methodInfo.GetGenericArguments();
                    for (var i = 0; i < array.Length; i++)
                    {
                        array[i] = GetNewType(array[i]);
                    }
            
                    if (array.Length > 0)
                        methodInfo = methodInfo.MakeGenericMethod(array);
                }
                if (_typeToProxy.IsGenericType)
                {
                    Type[] genericArguments = targetFieldType.GetGenericArguments();
                    for (var i = 0; i < genericArguments.Length; i++)
                    {
                        genericArguments[i] = GetNewType(genericArguments[i]);
                    }

                    targetFieldType = targetFieldType.MakeGenericType(genericArguments);
                }
            }
            
            fb = new FieldBuilder[parametersType.Length + 1];
            
            

            fb[0] = defineNestedType.DefineField("_target", targetFieldType, FieldAttributes.Public);
            for (var i = 1; i <= parametersType.Length; i++)
            {
                Type parameterType = parametersType[i - 1];
                
                FieldBuilder fieldBuilder = defineNestedType.DefineField("__fl" + i, parameterType,
                    FieldAttributes.Public);
                fb[i] = fieldBuilder;
            }

            defineDefaultConstructor = defineNestedType.DefineDefaultConstructor(MethodAttributes.Public);
            defineNestedType.AddInterfaceImplementation(ReflectionStaticValue.TypeIDefaultInvocation);

            MethodBuilder defineMethod =
                defineNestedType.DefineMethod(nameof(IDefaultInvocation.Invoke), MethodAttributes.Public | MethodAttributes.Virtual,null,null);
            defineMethod.SetReturnType(ReflectionStaticValue.TypeObject);
            
            ILGenerator ilGenerator = defineMethod.GetILGenerator();
            ilGenerator.DeclareLocal(ReflectionStaticValue.TypeObject);

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fb[0]);
            
            for (var i = 1; i <= parametersType.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, fb[i]);
                
            }
            
            ilGenerator.Emit(OpCodes.Callvirt, methodInfo);
            
            if (methodInfo.ReturnType == ReflectionStaticValue.TypeVoid)
                ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Ret);
            MethodInfo methodInfoDeclaration = ReflectionStaticValue.TypeIDefaultInvocation.GetMethods()[0];
            // defineNestedType.DefineMethodOverride(defineMethod, methodInfoDeclaration); 
            defineNestedType.CreateType();
            var gr = new Lokad.ILPack.AssemblyGenerator();
            gr.GenerateAssembly(_defineDynamicAssembly1, "b"+ methodInfo.Name + ".dll");
            // Type makeGenericType = defineNestedType.MakeGenericType(typeof(string),typeof(string));
            // FieldInfo fieldInfo = TypeBuilder.GetField(makeGenericType,fb[0]);
            return defineNestedType;
            // return defineNestedType;

        }
    }
}