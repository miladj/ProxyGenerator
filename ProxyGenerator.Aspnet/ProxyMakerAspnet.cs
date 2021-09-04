using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyGenerator.Core
{
    public class ProxyMakerAspnet : ProxyMaker
    {
        private Type _implementType=null;
        private Type _decoratorType = null;
        public ProxyMakerAspnet(Type typeToProxy) : base(typeToProxy, Array.Empty<Type>(), false)
        {
        }

        public ProxyMakerAspnet(Type typeToProxy, Type implementType, Type[] interceptorTypes) : base(typeToProxy, interceptorTypes, true)
        {
            this._implementType = implementType;
            this._decoratorType = null;
        }
        public ProxyMakerAspnet(Type typeToProxy, Type implementType, Type decoratorType) : base(typeToProxy, Array.Empty<Type>(), true)
        {
            this._implementType = implementType;
            this._decoratorType = decoratorType;
            _generateInterceptorPart = false;
        }


        protected override void CreateConstructor()
        {
            if (_implementType == null)
            {
                base.CreateConstructor();
                return;
            }
            if (this._defineGenericParameters !=null  && _defineGenericParameters.Length>0)
            {

                _implementType = _implementType.MakeGenericType(_defineGenericParameters);
                _decoratorType = _decoratorType?.MakeGenericType(_defineGenericParameters);
            }


            Type[] parameterTypes = _decoratorType switch
            {
                null => new[]
                {
                    typeof(IServiceProvider),
                    ReflectionStaticValue.TypeArrayOfIInterceptor
                },
                _ => new[]
                {
                    typeof(IServiceProvider)
                }
            };
                
            
            ConstructorBuilder constructorBuilder = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
            ConstructorInfo activatorUtilityAttributeConstructor = typeof(ActivatorUtilitiesConstructorAttribute).GetConstructors()[0];
            constructorBuilder.SetCustomAttribute(new CustomAttributeBuilder(activatorUtilityAttributeConstructor,Array.Empty<object>()));
            ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.DeclareLocal(_typeToImplement);

            ilGenerator.CallObjectCtorAsBaseCtor();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, _serviceProviderField);

            
            

            ilGenerator.Emit(OpCodes.Ldarg_0);

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldtoken, _implementType);
            ilGenerator.Emit(OpCodes.Call, ReflectionStaticValue.Type_GetTypeFromHandle);
            ilGenerator.Emit(OpCodes.Call, ReflectionStaticValue.Array_Empty_OfObjet);
            ilGenerator.Emit(OpCodes.Call, ReflectionStaticValue.ActivatorCreateInstanceUtilities);
            ilGenerator.Emit(OpCodes.Isinst, _typeToImplement);
            if (_decoratorType != null)
            {
                ilGenerator.Emit(OpCodes.Stloc_0);
                
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldtoken, _decoratorType);
                ilGenerator.Emit(OpCodes.Call, ReflectionStaticValue.Type_GetTypeFromHandle);
                ilGenerator.CreateArray(ReflectionStaticValue.TypeObject,1);
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ilGenerator.Emit(OpCodes.Stelem_Ref);
                ilGenerator.Emit(OpCodes.Call, ReflectionStaticValue.ActivatorCreateInstanceUtilities);
                ilGenerator.Emit(OpCodes.Isinst, _typeToImplement);
            }
            ilGenerator.Emit(OpCodes.Stfld, _fieldBuilder);

            if (_decoratorType == null)
            {
                FillInterceptorFieldWithServiceProvider(ilGenerator);
            }
            

            ilGenerator.Emit(OpCodes.Ret);
        }
    }
}