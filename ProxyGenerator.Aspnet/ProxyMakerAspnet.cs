using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using ProxyGenerator.Core;

namespace ProxyGenerator.Aspnet
{
    public partial class ProxyMakerAspnet : ProxyMaker
    {
        private Type _implementType=null;
        private Type _decoratorType = null;
        protected readonly FieldBuilder _serviceProviderField;
        private readonly Type[] _interceptorTypes;
        
        protected ProxyMakerAspnet(Type typeToProxy, Type implementType, Type[] interceptorTypes) : this(typeToProxy, implementType, null, interceptorTypes)
        {
        }

        protected ProxyMakerAspnet(Type typeToProxy, Type implementType, Type decoratorType) : this(typeToProxy,
            implementType, decoratorType, null)
        {

        }

        private ProxyMakerAspnet(Type typeToProxy, Type implementType, Type decoratorType =null, Type[] interceptorTypes = null) : base(typeToProxy)
        {
            if (implementType == null)
            {
                //TODO: throw exception
                throw new NullReferenceException("ImplementType");
            }
            this._implementType = implementType;
            this._decoratorType = decoratorType;
            this._interceptorTypes = interceptorTypes;
            _serviceProviderField = _typeBuilder.DefineField("___Iserviceprovider", ReflectionStaticValue.TypeIServiceProvider, FieldAttributes.Private);
        }

        protected override void CreateConstructor()
        {
            if (this._defineGenericParameters !=null  && _defineGenericParameters.Length>0)
            {

                _implementType = _implementType.MakeGenericType(_defineGenericParameters);
                _decoratorType = _decoratorType?.MakeGenericType(_defineGenericParameters);
            }

            List<Type> lst = new List<Type>();
            lst.Add(typeof(IServiceProvider));
            if (_decoratorType == null)
            {
                lst.AddRange(_interceptorTypes);
            }

            Type[] parameterTypes = lst.ToArray();
                
            
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
        protected void FillInterceptorFieldWithServiceProvider(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.CreateArray(ReflectionStaticValue.TypeIInterceptor, _interceptorTypes.Length);
            ilGenerator.Emit(OpCodes.Dup);
            for (var index = 0; index < _interceptorTypes.Length; index++)
            {
                ilGenerator.Ldc_I4(index);
                ilGenerator.Ldarg(index+2);
                ilGenerator.Emit(OpCodes.Stelem_Ref);
                if (index != _interceptorTypes.Length - 1)
                    ilGenerator.Emit(OpCodes.Dup);
            }

            ilGenerator.Emit(OpCodes.Stfld, _interceptorsField);
        }
    }
}