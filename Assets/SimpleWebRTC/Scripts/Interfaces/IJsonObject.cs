using System;

public interface IJsonObject<T> {
    string ConvertToJSON();
    static T FromJSON(string jsonString) => throw new NotImplementedException();
}