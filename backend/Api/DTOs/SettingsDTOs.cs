namespace Api.DTOs
{
    public class ShippingTickerMessagesRequest
    {
        public List<string> Messages { get; set; } = new List<string>();
    }

    public class ShippingTickerMessagesResponse
    {
        public List<string> Messages { get; set; } = new List<string>();
    }

    public class SettingsResponse
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}

