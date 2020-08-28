using System.Threading.Tasks;

namespace ServiceStack
{
    /// <summary>
    /// Provide unique, incrementing sequences. Used in PocoDynamo.
    /// </summary>
    public interface ISequenceSource : IRequiresSchema
    {
        long Increment(string key, long amount = 1);
        Task<long> IncrementAsync(string key, long amount = 1);
        void Reset(string key, long startingAt = 0);
        Task ResetAsync(string key, long startingAt = 0);
    }
}