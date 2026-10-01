using MongoDB.Bson.Serialization.Attributes;

namespace EventEase.Models
{
    /// <summary>
    /// Atomic counter document for generating sequential IDs in MongoDB.
    /// Uses findAndModify with $inc for thread-safe ID generation.
    /// </summary>
    public class Counter
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        public int Value { get; set; }
    }
}
