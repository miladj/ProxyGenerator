using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ProxyGenerator.Core
{
    public class ProxyMaker
    {
        public static Type CreateProxy(Type interfaceType,Type interceptor=null)
        {
            AssemblyBuilder defineDynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("salam"), AssemblyBuilderAccess.Run);
            ModuleBuilder defineDynamicModule = defineDynamicAssembly.DefineDynamicModule("MyModule");

            

            TypeBuilder typeBuilder = defineDynamicModule.DefineType("MyType",TypeAttributes.Public);
            Type typeToImplement = interfaceType;
            if (typeToImplement.IsGenericType)
            {
                Type[] genericArguments = typeToImplement.GetGenericArguments();
                GenericTypeParameterBuilder[] defineGenericParameters = typeBuilder.DefineGenericParameters(Enumerable.Range(0, genericArguments.Length).Select(x => "T" + x)
                    .ToArray());
                typeToImplement = interfaceType.MakeGenericType(defineGenericParameters);
            }
            FieldBuilder fieldBuilder = typeBuilder.DefineField("__backendvariable__", typeToImplement, FieldAttributes.Private);
            
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[]
                {
                    typeToImplement
                });
            ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call,
                typeof(object).GetConstructor(Type.EmptyTypes)!);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.AddInterfaceImplementation(typeToImplement);

            MethodInfo[] methodInfos = interfaceType.GetMethods();
            foreach (MethodInfo methodInfo in methodInfos)
            {
                
                ParameterInfo[] methodParameters = methodInfo.GetParameters();
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual);
                Type[] parameterTypes = methodParameters.Select(x => x.ParameterType).ToArray();
                Type methodInfoReturnType= methodInfo.ReturnType;
                if (methodInfo.IsGenericMethod)
                {
                    Type[] genericArguments = methodInfo.GetGenericArguments();
                    GenericTypeParameterBuilder[] genericTypeParameterBuilders = methodBuilder.DefineGenericParameters(Enumerable.Range(0, genericArguments.Length)
                        .Select(x => "M" + x)
                        .ToArray());
                    int indexOf = Array.IndexOf(genericArguments,methodInfoReturnType);
                    if (indexOf>=0)
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

                if (interceptor != null)
                {
                    
                    //TODO: Implement Interceptors

                }


                generator.Emit(OpCodes.Ldarg_0);

                generator.Emit(OpCodes.Ldfld, fieldBuilder);

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

            return typeBuilder.CreateType();

        }
        

    }
}
