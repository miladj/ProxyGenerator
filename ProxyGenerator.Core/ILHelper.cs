using System;
using System.Reflection.Emit;

namespace ProxyGenerator.Core
{
    public static class ILHelper
    {
        public static void Ldarg(this ILGenerator ilGenerator,int argNum)
        {
            switch (argNum)
            {
                case 0:
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    ilGenerator.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    ilGenerator.Emit(OpCodes.Ldarg, argNum);
                    break;
            }
        }
        public static void CallObjectCtorAsBaseCtor(this ILGenerator ilGenerator)
        {
            ilGenerator.Ldarg(0);
            ilGenerator.Emit(OpCodes.Call, ReflectionStaticValue.Object_Constructor);
        }
        public static void CreateArray(this ILGenerator ilGenerator, Type arrayType, int size)
        {
            ilGenerator.Ldc_I4(size);

            ilGenerator.Emit(OpCodes.Newarr, arrayType);
        }

        public static void Ldc_I4(this ILGenerator ilGenerator, int value)
        {
            switch (value)
            {
                case 0:
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    ilGenerator.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    ilGenerator.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    ilGenerator.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    ilGenerator.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    ilGenerator.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    ilGenerator.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    ilGenerator.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    ilGenerator.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    ilGenerator.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }
    }
}
