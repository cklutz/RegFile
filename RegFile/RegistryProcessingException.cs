using System;

namespace RegFile
{
    [System.Serializable]
    public class RegistryProcessingException : Exception
    {
        public RegistryProcessingException() { }
        public RegistryProcessingException(string message) : base(message) { }
        public RegistryProcessingException(string message, System.Exception inner) : base(message, inner) { }
        protected RegistryProcessingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
