using Newtonsoft.Json;

namespace CafeteriaSystem.Helpers
{
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
#pragma warning disable CS8603 // Possible null reference return.
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}
