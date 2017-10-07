using System;

namespace RestEasy.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class RequiresReadAccessAttribute : Attribute
    {
        public readonly bool RequiresRead = true;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class RequiresWriteAccessAttribute : Attribute
    {
        public readonly bool RequiresWrite = true;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class RequiresAdminAccessAttribute : Attribute
    {
        public readonly bool RequiresAdmin = true;
    }
}
