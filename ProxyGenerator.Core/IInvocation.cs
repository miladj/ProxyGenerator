﻿using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public interface IInvocation
    {
        object[] Arguments { get; }

    }
}