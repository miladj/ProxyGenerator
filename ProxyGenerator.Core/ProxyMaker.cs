using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ProxyGenerator.Core
{
    public partial class ProxyMaker
    {
        private static class AssemblyMaker
        {
            public static readonly AssemblyBuilder ProxyAssemblyBuilder;
            public static readonly ModuleBuilder ProxyModuleBuilder;

            static AssemblyMaker()
            {
                ProxyAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicProxy"), AssemblyBuilderAccess.Run);
                ProxyModuleBuilder = ProxyAssemblyBuilder.DefineDynamicModule("DynamicModule");
            }
        }
        private readonly Type _typeToProxy;
        
        
        protected readonly Type _typeToImplement;
        protected readonly TypeBuilder _typeBuilder;
        protected readonly FieldBuilder _fieldBuilder;
        protected readonly FieldBuilder _interceptorsField;
        
        protected readonly Type[] _defineGenericParameters;
        private Type[] _genericArguments;
        private ModuleBuilder _dynamicModule= AssemblyMaker.ProxyModuleBuilder;
        
        protected bool IsBaseTypeInterface { get; }

       
        protected ProxyMaker(Type typeToProxy)
        {
            this._typeToProxy = typeToProxy;
            

            this._typeBuilder = _dynamicModule.DefineType($"MyType_{Guid.NewGuid()}", TypeAttributes.Public);
            this._typeToImplement = typeToProxy;
            if (_typeToImplement.IsGenericTypeDefinition)
            {
                _genericArguments = _typeToImplement.GetGenericArguments();
                this._defineGenericParameters = _typeBuilder.DefineGenericParameters(Enumerable.Range(0, _genericArguments.Length).Select(x => "T" + x)
                    .ToArray());
                _typeToImplement = typeToProxy.MakeGenericType(_defineGenericParameters);
            }
            else
            {
                this._defineGenericParameters = Array.Empty<Type>();
            }

            
            this._fieldBuilder = _typeBuilder.DefineField("__backendvariable__", _typeToImplement, FieldAttributes.Private);
            this._interceptorsField = _typeBuilder.DefineField("__backendvariableinterceptors__", ReflectionStaticValue.TypeArrayOfIInterceptor, FieldAttributes.Private);
            
            IsBaseTypeInterface = _typeToImplement.IsInterface;
            if (IsBaseTypeInterface)
            {
                _typeBuilder.AddInterfaceImplementation(_typeToImplement);
                
            }
            else
            {
                _typeBuilder.SetParent(_typeToImplement);
            }
            

            
        }

        
        public Type CreateProxy()
        {
            InternalCreateType();
            CreateConstructor();
            Type proxy = _typeBuilder.CreateType();
            return proxy;
        }

        protected virtual void CreateConstructor()
        {
            ConstructorBuilder constructorBuilder = _typeBuilder.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, new[]
                {
                    _typeToImplement, ReflectionStaticValue.TypeArrayOfIInterceptor
                });

            ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.CallObjectCtorAsBaseCtor();

            ilGenerator.Ldarg(0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, _fieldBuilder);

            ilGenerator.Ldarg(0);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Stfld, _interceptorsField);

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
                GenericTypeParameterBuilder[] methodDefinedGenericArguments =
                    Array.Empty<GenericTypeParameterBuilder>();
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

                }

                methodBuilder.SetParameters(newMethodParameterTypes);
                methodBuilder.SetReturnType(methodInfoReturnType);

                ILGenerator generator = methodBuilder.GetILGenerator();

                generator.DeclareLocal(ReflectionStaticValue.TypeIInvocation);
                Label withoutInterceptor = generator.DefineLabel();
                //we can use a flag to generate this part or not
                //but here we check for null interceptors in generated code
                generator.Ldarg(0);
                generator.Emit(OpCodes.Ldfld, _interceptorsField);
                generator.Emit(OpCodes.Brfalse, withoutInterceptor);

                generator.Ldarg(0);
                generator.Emit(OpCodes.Ldfld, _interceptorsField);
                generator.Emit(OpCodes.Ldlen);
                generator.Ldc_I4(0);
                generator.Emit(OpCodes.Cgt_Un);
                generator.Emit(OpCodes.Brfalse, withoutInterceptor);


                #region interceptor part generator
                Type nestedType = CreateNestedType(methodParameters, methodInfo, out var fbs, out var ctor);
                Type makeGenericType = nestedType;
                Type[] typeArguments = _defineGenericParameters.Concat(methodDefinedGenericArguments).ToArray();
                if (typeArguments.Length > 0)
                    makeGenericType = nestedType.MakeGenericType(typeArguments);

                if (typeArguments.Length > 0)
                    generator.Emit(OpCodes.Newobj, TypeBuilder.GetConstructor(makeGenericType, ctor));
                else
                    generator.Emit(OpCodes.Newobj, ctor);


                generator.Emit(OpCodes.Stloc_0);

                //Set Original
                generator.Emit(OpCodes.Ldloc_0);
                generator.Ldarg(0);
                generator.Emit(OpCodes.Ldfld, _fieldBuilder);
                generator.Emit(OpCodes.Callvirt, ReflectionStaticValue.Invocation_Original_Set);

                

                generator.Emit(OpCodes.Ldloc_0);
                //generator.Emit(OpCodes.Dup);
                //generator.Ldarg(0);
                //generator.Emit(OpCodes.Ldfld, _fieldBuilder);
                //if (typeArguments.Length > 0)
                //    generator.Emit(OpCodes.Stfld, TypeBuilder.GetField(makeGenericType, fbs[0]));
                //else
                //    generator.Emit(OpCodes.Stfld, fbs[0]);
                if (methodParameters.Length > 0)
                {
                    generator.Emit(OpCodes.Dup);

                    for (var i = 1; i <= methodParameters.Length; i++)
                    {
                        generator.Ldarg(i);
                        Type parameterType = newMethodParameterTypes[i-1];
                        if (parameterType.IsByRef)
                            generator.Emit(OpCodes.Ldobj, parameterType.GetElementType());
                        if (typeArguments.Length > 0)
                            generator.Emit(OpCodes.Stfld, TypeBuilder.GetField(makeGenericType, fbs[i]));
                        else
                            generator.Emit(OpCodes.Stfld, fbs[i]);
                        if (methodParameters.Length != i)
                            generator.Emit(OpCodes.Dup);
                    }
                }

                generator.Ldarg(0);
                generator.Emit(OpCodes.Ldfld, _interceptorsField);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Newobj, ReflectionStaticValue.InterceptorHelper_Constructor);
                generator.Emit(OpCodes.Call, ReflectionStaticValue.InterceptorHelper_Intercept);

                if (methodParameters.Length > 0)
                {
                    for (var i = 1; i <= methodParameters.Length; i++)
                    {
                        //break;
                        if(!newMethodParameterTypes[i-1].IsByRef)
                            continue;

                        generator.Ldarg(i);
                        generator.Emit(OpCodes.Ldloc_0);

                        if (typeArguments.Length > 0)
                        {
                            generator.Emit(OpCodes.Ldfld, TypeBuilder.GetField(makeGenericType, fbs[i]));
                            
                        }
                        else
                        {
                            generator.Emit(OpCodes.Ldfld, fbs[i]);
                        }
                        generator.Emit(OpCodes.Stobj, newMethodParameterTypes[i - 1].GetElementType());
                    }
                }

                if (methodInfo.ReturnType == ReflectionStaticValue.TypeVoid)
                    generator.Emit(OpCodes.Pop);
                else
                    generator.Emit(methodInfo.ReturnType.NeedUnboxing() ? OpCodes.Unbox_Any : OpCodes.Castclass, methodInfo.ReturnType);
                generator.Emit(OpCodes.Ret);



                generator.MarkLabel(withoutInterceptor);
                #endregion

                generator.Ldarg(0);

                generator.Emit(OpCodes.Ldfld, _fieldBuilder);

                for (var i = 1; i <= methodParameters.Length; i++)
                {
                    generator.Ldarg(i);
                }

                generator.Emit(OpCodes.Callvirt, methodInfo);


                generator.Emit(OpCodes.Ret);

            }
        }

        
        private Type CreateNestedType(ParameterInfo[] methodParameters2, MethodInfo methodInfo, out FieldInfo[] fb, out ConstructorBuilder defineDefaultConstructor)
        {
            TypeBuilder nestedType = _dynamicModule.DefineType($"MT__{methodInfo.Name}_{Guid.NewGuid()}",  TypeAttributes.Public);

            Type targetFieldType = _typeToProxy;

            Type[] parametersType = methodParameters2.Select(x=>x.ParameterType).ToArray();
            List<Type> methodGenericArgument = new List<Type>();
            if (_genericArguments != null && _genericArguments.Length > 0)
            {
                methodGenericArgument.AddRange(_genericArguments);
                
            }
            Type methodInfoReturnType = methodInfo.ReturnType;
            if (methodInfo.IsGenericMethod)
            {
                Type[] methodGenericArguments = methodInfo.GetGenericArguments();
                methodGenericArgument.AddRange(methodGenericArguments);
            
            }

            if (methodGenericArgument.Count > 0)
            {
                var genericTypeParameterBuilders = nestedType.DefineGenericParameters(methodGenericArgument.Select((x, i) => "K" + i).ToArray());

                Type GetNewType(Type oldType)
                {
                    if (oldType.IsByRef)
                        oldType = oldType.GetElementType();
                    var indexOf = methodGenericArgument.FindIndex(x => x == oldType);
                    if (indexOf >= 0)
                        return genericTypeParameterBuilders[indexOf];
                    return oldType;
                }

                
                for (var i = 0; i < parametersType.Length; i++)
                {
                    Type oldType = parametersType[i];
                    parametersType[i] = GetNewType(oldType);
                    if (oldType.IsByRef)
                        parametersType[i] = parametersType[i].MakeByRefType();
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
                if (_typeToProxy.IsGenericTypeDefinition)
                {
                    Type[] genericArguments = targetFieldType.GetGenericArguments();
                    for (var i = 0; i < genericArguments.Length; i++)
                    {
                        genericArguments[i] = GetNewType(genericArguments[i]);
                    }

                    targetFieldType = targetFieldType.MakeGenericType(genericArguments);
                }
                
                if (methodInfoReturnType.IsGenericParameter)
                {
                    methodInfoReturnType = GetNewType(methodInfoReturnType);
                }
                if (methodInfoReturnType.IsGenericType)
                {
                    Type[] genericArguments = methodInfoReturnType.GetGenericArguments();
                    for (var i = 0; i < genericArguments.Length; i++)
                    {
                        genericArguments[i] = GetNewType(genericArguments[i]);
                    }
                    methodInfoReturnType = methodInfoReturnType.GetGenericTypeDefinition().MakeGenericType(genericArguments);
                }
            }
            
            fb = new FieldInfo[parametersType.Length + 1];
            
            

            // fb[0] = defineNestedType.DefineField("_target2", targetFieldType, FieldAttributes.Public);
            fb[0] = ReflectionStaticValue.TypeInvocation.GetField("_target",
                BindingFlags.Instance | BindingFlags.NonPublic);
            for (var i = 1; i <= parametersType.Length; i++)
            {
                Type fieldType = parametersType[i - 1];
                if(fieldType.IsByRef)
                    fieldType = fieldType.GetElementType();
                FieldBuilder fieldBuilder = nestedType.DefineField("__fl" + i, fieldType,
                    FieldAttributes.Public);
                fb[i] = fieldBuilder;
            }
            
            defineDefaultConstructor =
                nestedType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
            ILGenerator constructorIlGenerator = defineDefaultConstructor.GetILGenerator();
            constructorIlGenerator.Ldarg(0);
            constructorIlGenerator.Ldc_I4(parametersType.Length);
            constructorIlGenerator.Emit(OpCodes.Call,ReflectionStaticValue.Invocation_Constructor);
            constructorIlGenerator.Emit(OpCodes.Ret);

            nestedType.SetParent(ReflectionStaticValue.TypeInvocation);
            FieldBuilder methodInfoStaticField = nestedType.DefineField("__methodInfo", ReflectionStaticValue.MethodInfoType,
                FieldAttributes.Static | FieldAttributes.Private);
            
            ConstructorBuilder staticConstructor = nestedType.DefineTypeInitializer();
            ILGenerator staticCtorIL = staticConstructor.GetILGenerator();

            staticCtorIL.Emit(OpCodes.Ldtoken, methodInfo);
            staticCtorIL.Emit(OpCodes.Ldtoken, _typeToImplement);
            staticCtorIL.Emit(OpCodes.Call, ReflectionStaticValue.MethodBase_GetMethodFromHandle);
            staticCtorIL.Emit(OpCodes.Castclass, ReflectionStaticValue.MethodInfoType);
            staticCtorIL.Emit(OpCodes.Stsfld, methodInfoStaticField);

            staticCtorIL.Emit(OpCodes.Ret);

            #region Create Invocation.Method Property
            MethodBuilder implementGetForMethod = nestedType.DefineMethod(ReflectionStaticValue.Invocation_Method_Get.Name,MethodAttributes.Public|MethodAttributes.Virtual);
            implementGetForMethod.SetReturnType(ReflectionStaticValue.MethodInfoType);
            ILGenerator getMethodIL = implementGetForMethod.GetILGenerator();
            getMethodIL.Emit(OpCodes.Ldsfld,methodInfoStaticField);
            getMethodIL.Emit(OpCodes.Ret);
            #endregion
            
            MethodBuilder defineMethod =
                nestedType.DefineMethod(nameof(IDefaultInvocation.Invoke), MethodAttributes.Public | MethodAttributes.Virtual,null,null);
            defineMethod.SetReturnType(ReflectionStaticValue.TypeObject);
            
            ILGenerator ilGenerator = defineMethod.GetILGenerator();
            ilGenerator.DeclareLocal(ReflectionStaticValue.TypeObject);

            ilGenerator.Ldarg(0);
            ilGenerator.Emit(OpCodes.Ldfld, fb[0]);
            ilGenerator.Emit(OpCodes.Castclass,targetFieldType);
            
            
            for (var i = 1; i <= parametersType.Length; i++)
            {
                Type type = parametersType[i - 1];
                ilGenerator.Ldarg(0);
                ilGenerator.Emit(type.IsByRef? OpCodes.Ldflda:OpCodes.Ldfld, fb[i]);
                
            }
            
            ilGenerator.Emit(OpCodes.Callvirt, methodInfo);
            
            if (methodInfo.ReturnType == ReflectionStaticValue.TypeVoid)
                ilGenerator.Emit(OpCodes.Ldnull);
            else if(methodInfo.ReturnType.NeedUnboxing())
            {
                
                ilGenerator.Emit( OpCodes.Box, methodInfoReturnType);
            }

            ilGenerator.Emit(OpCodes.Ret);

            #region Create Invocation.GetArgument Method
            MethodBuilder implementForGetArgumentMethod = nestedType.DefineMethod(ReflectionStaticValue.Invocation_InternalGetArgument_Method.Name, MethodAttributes.Public | MethodAttributes.Virtual);
            implementForGetArgumentMethod.SetParameters(ReflectionStaticValue.Invocation_InternalGetArgument_Method.GetParameters().Select(x => x.ParameterType).ToArray());
            implementForGetArgumentMethod.SetReturnType(ReflectionStaticValue.Invocation_InternalGetArgument_Method.ReturnType);
            ILGenerator getArgumentMethodIl = implementForGetArgumentMethod.GetILGenerator();


            for (var i = 1; i <= parametersType.Length; i++)
            {
                var fieldInfo = fb[i];
                var fieldType = fieldInfo.FieldType;
                Label nextIfLabel = getArgumentMethodIl.DefineLabel();
                getArgumentMethodIl.Ldarg(1);
                getArgumentMethodIl.Ldc_I4(i - 1);
                getArgumentMethodIl.Emit(OpCodes.Bne_Un, nextIfLabel);
                getArgumentMethodIl.Ldarg(0);
                getArgumentMethodIl.Emit(OpCodes.Ldfld, fieldInfo);
                if(fieldType.NeedUnboxing())
                    getArgumentMethodIl.Emit(OpCodes.Box, fieldType);
                getArgumentMethodIl.Emit(OpCodes.Ret);
                getArgumentMethodIl.MarkLabel(nextIfLabel);
            }
            getArgumentMethodIl.Emit(OpCodes.Ldnull);
            getArgumentMethodIl.Emit(OpCodes.Ret);
            #endregion

            #region Create Invocation.SetArgument Method
            MethodBuilder implementForSetArgumentMethod = nestedType.DefineMethod(ReflectionStaticValue.Invocation_InternalSetArgument_Method.Name, MethodAttributes.Public | MethodAttributes.Virtual);
            implementForSetArgumentMethod.SetParameters(ReflectionStaticValue.Invocation_InternalSetArgument_Method.GetParameters().Select(x=>x.ParameterType).ToArray());
            ILGenerator setArgumentMethodIl = implementForSetArgumentMethod.GetILGenerator();
            
            
            for (var i = 1; i <= parametersType.Length; i++)
            {
                var fieldInfo = fb[i];
                var fieldType= fieldInfo.FieldType;
                Label nextIfLabel = setArgumentMethodIl.DefineLabel();
                setArgumentMethodIl.Ldarg(1);
                setArgumentMethodIl.Ldc_I4(i-1);
                setArgumentMethodIl.Emit(OpCodes.Bne_Un, nextIfLabel);
                setArgumentMethodIl.Ldarg(0);
                setArgumentMethodIl.Ldarg(2);
                setArgumentMethodIl.Emit(fieldType.NeedUnboxing() ? OpCodes.Unbox_Any : OpCodes.Castclass, fieldType);
                setArgumentMethodIl.Emit(OpCodes.Stfld, fieldInfo);
                setArgumentMethodIl.Emit(OpCodes.Ret);
                setArgumentMethodIl.MarkLabel(nextIfLabel);
            }
            setArgumentMethodIl.Emit(OpCodes.Ret);
            #endregion


            

            nestedType.CreateType();
            
            return nestedType;
            
        }
    }
}