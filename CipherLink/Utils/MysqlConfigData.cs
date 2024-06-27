namespace CipherLink.Utils
{
    public class MysqlConfigData
    {
        public string? Server { get; set; } = "localhost";
        public string? Database { get; set; } = "cipherlink";
        public string? User { get; set; } = "root";
        public string? Password { get; set; } = "password";
        public string? Port { get; set; } = "3306";
    }
}
