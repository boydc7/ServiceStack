#if !NETSTANDARD2_1
using System.ServiceModel.Channels;

namespace ServiceStack
{
    public interface IRequiresSoapMessage
    {
        Message Message { get; set; }
    }
}
#endif
