using FluentResults;

namespace InDuckTor.Shared.Models;

public static class Errors
{
    public class Forbidden(string? message = null) : Error(message);

    public class NotFound(string message) : Error(message);

    public class Conflict(string message) : Error(message);

    public class InvalidInput(string message) : Error(message)
    {
        public void AddInvalidField(string fieldName, string message)
        {
            if (Metadata.TryGetValue(fieldName, out var value) && value is List<string> validationMessages)
            {
                validationMessages.Add(message);
                return;
            }

            Metadata.Add(fieldName, new List<string> { message });
        }

        public Dictionary<string, string[]> ProduceFieldsErrors()
        {
            var errorsMap = new Dictionary<string, string[]>(Metadata.Count);
            foreach (var pair in Metadata)
            {
                if (Metadata[pair.Key] is not List<string> validationMessages) continue;
                errorsMap.Add(pair.Key, validationMessages.ToArray());
            }

            return errorsMap;
        }
    }
}