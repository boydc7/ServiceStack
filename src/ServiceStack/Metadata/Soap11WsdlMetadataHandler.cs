#if !NETSTANDARD2_1

namespace ServiceStack.Metadata
{
    public class Soap11WsdlMetadataHandler : WsdlMetadataHandlerBase
    {
        protected override WsdlTemplateBase GetWsdlTemplate() => new Soap11WsdlTemplate();
    }
}

#endif
