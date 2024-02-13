namespace HangmanServer
{
    public class RequestResult
    {
        public bool result {  get; set; }
        public string message { get; set; } = "";
    }

    public class ConnectResult : RequestResult
    {
        public Guid connectionID { get; set; }
        public byte[]? exponent { get; set; }
        public byte[]? modulus { get; set; }
    }

    public class DisconnectResult : RequestResult { }
}
