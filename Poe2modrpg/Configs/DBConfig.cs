namespace PoE2ModRPG.Configs
{
    public class DBConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3306;
        public string User { get; set; } = "user";
        public string Password { get; set; } = "password";
        public string Database { get; set; } = "poe2mod";
    }
}
